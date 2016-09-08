// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    internal class CustomHealthReporter : CsvHealthReporter, IHealthReporter, IDisposable
    {
        public Mock<StreamWriter> StreamWriterMock { get; private set; }
        private MemoryStream memoryStream;
        public CustomHealthReporter(string configurationFilePath)
            : base(configurationFilePath)
        {
            Initialize();
        }

        public CustomHealthReporter(IConfiguration configuration)
            : base(configuration)
        {
            Initialize();
        }

        void Initialize()
        {
            this.memoryStream = new MemoryStream();
            StreamWriterMock = new Mock<StreamWriter>(this.memoryStream);
        }

        internal override void SetNewStreamWriter()
        {
            this.StreamWriter = StreamWriterMock.Object;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StreamWriterMock = null;
                this.memoryStream = null;
            }
            base.Dispose(disposing);
        }
    }
}
