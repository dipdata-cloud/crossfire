// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Model.Base
{
    /// <summary>
    /// Hangfire job control request.
    /// </summary>
    public class JobRequest
    {
        /// <summary>
        /// An instruction to cancel a job.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOBACTION_CANCEL = "cancel";

        /// <summary>
        /// An instruction to requeue a job.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOBACTION_REQUEUE = "requeue";

        /// <summary>
        /// An instruction to fail a job.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
        public const string JOBACTION_FAILURE = "failed";

        /// <summary>
        /// Gets or sets a job identifier to control.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Gets or sets a SignalR connection identifier of a client.
        /// </summary>
        public string UniqueClientIdentifier { get; set; }

        /// <summary>
        /// Gets or sets a desired job action.
        /// </summary>
        public string JobAction { get; set; }
    }
}
