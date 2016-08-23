// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.HealthReporters
{
    public class CsvHealthReporter : IHealthReporter
    {
        #region Fields
        public const string DefaultHealthReportName = "HealthReport.csv";
        public const HealthReportLevel DefaultHealthReportLevel = HealthReportLevel.Error;
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private readonly object _locker = new object();
        private FileStream _fileStream;
        #endregion

        #region Properties
        internal HealthReportLevel LogLevel { get; private set; }
        internal bool IsExternalStreamWriter { get; private set; }
        internal StreamWriter StreamWriter { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a CsvHealthReporter with configuration.
        /// </summary>
        /// <param name="configuration">CsvHealthReporter configuration.</param>
        /// <param name="streamWriter">When provided, used to overwrite the stream writer created by the configuration.</param>
        public CsvHealthReporter(IConfiguration configuration, StreamWriter streamWriter = null)
        {
            IsExternalStreamWriter = streamWriter != null;
            Initialize(configuration, streamWriter);
        }

        /// <summary>
        /// Create a CsvHealthReporter with path to configuration file.
        /// </summary>
        /// <param name="configurationFilePath">CsvHealthReporter configuration file path.</param>
        /// <param name="streamWriter">When provided, used to overwrite the stream writer created by the configuration.</param>
        public CsvHealthReporter(string configurationFilePath, StreamWriter streamWriter = null)
        {
            Validation.Requires.NotNullOrWhiteSpace(configurationFilePath, nameof(configurationFilePath));
            IsExternalStreamWriter = streamWriter != null;

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false);
            IConfiguration configuration = builder.Build();

            Initialize(configuration, streamWriter);
        }
        #endregion

        #region Methods
        private void Initialize(IConfiguration configuration, StreamWriter streamWriter)
        {
            StringBuilder errorBuilder = new StringBuilder();

            string logLevelString = configuration["healthReporter:logLevel"] ?? "Warning";
            HealthReportLevel logLevel;
            if (!Enum.TryParse(logLevelString, out logLevel))
            {
                errorBuilder.AppendLine($"Failed to parse log level. Please check the value of: {logLevelString}.");
                logLevel = DefaultHealthReportLevel;
            }
            LogLevel = logLevel;

            if (IsExternalStreamWriter)
            {
                Validation.Requires.NotNull(streamWriter, nameof(streamWriter));
                StreamWriter = streamWriter;
            }
            else
            {
                string logFilePath = null;
                try
                {
                    logFilePath = configuration["healthReporter:logFilePath"];
                    // Set default value for HealthReport.csv
                    if (string.IsNullOrWhiteSpace(logFilePath))
                    {
                        logFilePath = DefaultHealthReportName;
                        errorBuilder.AppendLine($"logFilePath is not specified in configuration file. Fall back to default path: {logFilePath}");
                    }

                    _fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                catch (IOException)
                {
                    try
                    {
                        // In case file is locked by other process, give it another shot
                        logFilePath = $"{Path.GetFileNameWithoutExtension(logFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(logFilePath)}";
                        _fileStream = new FileStream(logFilePath, FileMode.Append);
                    }
                    catch
                    {
                        // Suppress exception to prevent HealthReporter from crashing its consumer
                        _fileStream = null;
                    }
                }

                if (_fileStream != null)
                {
                    StreamWriter = streamWriter ?? new StreamWriter(_fileStream, Encoding.UTF8);
                }
            }

            string selfErrorString = errorBuilder.ToString();
            if (!string.IsNullOrEmpty(selfErrorString))
            {
                ReportProblem(selfErrorString, TraceTag);
            }
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            ReportText(HealthReportLevel.Message, description, context);
        }

        public void ReportProblem(string description, string context = null)
        {
            ReportText(HealthReportLevel.Error, description, context);
        }

        public void ReportWarning(string description, string context = null)
        {
            ReportText(HealthReportLevel.Warning, description, context);
        }

        private void ReportText(HealthReportLevel level, string text, string context = null)
        {
            try
            {
                if (level < LogLevel)
                {
                    return;
                }
                context = context ?? string.Empty;
                string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
                string message = $"{timestamp},{context.Replace(',', '_')},{level},{text}";
                WriteLine(message);
            }
            catch
            {
                // Suppress exception to prevent HealthReporter from crashing its consumer
            }
        }

        private void WriteLine(string text)
        {
            lock (_locker)
            {
                StreamWriter.WriteLine(text);
            }
        }

        public void Dispose()
        {
            if (StreamWriter != null)
            {
                StreamWriter.Dispose();
            }

            if (_fileStream != null)
            {
                _fileStream.Dispose();
            }
        }
        #endregion
    }
}
