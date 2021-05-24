// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Crossfire.Background.Common;
using Crossfire.Model.Base;
using Crossfire.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Crossfire.Controllers
{
    /// <summary>
    /// Endpoints used to directly control Hangfire jobs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Authorize]
    [Route("[controller]")]
    public class JobController : Controller
    {
        private readonly ILogger logger;

        private readonly BackgroundJobLauncher jobLauncher;

        private readonly IHubContext<ModelMessageHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobController"/> class.
        /// </summary>
        /// <param name="jobLauncher">A Hangfire job launcher instance.</param>
        /// <param name="logger">Logger factory.</param>
        /// <param name="hubContext">SignalR hub context.</param>
        public JobController(
            BackgroundJobLauncher jobLauncher,
            ILoggerFactory logger,
            IHubContext<ModelMessageHub> hubContext)
        {
            this.logger = logger.CreateLogger("Crossfire.Controllers.JobController");
            this.jobLauncher = jobLauncher;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// Accepts cancellation request for a job. Result is returned to ClientHub via SignalR hub connection.
        /// </summary>
        /// <param name="value">A request to cancel a job.</param>
        /// <returns>Empty response with 202 code if successful, 500 in case of exception.</returns>
        [HttpPost("cancel/{jobId}")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Cancel([FromBody] JobRequest value)
        {
            try
            {
                await this.jobLauncher.CancelJob(
                    request: value,
                    hubContext: this.hubContext,
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
                return this.Accepted();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                return this.StatusCode(500, "Request evaluation failed badly");
            }
        }

        /// <summary>
        /// Accepts retry request for a job. Result is returned to ClientHub via SignalR hub connection.
        /// </summary>
        /// <param name="value">A request to retry a job.</param>
        /// <returns>Empty response with 202 code if successful, 500 in case of exception.</returns>
        [HttpPost("retry/{jobId}")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Retry([FromBody] JobRequest value)
        {
            try
            {
                await this.jobLauncher.RequeueJob(
                    request: value,
                    hubContext: this.hubContext,
                    userSubscriberName: this.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
                return this.Accepted();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                return this.StatusCode(500, "Request evaluation failed badly");
            }
        }
    }
}
