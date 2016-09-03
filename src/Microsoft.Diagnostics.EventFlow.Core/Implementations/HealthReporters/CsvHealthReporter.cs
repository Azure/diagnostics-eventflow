// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Core.Implementations;
using Microsoft.Diagnostics.EventFlow.Core.Implementations.HealthReporters;
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

        #region Constants
        public const string DefaultHealthReporterPrefix = "HealthReport";
        #endregion

        #region Fields
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private readonly BlockingCollection<string> reportCollection = new BlockingCollection<string>();
        private FileStream fileStream;
        private bool newStreamRequested = false;
        private INewReportTrigger newReportTrigger = null;
        private TimeSpanThrottle throttle;
        #endregion

        #region Properties
        protected CsvHealthReporterConfiguration Configuration { get; private set; }
        internal HealthReportLevel LogLevel { get; private set; } = HealthReportLevel.Error;
        internal StreamWriter StreamWriter;
        #endregion

        #region Constructors
        public CsvHealthReporter(CsvHealthReporterConfiguration configuration)
        {
            Initialize(configuration);
        }

        /// <summary>
        /// Create a CsvHealthReporter with configuration.
        /// </summary>
        /// <param name="configuration">CsvHealthReporter configuration.</param>
        /// <param name="streamWriter">When provided, used to overwrite the stream writer created by the configuration.</param>
        public CsvHealthReporter(IConfiguration configuration)
        {
            Initialize(configuration);
        }

        internal CsvHealthReporter(CsvHealthReporterConfiguration configuration, INewReportTrigger newReportTrigger)
        {
            Validation.Requires.NotNull(newReportTrigger, nameof(newReportTrigger));
            Initialize(configuration, newReportTrigger);
        }

        /// <summary>
        /// Create a CsvHealthReporter with path to configuration file.
        /// </summary>
        /// <param name="configurationFilePath">CsvHealthReporter configuration file path.</param>
        /// <param name="streamWriter">When provided, used to overwrite the stream writer created by the configuration.</param>
        public CsvHealthReporter(string configurationFilePath)
        {
            Validation.Requires.NotNullOrWhiteSpace(configurationFilePath, nameof(configurationFilePath));

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false).Build();

            Initialize(configuration.GetSection("healthReporter"));
        }
        #endregion

        #region Methods
        public virtual string GetReportFileName()
        {
            return $"{this.Configuration.LogFilePrefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}.csv";
        }

        private void Initialize(CsvHealthReporterConfiguration configuration, INewReportTrigger newReportTrigger = null)
        {
            // Prepare the configuration, set default values, handle invalid values.
            ProcessConfiguration(configuration);

            // Hook up the mid-night stream writer updater.
            this.newReportTrigger = newReportTrigger ?? UtcMidnightNotifier.Instance;
            this.newReportTrigger.Triggered += RequestNewHealthReport;
        }

        private void ProcessConfiguration(CsvHealthReporterConfiguration configuration)
        {
            this.Configuration = configuration;

            if (configuration.ThrottleTimeSpan == null || !configuration.ThrottleTimeSpan.HasValue)
            {
                configuration.ThrottleTimeSpan = 0;
                this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(0));
                // Force to report error until LogLevel is set by configuration.
                ReportProblem($"{nameof(configuration.ThrottleTimeSpan)} is not specified in configuration file. Fall back to default value: {this.Configuration.ThrottleTimeSpan.Value}");
            }
            else
            {
                this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(configuration.ThrottleTimeSpan.Value));
            }

            string logLevelString = this.Configuration?.MinReportLevel;
            HealthReportLevel logLevel;
            if (Enum.TryParse(logLevelString, out logLevel))
            {
                LogLevel = logLevel;
            }
            else
            {
                // The severity has to be at least the same as the default level of error.
                ReportProblem($"Failed to parse log level. Please check the value of: {logLevelString}.", TraceTag);
            }

            // Set default value for HealthReport file prefix
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFilePrefix))
            {
                this.Configuration.LogFilePrefix = DefaultHealthReporterPrefix;
                ReportWarning($"{nameof(this.Configuration.LogFilePrefix)} is not specified in configuration file. Fall back to default value: {this.Configuration.LogFilePrefix}", TraceTag);
            }

            // Set default value for health reports
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFileFolder))
            {
                this.Configuration.LogFileFolder = ".";
            }
        }

        /// <summary>
        /// Start to write the health reports. Note: Avoid calling this from the reporter constructor 
        /// because CreateStreamWriter calls into virtual mehtod of GetReportFileName().
        /// </summary>
        /// <returns>Returns true when the reporter is activated.</returns>
        public bool Activate()
        {
            try
            {
                StreamWriter = CreateStreamWriter();
                // Start the consumer of the report items in the collection.
                Task.Run(() =>
                {
                    // Create the stream writer
                    foreach (string text in this.reportCollection.GetConsumingEnumerable())
                    {
                        if (this.newStreamRequested)
                        {
                            try
                            {
                                this.StreamWriter = CreateStreamWriter();
                            }
                            finally
                            {
                                this.newStreamRequested = false;
                            }
                        }
                        this.StreamWriter.WriteLine(text);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create the stream writer for the health reporter.
        /// </summary>
        /// <returns></returns>
        internal virtual StreamWriter CreateStreamWriter()
        {
            string logFilePath = GetReportFileName();
            string logFileFolder = Configuration.LogFileFolder;
            logFileFolder = Path.GetFullPath(logFileFolder);

            // Get the full path.
            logFilePath = Path.Combine(logFileFolder, logFilePath);

            // Create the folder if not exist.
            if (!Directory.Exists(logFileFolder))
            {
                Directory.CreateDirectory(logFileFolder);
            }

            if (TryCreateFileStream(logFilePath))
            {
                if (this.fileStream != null)
                {
                    if (this.StreamWriter != null)
                    {
                        this.StreamWriter.Flush();
                        this.StreamWriter.Dispose();
                        this.StreamWriter = null;
                    }
                    return new StreamWriter(this.fileStream, Encoding.UTF8);
                }
            }
            return null;
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
                // Do not dispose the existing FileStream yet to avoid exception on flushing the StreamWriter.
                this.fileStream = new FileStream(fullFilePath, FileMode.Append);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shot.
                fullFilePath = $"{Path.GetFileNameWithoutExtension(fullFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(fullFilePath)}";
                fileStream = new FileStream(fullFilePath, FileMode.Append);
            }
            return true;
        }

        private void RequestNewHealthReport(object sender, EventArgs e)
        {
            this.newStreamRequested = true;
        }

        private void Initialize(IConfiguration configuration, INewReportTrigger newReportTrigger = null)
        {
            CsvHealthReporterConfiguration boundConfiguration = new CsvHealthReporterConfiguration();
            configuration.Bind(boundConfiguration);

            Initialize(boundConfiguration, newReportTrigger);
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
            Debug.Assert(this.throttle != null);
            this.throttle.Execute(() =>
            {
                WriteLine(level, text, context);
            });
        }

        private void WriteLine(HealthReportLevel level, string text, string context = null)
        {
            try
            {
                if (level < LogLevel)
                {
                    return;
                }
                context = context ?? string.Empty;
                string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
                // Verified string concatenation has better performance than format, interpolation, String.Join or StringBuilder here.
                string message = timestamp + ',' + EscapeComma(context) + ',' + level + ',' + EscapeComma(text);
                WriteLine(message);
            }
            catch
            {
                // Suppress exception to prevent HealthReporter from crashing its consumer
            }
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
            this.reportCollection.Add(text);
        }

        public void Dispose()
        {
            if (this.StreamWriter != null)
            {
                StreamWriter.Dispose();
                StreamWriter = null;
            }

            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }

            if (newReportTrigger != null)
            {
                this.newReportTrigger.Triggered -= RequestNewHealthReport;
                this.newReportTrigger = null;
            }
        }
        #endregion
    }
}
