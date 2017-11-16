// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    internal class FileInfoWrapper : ILogFileInfo
    {
        FileInfo innerFileInfo;

        public FileInfoWrapper(FileInfo fileInfo)
        {
            this.innerFileInfo = fileInfo;
        }

        public void Delete()
        {
            this.innerFileInfo?.Delete();
            this.innerFileInfo = null;
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                if (this.innerFileInfo != null)
                {
                    return this.innerFileInfo.CreationTimeUtc;
                }
                else
                {
                    return default(DateTime);
                }
            }
        }
    }
}
