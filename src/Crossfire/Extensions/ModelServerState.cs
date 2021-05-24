// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Crossfire.Extensions
{
    /// <summary>
    /// Azure AS health states.
    /// </summary>
    public class ModelServerState
    {
        /// <summary>
        /// Server online.
        /// </summary>
        public const string Online = "Succeeded";

        /// <summary>
        /// Server offline.
        /// </summary>
        public const string Offline = "Paused";

        /// <summary>
        /// Server launching.
        /// </summary>
        public const string Updating = "Updating";
    }
}
