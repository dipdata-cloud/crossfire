// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Crossfire.Background;
using Crossfire.Background.Common;
using Crossfire.Extensions;
using Crossfire.Extensions.CodeSecurity;
using Crossfire.Model;
using Crossfire.Model.Metadata;
using Crossfire.Model.Query;
using Crossfire.SignalR.Hubs;
using Crossfire.SignalR.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PyrosWeb.Extensions;

namespace Crossfire.Controllers
{
    /// <summary>
    /// Endpoints used to interact with connected models: query, get metadata etc.
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class ModelController : Controller
    {
        private readonly ILogger logger;
        private readonly BackgroundJobLauncher jobLauncher;
        private readonly AzureAdB2CGraphClient graphClient;
        private readonly IHubContext<ModelMessageHub> hubContext;
        private readonly ILoggerFactory loggerFactory;
        private readonly IMemoryCache memoryCache;
        private readonly CachedEntityClient<string> tokenCache;
        private readonly CachedEntityClient<UserConnectionInfo[]> userInfoCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelController"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory <see cref="ILoggerFactory"/>.</param>
        /// <param name="memoryCache">Memory cache to be used by jobs.</param>
        /// <param name="jobLauncher">Job provisioner.</param>
        /// <param name="graphClient">Azure AD Graph API client.</param>
        /// <param name="tokenCache">A distributed cache for storing access tokens.</param>
        /// <param name="userInfoCache">A distributed cache for storing user connection info.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        public ModelController(
            ILoggerFactory loggerFactory,
            IMemoryCache memoryCache,
            BackgroundJobLauncher jobLauncher,
            AzureAdB2CGraphClient graphClient,
            CachedEntityClient<string> tokenCache,
            CachedEntityClient<UserConnectionInfo[]> userInfoCache,
            IHubContext<ModelMessageHub> hubContext)
        {
            this.logger = loggerFactory.CreateLogger(nameof(Controllers.ModelController));
            this.loggerFactory = loggerFactory;
            this.jobLauncher = jobLauncher;
            this.graphClient = graphClient;
            this.hubContext = hubContext;
            this.memoryCache = memoryCache;
            this.tokenCache = tokenCache;
            this.userInfoCache = userInfoCache;
        }

        /// <summary>
        /// Accepts request to launch executor
        /// NB: this method should only be called on channel join.
        /// </summary>
        /// <param name="value">LaunchRequest json.</param>
        /// <returns>Launched executor SHA.</returns>
        [HttpPost("launch")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> Launch([FromBody] LaunchRequest value)
        {
            if (!this.HttpContext.User.Identity.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            if (value == null)
            {
                return this.BadRequest();
            }

            try
            {
                LaunchExecutorJob job = new LaunchExecutorJob(
                    loggerFactory: this.loggerFactory,
                    hubContext: this.hubContext);

                string jobId = this.jobLauncher.AcceptJob(
                    job: job,
                    request: value,
                    userPrincipalName: await UserPrincipalResolver.ResolveUserFromClaimsPrincipal(this.User, this.graphClient),
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

                return this.Accepted(new { JobId = jobId });
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, $"Failed to accept model launch: {JsonConvert.SerializeObject(value)}");
                return this.StatusCode(500, "Job preparation failed");
            }
        }

        /// <summary>
        /// <response code="202">Accepts new request for background job. Job result is returned to a client via a SignalR hub connection</response>
        /// </summary>
        /// <param name="value"><see cref="QueryRequest"/>.</param>
        /// <returns>JobId in Hangfire framework, if accepted, otherwise HTTP500.</returns>
        [HttpPost("query")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> QueryModel([FromBody] QueryRequest value)
        {
            if (!this.HttpContext.User.Identity.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            if (value == null)
            {
                return this.BadRequest();
            }

            try
            {
                QueryJob job = new QueryJob(
                    cache: this.memoryCache,
                    loggerFactory: this.loggerFactory,
                    hubContext: this.hubContext,
                    tokenCache: this.tokenCache);

                string jobId = this.jobLauncher.AcceptJob(
                    job: job,
                    request: value,
                    userPrincipalName: await UserPrincipalResolver.ResolveUserFromClaimsPrincipal(this.User, this.graphClient),
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

                return this.Accepted(new { JobId = jobId });
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, $"Failed to accept a model query: {JsonConvert.SerializeObject(value)}");
                return this.StatusCode(500, "Job preparation failed");
            }
        }

        /// <summary>
        /// Retrieves a connected model metadata.
        /// </summary>
        /// <param name="value"><see cref="ModelMetadataRequest"/>.</param>
        /// <returns>JobId in Hangfire framework, if accepted, otherwise HTTP500.</returns>
        [HttpPost("metadata")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> ModelMetadata([FromBody] ModelMetadataRequest value)
        {
            if (!this.HttpContext.User.Identity.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            if (value == null)
            {
                return this.BadRequest();
            }

            try
            {
                MetadataJob job = new MetadataJob(
                    cache: this.memoryCache,
                    loggerFactory: this.loggerFactory,
                    hubContext: this.hubContext,
                    tokenCache: this.tokenCache);

                string jobId = this.jobLauncher.AcceptJob(
                    job: job,
                    request: value,
                    userPrincipalName: await UserPrincipalResolver.ResolveUserFromClaimsPrincipal(this.User, this.graphClient),
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);

                return this.Accepted(new { JobId = jobId });
            }
            catch
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Retrieves user connection info for the authenticated user.
        /// </summary>
        /// <param name="request"><see cref="UserConnectionInfoRequest"/>.</param>
        /// <returns>JobId in Hangfire framework, if accepted, otherwise HTTP500.</returns>
        [HttpPost("user/connections")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [Produces("application/json")]
        public async Task<IActionResult> GetUserConnectionInfo([FromBody] UserConnectionInfoRequest request)
        {
            if (!this.HttpContext.User.Identity.IsAuthenticated)
            {
                return this.Unauthorized();
            }

            if (request == null)
            {
                return this.BadRequest();
            }

            try
            {
                UserConnectionInfoJob job = new UserConnectionInfoJob(
                    cache: this.memoryCache,
                    loggerFactory: this.loggerFactory,
                    hubContext: this.hubContext,
                    resultCache: this.userInfoCache,
                    tokenCache: this.tokenCache);

                string jobId = this.jobLauncher.AcceptJob(
                    job: job,
                    request: request,
                    userPrincipalName: await UserPrincipalResolver.ResolveUserFromClaimsPrincipal(this.User, this.graphClient),
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
                return this.Accepted(jobId);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                return this.StatusCode(500, "Request evaluation failed badly");
            }
        }
    }
}
