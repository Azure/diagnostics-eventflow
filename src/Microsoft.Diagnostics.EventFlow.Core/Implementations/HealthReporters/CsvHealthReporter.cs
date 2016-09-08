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
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    public class CsvHealthReporter : IHealthReporter, IRequireActivation
    {
        internal enum HealthReportLevel
        {
            Message,
            Warning,
            Error
        }

        #region Constants
        internal const string DefaultLogFilePrefix = "HealthReport";
        #endregion

        #region Fields
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private BlockingCollection<string> reportCollection;
        private FileStream fileStream;
        private bool newStreamRequested = false;
        private INewReportFileTrigger newReportFileTrigger = null;
        private TimeSpanThrottle throttle;
        private Task writingTask;
        private bool disposed = false;
        #endregion

        #region Properties
        protected CsvHealthReporterConfiguration Configuration { get; private set; }
        internal HealthReportLevel LogLevel { get; private set; } = HealthReportLevel.Error;
        internal StreamWriter StreamWriter;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a CsvHealthReporter with configuration.
        /// </summary>
        /// <param name="configuration">CsvHealthReporter configuration.</param>
        public CsvHealthReporter(CsvHealthReporterConfiguration configuration)
        {
            Initialize(configuration);
        }

        /// <summary>
        /// Create a CsvHealthReporter with configuration.
        /// </summary>
        /// <param name="configuration">CsvHealthReporter configuration.</param>
        public CsvHealthReporter(IConfiguration configuration)
            : this(configuration.ToCsvHealthReporterConfiguration())
        {
        }

        /// <summary>
        /// Create a CsvHealthReporter with path to configuration file.
        /// </summary>
        /// <param name="configurationFilePath">CsvHealthReporter configuration file path.</param>
        public CsvHealthReporter(string configurationFilePath)
        {
            Requires.NotNullOrWhiteSpace(configurationFilePath, nameof(configurationFilePath));

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false).Build();

            Initialize((configuration.GetSection("healthReporter")).ToCsvHealthReporterConfiguration());
        }

        // Constructor for testing purpose.
        internal CsvHealthReporter(CsvHealthReporterConfiguration configuration, INewReportFileTrigger newReportTrigger)
        {
            Requires.NotNull(newReportTrigger, nameof(newReportTrigger));
            Initialize(configuration, newReportTrigger);
        }
        #endregion

        #region Methods
        public virtual string GetReportFileName()
        {
            return $"{this.Configuration.LogFilePrefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}.csv";
        }

        private void Initialize(CsvHealthReporterConfiguration configuration, INewReportFileTrigger newReportTrigger = null)
        {
            this.reportCollection = new BlockingCollection<string>();

            // Prepare the configuration, set default values, handle invalid values.
            this.Configuration = configuration;

            // Create a default throttle
            configuration.ThrottlingPeriodMsec = 0;
            this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(0));

            string logLevelString = this.Configuration?.MinReportLevel;
            HealthReportLevel logLevel;
            if (Enum.TryParse(logLevelString, out logLevel))
            {
                LogLevel = logLevel;
            }
            else
            {
                this.Configuration.MinReportLevel = this.LogLevel.ToString();
                // The severity has to be at least the same as the default level of error.
                ReportProblem($"Failed to parse log level. Please check the value of: {logLevelString}. Falling back to default value: {this.Configuration.MinReportLevel}", TraceTag);
            }

            if (configuration.ThrottlingPeriodMsec == null || !configuration.ThrottlingPeriodMsec.HasValue || configuration.ThrottlingPeriodMsec.Value < 0)
            {
                ReportWarning($"{nameof(configuration.ThrottlingPeriodMsec)} is not specified or the value is invalid in configuration file. Falling back to default value: {this.Configuration.ThrottlingPeriodMsec.Value}", TraceTag);
                // Keep using the default throttle.
            }
            else if (configuration.ThrottlingPeriodMsec.Value > 0)
            {
                this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(configuration.ThrottlingPeriodMsec.Value));
            }

            // Set default value for HealthReport file prefix
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFilePrefix))
            {
                this.Configuration.LogFilePrefix = DefaultLogFilePrefix;
                ReportWarning($"{nameof(this.Configuration.LogFilePrefix)} is not specified in configuration file. Falling back to default value: {this.Configuration.LogFilePrefix}", TraceTag);
            }

            // Set default value for health reports
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFileFolder))
            {
                this.Configuration.LogFileFolder = ".";
                ReportWarning($"{nameof(this.Configuration.LogFileFolder)} is not specified in configuration file. Falling back to default value: {this.Configuration.LogFileFolder}", TraceTag);

            }

            this.newReportFileTrigger = newReportTrigger ?? UtcMidnightNotifier.Instance;
            this.newReportFileTrigger.NewReportFileRequested += OnNewReportFileRequested;
        }

        /// <summary>
        /// Starts to write the health reports.
        /// </summary>
        /// <remarks>
        /// Avoid calling this from the reporter constructor 
        /// because CreateStreamWriter calls into virtual mehtod of GetReportFileName().
        /// </remarks>
        public void Activate()
        {
            VerifyObjectIsNotDisposed();
            SetNewStreamWriter();
            Assumes.NotNull(StreamWriter);

            // Start the consumer of the report items in the collection.
            this.writingTask = Task.Run(() =>
            {
                foreach (string text in this.reportCollection.GetConsumingEnumerable())
                {
                    if (this.newStreamRequested)
                    {
                        try
                        {
                            SetNewStreamWriter();
                            Assumes.NotNull(StreamWriter);
                        }
                        finally
                        {
                            this.newStreamRequested = false;
                        }
                    }
                    this.StreamWriter.WriteLine(text);
                }

                Debug.Assert(this.reportCollection.IsAddingCompleted && this.reportCollection.IsCompleted);
                this.reportCollection.Dispose();
                this.reportCollection = null;

                if (this.StreamWriter != null)
                {
                    this.StreamWriter.Flush();
                    this.StreamWriter.Dispose();
                    this.StreamWriter = null;
                }

                if (this.fileStream != null)
                {
                    this.fileStream.Dispose();
                    this.fileStream = null;
                }
            });
        }

        private void VerifyObjectIsNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(TraceTag);
            }
        }

        /// <summary>
        /// Create the stream writer for the health reporter.
        /// </summary>
        /// <returns></returns>
        internal virtual void SetNewStreamWriter()
        {
            string logFilePath = GetReportFileName();
            string logFileFolder = Configuration.LogFileFolder;
            logFileFolder = Path.GetFullPath(logFileFolder);
            if (!Directory.Exists(logFileFolder))
            {
                Directory.CreateDirectory(logFileFolder);
            }
            logFilePath = Path.Combine(logFileFolder, logFilePath);

            // Do not update file stream or stream writer when targeting the same path.
            if (this.fileStream != null &&
                this.StreamWriter != null &&
                Path.GetFullPath(this.fileStream.Name).Equals(logFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Flush the current stream.
            if (this.StreamWriter != null)
            {
                this.StreamWriter.Flush();
                this.StreamWriter.Dispose();
                this.StreamWriter = null;
            }

            if (this.fileStream != null)
            {
                this.fileStream.Dispose();
                this.fileStream = null;
            }

            // Create a new stream writer
            try
            {
                this.fileStream = new FileStream(logFilePath, FileMode.Append);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shot.
                string original = logFilePath;
                logFilePath = $"{Path.GetFileNameWithoutExtension(logFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(logFilePath)}";
                this.fileStream = new FileStream(logFilePath, FileMode.Append);

                ReportWarning($"IOExcepion happened for the LogFilePath: {original}. Use new path: {logFilePath}", TraceTag);
            }

            this.StreamWriter = new StreamWriter(this.fileStream, Encoding.UTF8);
        }


        private void OnNewReportFileRequested(object sender, EventArgs e)
        {
            this.newStreamRequested = true;
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            VerifyObjectIsNotDisposed();
            ReportText(HealthReportLevel.Message, description, context);
        }

        public void ReportProblem(string description, string context = null)
        {
            VerifyObjectIsNotDisposed();
            ReportText(HealthReportLevel.Error, description, context);
        }

        public void ReportWarning(string description, string context = null)
        {
            VerifyObjectIsNotDisposed();
            ReportText(HealthReportLevel.Warning, description, context);
        }

        private void ReportText(HealthReportLevel level, string text, string context = null)
        {
            if (level < LogLevel)
            {
                return;
            }

            Debug.Assert(this.throttle != null);
            this.throttle.Execute(() =>
            {
                WriteLine(level, text, context);
            });
        }

        private void WriteLine(HealthReportLevel level, string text, string context = null)
        {
            if (this.writingTask != null && this.writingTask.IsFaulted)
            {
                throw new InvalidOperationException("Failed to write health report.", writingTask.Exception);
            }

            context = context ?? string.Empty;
            string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
            // Verified string concatenation has better performance than format, interpolation, String.Join or StringBuilder here.
            string message = timestamp + ',' + EscapeComma(context) + ',' + level + ',' + EscapeComma(text);
            this.reportCollection.Add(message);
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

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (disposing)
            {
                if (this.newReportFileTrigger != null)
                {
                    this.newReportFileTrigger.NewReportFileRequested -= OnNewReportFileRequested;
                    this.newReportFileTrigger = null;
                }

                // Mark report collection as complete adding. When the collection is empty, it will dispose the stream writers.
                this.reportCollection.CompleteAdding();
            }

        }
        #endregion
    }
}
