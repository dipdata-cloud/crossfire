// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using Microsoft.AspNetCore.Authentication;

namespace Crossfire.Extensions
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "HMAC auth not used atm. Purpose is under review.")]
    public class HMACAuthenticationOptions : AuthenticationSchemeOptions
    {
        public HMACAuthenticationOptions()
        {
        }

        public NonceCacheExtension NonceCache { get; set; }

        public HMACAllowedApplicationCollection AllowedApplications { get; set; }

        public string AuthenticationScheme
        {
            get { return "pyros-signature"; }
        }

        /// <summary>
        /// Gets or sets max age for each request in seconds.
        /// </summary>
        public ulong RequestMaxAge { get; set; }

        public class HMACAllowedApplicationCollection
        {
            private readonly Dictionary<string, HMACAllowedApplication> hmacApplications;

            public HMACAllowedApplicationCollection()
            {
                this.hmacApplications = new Dictionary<string, HMACAllowedApplication>();
            }

            public HMACAllowedApplicationCollection(string appId, string appSecret)
            {
                this.hmacApplications = new Dictionary<string, HMACAllowedApplication>
                {
                    { appId, new HMACAllowedApplication { ApplicationId = appId, ApplicationSecret = appSecret } },
                };
            }

            public HMACAllowedApplication this[string appId] => this.hmacApplications[appId];

            public void AddApplication(string appId, string appSecret)
            {
                if (!this.hmacApplications.ContainsKey(appId))
                {
                    this.hmacApplications.Add(appId, new HMACAllowedApplication { ApplicationId = appId, ApplicationSecret = appSecret });
                }
            }

            public bool ContainsApplication(string appId)
            {
                return this.hmacApplications.ContainsKey(appId);
            }

            public class HMACAllowedApplication
            {
                public string ApplicationId { get; set; }

                public string ApplicationSecret { get; set; }
            }
        }

        public class NonceCacheExtension
        {
            private readonly Dictionary<string, DateTime> noncePool;
            private readonly Timer nonceRecycler;

            public NonceCacheExtension(int recyclePeriod)
            {
                this.noncePool = new Dictionary<string, DateTime>();
                this.nonceRecycler = new Timer
                {
                    Interval = recyclePeriod * 1e3,
                    AutoReset = true,
                };
                this.nonceRecycler.Elapsed += this.NonceRecycler_Elapsed;
                this.nonceRecycler.Enabled = true;
            }

            public Dictionary<string, DateTime> NoncePool => this.noncePool;

            public void AddNonce(string nonce, ulong age)
            {
                this.noncePool.Add(nonce, DateTime.UtcNow.AddSeconds(age));
            }

            public bool IsNonceExpired(string nonce)
            {
                return this.NoncePool[nonce] < DateTime.Now;
            }

            // TODO: bad complexity O(n) + O(m) - fix later
            private void NonceRecycler_Elapsed(object sender, ElapsedEventArgs e)
            {
                List<string> expired = new List<string>(1000);

                // prevent memory leak
                // recycle all nonces with > maxage lifetime
                foreach (string nonce in this.NoncePool.Keys)
                {
                    if (this.IsNonceExpired(nonce))
                    {
                        expired.Add(nonce);
                    }
                }

                foreach (string nonce in expired)
                {
                    this.noncePool.Remove(nonce);
                }
            }
        }
    }
}
