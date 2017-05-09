// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Security;
using Validation;

namespace Microsoft.Extensions.Configuration.ServiceFabric
{
    public static class SecureStringExtensions
    {
        public static string ToUnsecureString(this SecureString secureString)
        {
            Requires.NotNull(secureString, nameof(secureString));

            IntPtr unmanagedString = IntPtr.Zero;

            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
