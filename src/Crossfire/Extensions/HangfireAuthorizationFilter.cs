// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Hangfire.Dashboard;

namespace Crossfire.Extensions
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Hangifre Auth Filter")]
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        [ExcludeFromCodeCoverage]
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow all authenticated users to see the Dashboard (potentially dangerous).
            // TODO: change this to authenticate separately and for admins only
            return httpContext.User.Identity.IsAuthenticated && httpContext.User.FindFirst("Hangfire") != null;
        }
    }
}
