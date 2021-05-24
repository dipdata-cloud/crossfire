// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Crossfire.Extensions
{
    // special credits to this guy
    // http://bitoftech.net/2014/12/15/secure-asp-net-web-api-using-api-key-authentication-hmac-authentication/
    // below is an Asp.NET Core adaptation of the solution
    [ExcludeFromCodeCoverage]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Code not currently used - purpose to be re-evaluated")]
    public class HMACAuthentication : AuthenticationHandler<HMACAuthenticationOptions>
    {
        private readonly HMACAuthenticationOptions authOptions;
        private readonly UrlEncoder urlEncoder;

        [ExcludeFromCodeCoverage]
        public HMACAuthentication(IOptionsMonitor<HMACAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            this.authOptions = options.CurrentValue;
            this.urlEncoder = encoder;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = this.Context.Request.Headers["Authorization"];
            var schemeHeader = this.Context.Request.Headers["AuthorizationPayload"];

            // full content is signed - read it
            string requestContent = string.Empty;
            using (var requestBodyStream = new MemoryStream())
            {
                var body = this.Context.Request.Body;
                await body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                requestContent = await new StreamReader(requestBodyStream).ReadToEndAsync();
            }

            // load request body back
            byte[] requestData = Encoding.UTF8.GetBytes(requestContent);
            this.Request.Body = new MemoryStream(requestData);

            // no debug info on purpose - less known, more protected
            var defaultResult = AuthenticateResult.Fail("Authentication Failed");

            if (!string.IsNullOrEmpty(authHeader)
                && this.GetScheme(authHeader).Contains(this.authOptions.AuthenticationScheme))
            {
                var applicationId = this.GetApplicationId(schemeHeader);
                var incomingBase64Signature = this.GetSignature(authHeader);
                var nonce = this.GetNonce(schemeHeader);
                var requestTimeStamp = this.GetTimestamp(schemeHeader);
                var userClaim = this.GetUserIdClaim(schemeHeader);

                var isValid = this.IsValidRequest(requestContent, applicationId, GetUri(this.Context.Request), this.Context.Request.Method, incomingBase64Signature, nonce, requestTimeStamp);

                if (isValid)
                {
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity("pyros-signature");
                    claimsIdentity.AddClaim(new Claim("UserId", userClaim));
                    claimsIdentity.AddClaim(new Claim("ApplicationId", applicationId));

                    ClaimsPrincipal principal = new ClaimsPrincipal(claimsIdentity);
                    var successResult = AuthenticateResult.Success(new AuthenticationTicket(principal, this.authOptions.AuthenticationScheme));
                    return successResult;
                }
                else
                {
                    return defaultResult;
                }
            }
            else
            {
                return defaultResult;
            }
        }

        /// <summary>
        /// Computes SHA-256 hash for given string.
        /// </summary>
        /// <param name="contentString">String to hash.</param>
        /// <returns>Hash a byte array.</returns>
        private static byte[] ComputeHash(string contentString)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = null;
            var content = Encoding.UTF8.GetBytes(contentString);
            if (content.Length != 0)
            {
                hash = sha256.ComputeHash(content);
            }

            return hash;
        }

        private static string GetUri(HttpRequest request)
        {
            UriBuilder result;
            if (request.Host.Port != null)
            {
                result = new UriBuilder(request.Scheme, request.Host.Host, (int)request.Host.Port, request.Path);
                return result.ToString();
            }
            else
            {
                result = new UriBuilder(request.Scheme, request.Host.Host)
                {
                    Path = request.Path,
                };
                return result.ToString();
            }
        }

        [ExcludeFromCodeCoverage]
        private bool IsValidRequest(string requestContent, string applicationId, string incomingRequestUri, string requestHttpMethod, string incomingBase64Signature, string nonce, string requestTimeStamp)
        {
            string requestUri = this.urlEncoder.Encode(incomingRequestUri).ToLower();

            // application not allowed - failed request
            if (!this.authOptions.AllowedApplications.ContainsApplication(applicationId))
            {
                return false;
            }

            var sharedKey = this.authOptions.AllowedApplications[applicationId].ApplicationSecret;

            if (this.IsReplayRequest(nonce, requestTimeStamp))
            {
                return false;
            }

            byte[] hash = ComputeHash(requestContent);

            string requestContentBase64String;

            // no hash - no fun
            if (hash != null)
            {
                requestContentBase64String = Convert.ToBase64String(hash);
            }
            else
            {
                return false;
            }

            string data = $"{applicationId}{requestHttpMethod}{requestUri}{requestTimeStamp}{nonce}{requestContentBase64String}";

            var secretKeyBytes = Convert.FromBase64String(sharedKey);

            byte[] signature = Encoding.UTF8.GetBytes(data);

            using HMACSHA256 hmac = new HMACSHA256(secretKeyBytes);
            byte[] signatureBytes = hmac.ComputeHash(signature);

            return incomingBase64Signature.Equals(Convert.ToBase64String(signatureBytes), StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if this request is a replay request by caching nonce and verifying timestamp.
        /// </summary>
        /// <param name="nonce">Nonce value.</param>
        /// <param name="requestTimeStamp">Time when request was received.</param>
        /// <returns>True if request is a replay attempt.</returns>
        private bool IsReplayRequest(string nonce, string requestTimeStamp)
        {
            if (this.authOptions.NonceCache.NoncePool.ContainsKey(nonce))
            {
                return true;
            }

            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;

            var serverTotalSeconds = Convert.ToUInt64(currentTs.TotalSeconds);
            var requestTotalSeconds = Convert.ToUInt64(requestTimeStamp);

            if ((serverTotalSeconds - requestTotalSeconds) > this.authOptions.RequestMaxAge)
            {
                return true;
            }

            this.authOptions.NonceCache.AddNonce(nonce, this.authOptions.RequestMaxAge);

            return false;
        }

        // HEADER format: pyros-signature <hash>
        private string GetSignature(StringValues authHeader)
        {
            if (authHeader.Count > 1)
            {
                throw new Exception("Bad header");
            }

            return authHeader[0].Split(' ')[1];
        }

        private string GetScheme(StringValues authHeader)
        {
            if (authHeader.Count > 1)
            {
                throw new Exception("Bad header");
            }

            return authHeader[0].Split(' ')[0];
        }

        // PAYLOAD format: <appid> <nonce> <timestamp>
        private string GetApplicationId(StringValues authPayload)
        {
            if (authPayload.Count > 1)
            {
                throw new Exception("Bad payload");
            }

            return authPayload[0].Split(' ')[0];
        }

        private string GetNonce(StringValues authPayload)
        {
            if (authPayload.Count > 1)
            {
                throw new Exception("Bad payload");
            }

            return authPayload[0].Split(' ')[1];
        }

        private string GetTimestamp(StringValues authPayload)
        {
            if (authPayload.Count > 1)
            {
                throw new Exception("Bad payload");
            }

            return authPayload[0].Split(' ')[2];
        }

        private string GetUserIdClaim(StringValues authPayload)
        {
            if (authPayload.Count > 1)
            {
                throw new Exception("Bad payload");
            }

            return authPayload[0].Split(' ')[3];
        }
    }
}
