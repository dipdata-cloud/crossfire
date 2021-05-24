// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Crossfire.Extensions.CodeSecurity
{
    /// <summary>
    /// Cryphtographic operations on text.
    /// </summary>
    public class TextSecurity
    {
        /// <summary>
        /// Computes SHA-256 hash for given string.
        /// </summary>
        /// <param name="contentString">String to hash.</param>
        /// <returns>Hash in a form of a byte array.</returns>
        public static byte[] ComputeHash(string contentString)
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

        /// <summary>
        /// Generates a SHA256 hash of a string.
        /// </summary>
        /// <param name="contentString">String to hash.</param>
        /// <returns>Hash in a form of Base64 string.</returns>
        public static string ComputeHashString(string contentString)
        {
            byte[] hash = ComputeHash(contentString);
            return Convert.ToBase64String(hash);
        }
    }
}
