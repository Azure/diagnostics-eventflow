// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Validation;

#if NET451
using System.Web;
#endif
#if NETSTANDARD1_6
using System.Reflection;
#endif

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

        private static class FileSuffix
        {
            public const string Current = "current";
            public const string Last = "last";
        }

        #region Constants
        internal const string DefaultLogFilePrefix = "HealthReport";
        private const int DefaultThrottlingPeriodMsec = 0;
        #endregion

        #region Fields
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private static readonly TimeSpan DisposalTimeout = TimeSpan.FromSeconds(10);

        private bool disposed = false;

        private bool newStreamRequested = false;
        private BlockingCollection<string> reportCollection;
        private INewReportFileTrigger newReportFileTrigger = null;
        private TimeSpanThrottle throttle;
        private Task writingTask;
        private HealthReportLevel minReportLevel;
        private long singleLogFileMaximumSizeInBytes;

        private DateTime flushTime;
        private int flushPeriodMsec = 5000;
        private FileStream fileStream;
        internal StreamWriter StreamWriter;
        #endregion

        #region Properties
        protected CsvHealthReporterConfiguration Configuration { get; private set; }
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
        internal CsvHealthReporter(
            CsvHealthReporterConfiguration configuration,
            INewReportFileTrigger newReportTrigger = null,
            int flushPeriodMsec = 5000)
        {
            this.flushPeriodMsec = flushPeriodMsec;
            Initialize(configuration, newReportTrigger);
        }
        #endregion

        #region Public Methods
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
            this.flushTime = DateTime.Now.AddMilliseconds(this.flushPeriodMsec);

            // Start the consumer of the report items in the collection.
            this.writingTask = Task.Run(() => ConsumeCollectedData());
        }

        /// <summary>
        /// Consumes data from the data collection.
        /// </summary>
        private void ConsumeCollectedData()
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

                if (DateTime.Now >= this.flushTime)
                {

                    this.StreamWriter.Flush();

                    // Check if the file limit is exceeded.
                    if (this.StreamWriter.BaseStream.Position > this.singleLogFileMaximumSizeInBytes)
                    {
                        this.newStreamRequested = true;
                    }

                    this.flushTime = DateTime.Now.AddMilliseconds(this.flushPeriodMsec);
                }
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
        }

        public virtual string GetReportFileName(string suffix = null)
        {
            string fileName = $"{this.Configuration.LogFilePrefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}";
            if (!string.IsNullOrEmpty(suffix))
            {
                fileName += "_" + suffix;
            }
            fileName += ".csv";
            return fileName;
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

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Protected Methods
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

                try
                {
                    // Make sure that when Dispose() returns, the reporter is fully disposed (streams are closed etc.)
                    this.writingTask?.Wait(DisposalTimeout);
                }
                catch
                {
                    // We are reporting writing task errors from ReportXxx() methods, to no need to pass the task exception up here.
                }
            }

        }
        #endregion

        #region Private or internal methods
        /// <summary>
        /// Create the stream writer for the health reporter.
        /// </summary>
        /// <returns></returns>
        internal virtual void SetNewStreamWriter()
        {
            string logFileName = GetReportFileName(FileSuffix.Current);
            string logFileFolder = Path.GetFullPath(this.Configuration.LogFileFolder);
            if (!Directory.Exists(logFileFolder))
            {
                Directory.CreateDirectory(logFileFolder);
            }
            string logFilePath = Path.Combine(logFileFolder, logFileName);

            // Rotate the log file when needed
            if (File.Exists(logFilePath))
            {
                string rotateFilePath = Path.Combine(logFileFolder, GetReportFileName(FileSuffix.Last));

                // Making sure writing to current stream flushed and paused before renaming the log files.
                FinishCurrentStream();
                if (File.Exists(rotateFilePath))
                {
                    File.Delete(rotateFilePath);
                }
                File.Move(logFilePath, rotateFilePath);
            }

            // Do not update file stream or stream writer when targeting the same path.
            if (this.fileStream != null &&
                this.StreamWriter != null &&
                Path.GetFullPath(this.fileStream.Name).Equals(logFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Flush the current stream.
            FinishCurrentStream();

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
                string originalFilePath = logFilePath;
                logFileName = $"{Path.GetFileNameWithoutExtension(logFileName)}_{Path.GetRandomFileName()}{Path.GetExtension(logFileName)}";
                logFilePath = Path.Combine(logFileFolder, logFileName);
                this.fileStream = new FileStream(logFilePath, FileMode.Append);

                ReportWarning($"IOExcepion happened for the LogFilePath: {originalFilePath}. Use new path: {logFilePath}", TraceTag);
            }

            this.StreamWriter = new StreamWriter(this.fileStream, Encoding.UTF8);
        }

        private void FinishCurrentStream()
        {
            if (this.StreamWriter != null)
            {
                this.StreamWriter.Flush();
                this.StreamWriter.Dispose();
                this.StreamWriter = null;
            }
        }

        private void Initialize(CsvHealthReporterConfiguration configuration, INewReportFileTrigger newReportTrigger = null)
        {
            Requires.NotNull(configuration, nameof(configuration));

            this.reportCollection = new BlockingCollection<string>();

            // Prepare the configuration, set default values, handle invalid values.
            this.Configuration = configuration;

            // Create a default throttle.
            this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(DefaultThrottlingPeriodMsec));

            // Set the file size for csv health report. Minimum is 1MB. Default is 8192MB.
            this.singleLogFileMaximumSizeInBytes = (long)(configuration.SingleLogFileMaximumSizeInMBytes > 0 ? configuration.SingleLogFileMaximumSizeInMBytes : 8192) * 1024 * 1024;

            // Set default value for retention days for the logs. Minimum value is 1.
            if (configuration.RententionLogsInDays <= 0)
            {
                configuration.RententionLogsInDays = 30;
            }

            HealthReportLevel logLevel;
            string logLevelString = this.Configuration.MinReportLevel;
            if (string.IsNullOrWhiteSpace(logLevelString))
            {
                this.minReportLevel = HealthReportLevel.Error;
            }
            else if (Enum.TryParse(logLevelString, out logLevel))
            {
                this.minReportLevel = logLevel;
            }
            else
            {
                this.minReportLevel = HealthReportLevel.Error;
                // The severity has to be at least the same as the default level of error.
                ReportProblem($"Failed to parse log level. Please check the value of: {logLevelString}. Falling back to default value: {this.minReportLevel.ToString()}", TraceTag);
            }
            this.Configuration.MinReportLevel = this.minReportLevel.ToString();

            if (this.Configuration.ThrottlingPeriodMsec == null || !this.Configuration.ThrottlingPeriodMsec.HasValue)
            {
                this.Configuration.ThrottlingPeriodMsec = DefaultThrottlingPeriodMsec;
                ReportHealthy($"{nameof(this.Configuration.ThrottlingPeriodMsec)} is not specified. Falling back to default value: {this.Configuration.ThrottlingPeriodMsec}.", TraceTag);
            }
            else if (this.Configuration.ThrottlingPeriodMsec.Value == DefaultThrottlingPeriodMsec)
            {
                // Keep using the default throttle created before.
            }
            else if (this.Configuration.ThrottlingPeriodMsec.Value < 0)
            {
                ReportWarning($"{nameof(this.Configuration.ThrottlingPeriodMsec)}: {this.Configuration.ThrottlingPeriodMsec.Value} specified in the configuration file is invalid. Falling back to default value: {DefaultThrottlingPeriodMsec}", TraceTag);
                this.Configuration.ThrottlingPeriodMsec = DefaultThrottlingPeriodMsec;
            }
            else if (this.Configuration.ThrottlingPeriodMsec.Value >= 0)
            {
                this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(this.Configuration.ThrottlingPeriodMsec.Value));
            }

            // Set default value for health report file prefix
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFilePrefix))
            {
                this.Configuration.LogFilePrefix = DefaultLogFilePrefix;
                ReportHealthy($"{nameof(this.Configuration.LogFilePrefix)} is not specified in configuration file. Falling back to default value: {this.Configuration.LogFilePrefix}", TraceTag);
            }

            // Set target folder for health report files.
            if (string.IsNullOrWhiteSpace(this.Configuration.LogFileFolder))
            {
                this.Configuration.LogFileFolder = ".";
                ReportHealthy($"{nameof(this.Configuration.LogFileFolder)} is not specified in configuration file. Falling back to default value: {this.Configuration.LogFileFolder}", TraceTag);
            }
            ProcessLogFileFolder();

            this.newReportFileTrigger = newReportTrigger ?? UtcMidnightNotifier.Instance;
            this.newReportFileTrigger.NewReportFileRequested += OnNewReportFileRequested;

            // Clean up existing logging files per rentention policy
            CleanupExistingLogs();
        }

        private void VerifyObjectIsNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(TraceTag);
            }
        }

        /// <summary>
        /// For ASP.NET (non-core): Default path is 'App_Data'. The relative path would relative to 'App_Data'.
        /// For other projects(including ASP.NET Core), relative path would relative to the assembly folder.
        /// </summary>
        private void ProcessLogFileFolder()
        {
            this.Configuration.LogFileFolder = Environment.ExpandEnvironmentVariables(this.Configuration.LogFileFolder);

            if (Path.IsPathRooted(this.Configuration.LogFileFolder))
            {
                return;
            }

            string basePath;
#if NET451
            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                basePath = HttpContext.Current.Server.MapPath("~/App_Data");
            }
            else
            {
                basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(this.GetType()).Location);
            }
