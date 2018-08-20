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
#else
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
            public const string Last = "last";
        }

        
        internal const string DefaultLogFilePrefix = "HealthReport";
        private const int DefaultThrottlingPeriodMsec = 0;

        
        private static readonly string TraceTag = nameof(CsvHealthReporter);
        private static readonly TimeSpan DisposalTimeout = TimeSpan.FromSeconds(10);

        private bool disposed = false;

        private bool newStreamRequested = false;
        private BlockingCollection<string> reportCollection;
        private INewReportFileTrigger newReportFileTrigger = null;
        private TimeSpanThrottle throttle;
        private Task writingTask;
        private HealthReportLevel minReportLevel;
        private Action<HealthReportLevel, string, string> innerReportWriter;

        private DateTime flushTime;
        private int flushPeriodMsec = 5000;
        private Func<DateTime> getCurrentTime = () => DateTime.Now;
        private FileStream fileStream;
        internal StreamWriter StreamWriter;
        internal bool EnsureOutputCanBeSaved { get; private set; }
        internal long SingleLogFileMaximumSizeInBytes { get; private set; }

        
        protected CsvHealthReporterConfiguration Configuration { get; private set; }

        
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
            int flushPeriodMsec = 5000,
            Func<DateTime> currentTimeProvider = null)
        {
            this.flushPeriodMsec = flushPeriodMsec;
            if (currentTimeProvider != null)
            {
                this.getCurrentTime = currentTimeProvider;
            }
            Initialize(configuration, newReportTrigger);
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
            bool activated = true;
            string message = null;
            try
            {
                SetNewStreamWriter();
                if (StreamWriter == null)
                {
                    message = $"Fail to set new stream writer for {nameof(CsvHealthReporter)}.";
                    if (EnsureOutputCanBeSaved)
                    {
                        throw new InvalidOperationException(message);
                    }
                    activated = false;
                }
                this.flushTime = this.getCurrentTime().AddMilliseconds(this.flushPeriodMsec);

                // Start the consumer of the report items in the collection.
                this.writingTask = Task.Run(() => ConsumeCollectedData());

            }
            catch (UnauthorizedAccessException ex)
            {
                if (EnsureOutputCanBeSaved)
                {
                    throw;
                }
                message = ex.Message;
                activated = false;
            }

            if (!activated)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    // Unfortunately, when there is no permission to write a csv health report, there is no where we can report the error.
                    // Push the info to the debugger and hope for the best.
                    Debug.WriteLine($"Fail to create EventFlow health report. Details: {message}.");
                }
                // No report writer, no reports.
                this.innerReportWriter = null;
            }
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

                if (this.getCurrentTime() >= this.flushTime)
                {

                    this.StreamWriter.Flush();

                    // Check if the file limit is exceeded.
                    if (this.StreamWriter.BaseStream.Position > this.SingleLogFileMaximumSizeInBytes)
                    {
                        this.newStreamRequested = true;
                    }

                    this.flushTime = this.getCurrentTime().AddMilliseconds(this.flushPeriodMsec);
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

        public string RotateLogFile(string logFileFolder)
        {
            return RotateLogFileImp(logFileFolder, File.Exists, File.Delete, File.Move);
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            VerifyObjectIsNotDisposed();
            this.innerReportWriter?.Invoke(HealthReportLevel.Message, description, context);
        }

        public void ReportProblem(string description, string context = null)
        {
            VerifyObjectIsNotDisposed();
            this.innerReportWriter?.Invoke(HealthReportLevel.Error, description, context);
        }

        public void ReportWarning(string description, string context = null)
        {
            VerifyObjectIsNotDisposed();
            this.innerReportWriter?.Invoke(HealthReportLevel.Warning, description, context);
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

        
        /// <summary>
        /// Implementation for rotating the log file.
        /// </summary>
        /// <param name="logFileFolder">Log file folder.</param>
        /// <param name="fileExist">Method to check whether a file exists or not.</param>
        /// <param name="fileDelete">Method to delete a file.</param>
        /// <param name="fileMove">Method to move a file.</param>
        /// <returns></returns>
        internal virtual string RotateLogFileImp(string logFileFolder, Func<string, bool> fileExist, Action<string> fileDelete, Action<string, string> fileMove)
        {
            string fileName = $"{this.Configuration.LogFilePrefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}.csv";
            string logFilePath = Path.Combine(logFileFolder, fileName);

            // Rotate the log file when needed
            if (fileExist(logFilePath))
            {
                string rotateFilePath = Path.Combine(logFileFolder, $"{this.Configuration.LogFilePrefix}_{DateTime.UtcNow.Date.ToString("yyyyMMdd")}_{FileSuffix.Last}.csv");

                // Making sure writing to current stream flushed and paused before renaming the log files.
                FinishCurrentStream();
                if (fileExist(rotateFilePath))
                {
                    fileDelete(rotateFilePath);
                }
                fileMove(logFilePath, rotateFilePath);
            }
            return logFilePath;
        }

        /// <summary>
        /// Create the stream writer for the health reporter.
        /// </summary>
        /// <returns></returns>
        internal virtual void SetNewStreamWriter()
        {
            // Ensure Log folder exists
            string logFileFolder = Path.GetFullPath(this.Configuration.LogFileFolder);
            if (!Directory.Exists(logFileFolder))
            {
                Directory.CreateDirectory(logFileFolder);
            }

            string logFilePath = RotateLogFile(logFileFolder);

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
            CreateNewFileWriter(logFilePath);

            if (this.fileStream != null)
            {
                this.StreamWriter = new StreamWriter(this.fileStream, Encoding.UTF8);
            }
        }

        internal void CreateNewFileWriter(string logFilePath)
        {
            try
            {
                this.fileStream = this.CreateFileStream(logFilePath);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shot.
                string originalFilePath = logFilePath;
                string logFileName = $"{Path.GetFileNameWithoutExtension(logFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(logFilePath)}";
                string logFileFolder = Path.GetDirectoryName(logFilePath);
                logFilePath = Path.Combine(logFileFolder, logFileName);
                this.fileStream = new FileStream(logFilePath, FileMode.Append);

                ReportWarning($"IOExcepion happened for the LogFilePath: {originalFilePath}. Use new path: {logFilePath}", TraceTag);
            }
        }

        internal virtual FileStream CreateFileStream(string logFilePath)
        {
            return new FileStream(logFilePath, FileMode.Append);
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

            this.EnsureOutputCanBeSaved = configuration.EnsureOutputCanBeSaved;

            this.innerReportWriter = this.ReportText;

            // Prepare the configuration, set default values, handle invalid values.
            this.Configuration = configuration;

            // Create a default throttle.
            this.throttle = new TimeSpanThrottle(TimeSpan.FromMilliseconds(DefaultThrottlingPeriodMsec));

            // Set the file size for csv health report. Minimum is 1MB. Default is 8192MB.
            this.SingleLogFileMaximumSizeInBytes = (long)(configuration.SingleLogFileMaximumSizeInMBytes > 0 ? configuration.SingleLogFileMaximumSizeInMBytes : 8192) * 1024 * 1024;

            // Log retention days has a minimum of 1 day. Set to the default value of 30 days.
            if (configuration.LogRetentionInDays <= 0)
            {
                configuration.LogRetentionInDays = 30;
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

            // Clean up existing logging files per retention policy
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
                basePath = Directory.GetCurrentDirectory();
            }
#else
            basePath = Directory.GetCurrentDirectory();
#endif
            this.Configuration.LogFileFolder = Path.Combine(basePath, this.Configuration.LogFileFolder);
        }

        private void OnNewReportFileRequested(object sender, EventArgs e)
        {
            this.newStreamRequested = true;

            // Clean up existing logging files per retention policy
            CleanupExistingLogs();
        }

        internal void CleanupExistingLogs(Action<ILogFileInfo> cleaner = null)
        {
            cleaner = cleaner ?? ((fileInfo) => fileInfo.Delete());
            DateTime criteria = DateTime.UtcNow.Date.AddDays(-Configuration.LogRetentionInDays + 1);
            DirectoryInfo logFolder = new DirectoryInfo(Configuration.LogFileFolder);

            IEnumerable<ILogFileInfo> files = GetLogFiles(logFolder);

            foreach (ILogFileInfo file in files)
            {
                try
                {
                    if (file.CreationTimeUtc < criteria)
                    {
                        cleaner(file);
                    }
                }
                catch (Exception ex)
                {
                    ReportWarning($"Fail to remove logging file. Details: {ex.Message}");
                }
            }
        }

        internal virtual IEnumerable<ILogFileInfo> GetLogFiles(DirectoryInfo logFolder)
        {
            if (logFolder != null && logFolder.Exists)
            {
                return (
                    logFolder.EnumerateFiles($"{Configuration.LogFilePrefix}_????????.csv", SearchOption.TopDirectoryOnly)).Union(
                    logFolder.EnumerateFiles($"{Configuration.LogFilePrefix}_????????_{FileSuffix.Last}.csv", SearchOption.TopDirectoryOnly))
                    .Select(fileInfo => new FileInfoWrapper(fileInfo));
            }
            return Enumerable.Empty<FileInfoWrapper>();
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
    }
}
