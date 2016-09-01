// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.EventFlow.Core.Implementations;
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
        private readonly object locker = new object();
        private FileStream fileStream;
        private CsvHealthReporterConfiguration configuration;
        private ManualResetEventSlim streamCreationEventWaiterSlim = new ManualResetEventSlim(false);
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

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false).Build();

            Initialize(configuration.GetSection("healthReporter"), streamWriter);
        }
        #endregion

        #region Methods
        public virtual string GetReportFileName(string prefix)
        {
            return $"{prefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}.csv";
        }

        private void Initialize(CsvHealthReporterConfiguration configuration, StreamWriter streamWriter)
        {
            StringBuilder errorBuilder = new StringBuilder();

            this.configuration = configuration;
            // Set default value for HealthReport file prefix
            if (string.IsNullOrWhiteSpace(this.configuration.LogFilePrefix))
            {
                this.configuration.LogFilePrefix = DefaultHealthReporterPrefix;
                errorBuilder.AppendLine($"{nameof(this.configuration.LogFilePrefix)} is not specified in configuration file. Fall back to default value: {this.configuration.LogFilePrefix}");
            }
            // TODO: Add to CsvHealthReporterConfiguration
            int throttleTimeSpan;
            int.TryParse(configuration["throttleTimeSpan"], out throttleTimeSpan); // Will return 0 if failed to parse
            throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(throttleTimeSpan));

            string logLevelString = this.configuration?.MinReportLevel;



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
                this.streamCreationEventWaiterSlim.Set();
                Validation.Requires.NotNull(streamWriter, nameof(streamWriter));
                StreamWriter = streamWriter;
            }
            else
            {
                BuildStreamWriterFromConfiguration();
                UtcMidnightNotifier.DayChanged += UtcMidnightNotifier_DayChanged;
            }

            string selfErrorString = errorBuilder.ToString();
            if (!string.IsNullOrEmpty(selfErrorString))
            {
                ReportProblem(selfErrorString, TraceTag);
            }
        }

        private void BuildStreamWriterFromConfiguration()
        {
            string logFilePath = GetFullFilePath(this.configuration);
            if (TryCreateFileStream(logFilePath))
            {
                if (this.fileStream != null)
                {
                    try
                    {
                        this.streamCreationEventWaiterSlim.Reset();
                        StreamWriter = new StreamWriter(this.fileStream, Encoding.UTF8);
                    }
                    finally
                    {
                        this.streamCreationEventWaiterSlim.Set();
                    }
                }
            }
        }

        private string GetFullFilePath(CsvHealthReporterConfiguration configuration)
        {
            string logFilePath;
            logFilePath = GetReportFileName(configuration.LogFilePrefix);
            if (!string.IsNullOrEmpty(configuration.LogFileFolder))
            {
                logFilePath = Path.Combine(configuration.LogFileFolder, logFilePath);
            }
            return Path.GetFullPath(logFilePath);
        }

        /// <summary>
        /// Create a new file stream when fullFilePath is different than the current file stream.
        /// </summary>
        /// <param name="fullFilePath">New path to the health reporter.</param>
        /// <param name="fileStream">New FileStream when creation executed.</param>
        /// <returns>Return true when creation successfully executed. Otherwise, return false.</returns>
        private bool TryCreateFileStream(string fullFilePath)
        {
            Validation.Requires.NotNullOrWhiteSpace(fullFilePath, nameof(fullFilePath));
            if (this.fileStream != null && Path.GetFullPath(this.fileStream.Name).Equals(fullFilePath, StringComparison.OrdinalIgnoreCase))
            {
                // Do not create new file stream when the current stream and the new stream pointing to the same path.
                return false;
            }

            try
            {
                fileStream = new FileStream(fullFilePath, FileMode.Append);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shot.
                fullFilePath = $"{Path.GetFileNameWithoutExtension(fullFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(fullFilePath)}";
                fileStream = new FileStream(fullFilePath, FileMode.Append);
            }
            return true;
        }

        private void UtcMidnightNotifier_DayChanged(object sender, EventArgs e)
        {
            // Change file name
            BuildStreamWriterFromConfiguration();
        }

        private void Initialize(IConfiguration configuration, StreamWriter streamWriter)
        {
            CsvHealthReporterConfiguration boundConfiguration = new CsvHealthReporterConfiguration();
            configuration.Bind(boundConfiguration);

            Initialize(boundConfiguration, streamWriter);
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
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return text;
            }
        }

        private void WriteLine(string text)
        {
            ThreadStart ts = new ThreadStart(() =>
            {
                this.streamCreationEventWaiterSlim.Wait();
                lock (locker)
                {
                    StreamWriter.WriteLine(text);
                }
            });
            ts.Invoke();
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

            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }

            UtcMidnightNotifier.DayChanged -= UtcMidnightNotifier_DayChanged;
        }
        #endregion
    }
}
