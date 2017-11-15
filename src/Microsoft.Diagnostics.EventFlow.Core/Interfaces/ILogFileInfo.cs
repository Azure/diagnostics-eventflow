﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow
{
    internal interface ILogFileInfo
    {
        DateTime CreationTimeUtc { get; }

        void Delete();
    }
}