#elif NETSTANDARD1_6
            basePath = Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location);
#endif
            this.Configuration.LogFileFolder = Path.Combine(basePath, this.Configuration.LogFileFolder);
        }

        private void OnNewReportFileRequested(object sender, EventArgs e)
        {
            this.newStreamRequested = true;

            // Clean up existing logging files per rentention policy
            CleanupExistingLogs();
        }

        private void CleanupExistingLogs()
        {
            DateTime criteria = DateTime.UtcNow.Date.AddDays(-Configuration.RententionLogsInDays + 1);
            DirectoryInfo logFolder = new DirectoryInfo(Configuration.LogFileFolder);
            IEnumerable<FileInfo> files = (
                logFolder.EnumerateFiles($"{Configuration.LogFilePrefix}_????????_{FileSuffix.Current}.csv", SearchOption.TopDirectoryOnly)).Union(
                logFolder.EnumerateFiles($"{Configuration.LogFilePrefix}_????????_{FileSuffix.Last}.csv", SearchOption.TopDirectoryOnly));

            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.CreationTimeUtc < criteria)
                    {
                        file.Delete();
                    }
                }
                catch (Exception ex)
                {
                    ReportWarning($"Fail to remove logging file. Details: {ex.Message}");
                }
            }
        }

        private void ReportText(HealthReportLevel level, string text, string context = null)
        {
            if (level < this.minReportLevel)
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
        internal string EscapeComma(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text.Contains(","))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return text;
            }
        }
        #endregion
    }
}
