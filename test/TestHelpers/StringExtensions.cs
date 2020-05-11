// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public static class StringExtensions
    {
        public static string RemoveAllWhitespace(this string s)
        {
            if (s == null) return null;
            if (s.Length == 0) return s;

            StringBuilder sb = new StringBuilder(s.Length);
            foreach(char c in s) 
            { 
                if (!Char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
