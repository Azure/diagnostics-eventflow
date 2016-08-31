// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    public class CsvHealthReporter : IHealthReporter
    {
        internal enum HealthReportLevel
        {
            Message,
            Warning,
            Error
        }

        #region Fields
        public const string DefaultHealthReporterPrefix = "HealthReport";
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private readonly object _locker = new object();
        private FileStream _fileStream;
        private TimeSpanThrottle throttle;
        #endregion

        #region Properties
        internal HealthReportLevel LogLevel { get; private set; } = HealthReportLevel.Error;
        internal bool IsExternalStreamWriter { get; private set; }
        internal StreamWriter StreamWriter { get; private set; }
        #endregion

        #region Constructors
        public CsvHealthReporter(CsvHealthReporterConfiguration configuration, StreamWriter streamWriter = null)
        {
            IsExternalStreamWriter = streamWriter != null;
            Initialize(configuration, streamWriter);
        }

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

            Initialize(configuration.GetSection("healthReporter"), streamWriter);
        }
        #endregion

        #region Methods
        public virtual string GetReportFileName(string prefix)
        {
            return $"{prefix}_{DateTime.Today.ToString("yyyyMMdd")}.csv";
        }

        private void Initialize(CsvHealthReporterConfiguration configuration, StreamWriter streamWriter)
        {
            StringBuilder errorBuilder = new StringBuilder();

            // TODO: Add to CsvHealthReporterConfiguration
            int throttleTimeSpan;
            int.TryParse(configuration["throttleTimeSpan"], out throttleTimeSpan); // Will return 0 if failed to parse
            throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(throttleTimeSpan));

            string logLevelString = configuration["healthReporter:logLevel"];
            string logLevelString = configuration?.MinReportLevel;
            HealthReportLevel logLevel;
            if (Enum.TryParse(logLevelString, out logLevel))
            {
                LogLevel = logLevel;
            }
            else
            {
                errorBuilder.AppendLine($"Failed to parse log level. Please check the value of: {logLevelString}.");
            }

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
                    if (!string.IsNullOrEmpty(configuration.LogFileFolder))
                    {
                        logFilePath = Path.Combine(configuration.LogFileFolder, GetReportFileName(configuration.LogFilePrefix));
                    }
                    else
                    {
                        logFilePath = configuration.LogFilePrefix;
                        // Set default value for HealthReport.csv
                        if (string.IsNullOrWhiteSpace(logFilePath))
                        {
                            logFilePath = DefaultHealthReporterPrefix;
                            errorBuilder.AppendLine($"{nameof(configuration.LogFilePrefix)} is not specified in configuration file. Fall back to default value: {logFilePath}");
                        }
                        logFilePath = GetReportFileName(logFilePath);
                    }

                    _fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                catch (IOException)
                {
                    // In case file is locked by other process, give it another shot.
                    logFilePath = $"{Path.GetFileNameWithoutExtension(logFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(logFilePath)}";
                    _fileStream = new FileStream(logFilePath, FileMode.Append);
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

        private void Initialize(IConfiguration configuration, StreamWriter streamWriter)
        {
            CsvHealthReporterConfiguration boundConfiguration = new CsvHealthReporterConfiguration();
            configuration.Bind(boundConfiguration);

            Initialize(boundConfiguration, streamWriter);
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            ReportText(HealthReportLevel.Warning, description, context);
        }

        public void ReportProblem(string description, string context = null)
        {
            ReportText(HealthReportLevel.Warning, description, context);
        }

        public void ReportWarning(string description, string context = null)
        {
            ReportText(HealthReportLevel.Warning, description, context);
        }

        private void ReportText(HealthReportLevel level, string text, string context = null)
        {
            throttle.Execute(() =>
            {
                try
                {
                    if (level < LogLevel)
                    {
                        return;
                    }
                    context = context ?? string.Empty;
                    string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
                    string message = $"{timestamp},{EscapeComma(context)},{level},{ EscapeComma(text)}";
                    WriteLine(message);
                }
                catch
                {
                    // Suppress exception to prevent HealthReporter from crashing its consumer
                }
            });
        }

        /// <summary>
        /// Escape comma in a string by quotes the text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string EscapeComma(string text)
        {
            if (text.Contains(","))
            {
                text = text.Replace("\"", "\"\"");
                text = string.Format(CultureInfo.CurrentCulture, "\"{0}\"", text);
                return text;
            }
            else
            {
                return text;
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
            if (!IsExternalStreamWriter)
            {
                if (StreamWriter != null)
                {
                    StreamWriter.Dispose();
                    StreamWriter = null;
                }
            }

            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
        }
        #endregion
    }
}
