// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class CustomHealthReporter : CsvHealthReporter, IHealthReporter, IDisposable
    {
        public CsvHealthReporterConfiguration ConfigurationWrapper
        {
            get
            {
                return this.Configuration;
            }
        }

        public Mock<StreamWriter> StreamWriterMock { get; private set; }
        public AutoResetEvent WriteOperation { get; private set; }
        public AutoResetEvent FlushOperation { get; private set; }

        private MemoryStream memoryStream;
        private Func<FileStream> createFileStream;
        private Action setStreamWriter;

        internal bool UnauthorizedExceptionUponCreatingFileStreaming { get; set; }

        public CustomHealthReporter(string configurationFilePath)
            : base(configurationFilePath)
        {
            Initialize();
        }

        // This constructor is needed for instantiate from reflection.
        public CustomHealthReporter(IConfiguration configuration)
            : this(configuration, 5000)
        {
        }

        public CustomHealthReporter(IConfiguration configuration,
            int flushPeriodMsec,
            Func<FileStream> customCreateFileStream = null,
            Action setNewStreamWriter = null,
            Func<DateTime> currentTimeProvider = null
            )
            : base(configuration.ToCsvHealthReporterConfiguration(), new Mock<INewReportFileTrigger>().Object, flushPeriodMsec, currentTimeProvider)
        {
            Initialize();

            this.createFileStream = customCreateFileStream;
            this.setStreamWriter = setNewStreamWriter ?? (() =>
            {
                this.StreamWriter = StreamWriterMock.Object;
            });
        }

        void Initialize()
        {
            this.WriteOperation = new AutoResetEvent(initialState: false);
            this.FlushOperation = new AutoResetEvent(initialState: false);
            this.memoryStream = new MemoryStream();
            StreamWriterMock = new Mock<StreamWriter>(this.memoryStream);
            StreamWriterMock.Setup(sw => sw.WriteLine(It.IsAny<string>())).Callback(() => this.WriteOperation.Set());
            StreamWriterMock.Setup(sw => sw.Flush()).Callback(() => this.FlushOperation.Set());
            StreamWriterMock.SetupGet(sw => sw.BaseStream).Returns(this.memoryStream);
        }

        internal override void SetNewStreamWriter()
        {
            this.setStreamWriter();
        }

        internal override FileStream CreateFileStream(string logFilePath)
        {
            if (this.createFileStream != null)
            {
                return this.createFileStream();
            }
            else
            {
                return base.CreateFileStream(logFilePath);
            }
        }

        internal override IEnumerable<ILogFileInfo> GetLogFiles(DirectoryInfo logFolder)
        {
            return new List<TestLogFileInfo>() {
                new TestLogFileInfo(DateTime.UtcNow),
                new TestLogFileInfo(DateTime.UtcNow.AddDays(-1)),
                new TestLogFileInfo(DateTime.UtcNow.AddDays(-2)),
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StreamWriterMock = null;
                this.memoryStream = null;
                this.WriteOperation.Dispose();
                this.FlushOperation.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
