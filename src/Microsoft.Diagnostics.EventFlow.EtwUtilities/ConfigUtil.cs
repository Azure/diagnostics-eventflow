// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Utilities.Etw
{
    public static class ConfigUtil
    {
        /// <summary>
        /// Converts Keywords property value from hexadecimal notation to decimal notation
        /// </summary>
        /// <param name="configuration">Configuration instance to process (a list of records)</param>
        /// <remarks>Configuration binder cannot parse hexadecimal notation, which is very useful for Keywords. 
        /// So as a workaround, we convert the value to decimal notation before passing the configuration to the binder.
        /// </remarks>
        public static void ConvertKeywordsToDecimal(IConfiguration configuration)
        {
            Requires.NotNull(configuration, nameof(configuration));

            foreach (var record in configuration.GetChildren())
            {
                var keywordsStr = record["Keywords"];
                if (string.IsNullOrEmpty(keywordsStr)) continue;

                keywordsStr = keywordsStr.TrimStart(null); // Null means "remove whitespace"
                if (string.IsNullOrEmpty(keywordsStr) || !keywordsStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) continue;

                // Event if HexNumber is set, the .NET Framework TryParse() methods do not allow the '0x' prefix to be part of the value.
                if (!ulong.TryParse(keywordsStr.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong keywords)) continue;

                record["Keywords"] = keywords.ToString("d");
            }
        }
    }
}
