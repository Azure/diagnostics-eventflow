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
    public class CsvFileHealthReporter : IHealthReporter
    {
        #region Fields
        private static readonly string TraceTag = nameof(CsvFileHealthReporter);
        private readonly object _locker = new object();
        private HealthReportLevel _logLevel;
        private string _logFilePath;
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        #endregion

        public CsvFileHealthReporter(string configurationFilePath)
        {
            Validation.Requires.NotNullOrWhiteSpace(configurationFilePath, nameof(configurationFilePath));

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: false);
            IConfiguration configuration = builder.Build();

            _logFilePath = configuration["healthReporter:logFilePath"];
            string logLevelString = configuration["healthReporter:logLevel"] ?? "Warning";
            string csvFileHealthReporterWarning = null;
            if (!Enum.TryParse(logLevelString, out _logLevel))
            {
                csvFileHealthReporterWarning = $"Log level parse fail. Please check the value of: {logLevelString}.";
                _logLevel = HealthReportLevel.Error;
            }

            try
            {
                _fileStream = new FileStream(_logFilePath, FileMode.Append);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shoot
                _logFilePath = $"{Path.GetFileNameWithoutExtension(_logFilePath)}_{Path.GetRandomFileName()}{Path.GetExtension(_logFilePath)}";
                _fileStream = new FileStream(_logFilePath, FileMode.Append);
            }

            _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

            if (!string.IsNullOrEmpty(csvFileHealthReporterWarning))
            {
                ReportProblem(csvFileHealthReporterWarning, TraceTag);
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
                if (level < _logLevel)
                {
                    return;
                }
                context = context ?? string.Empty;
                string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
                string message = $"{timestamp},{context.Replace(',', '_')},{level},{text}";
                WriteLine(_logFilePath, message);
            }
            catch
            {
                // Crash prevention from with in CsvFileHealthReporter.
                // Reason: Not to carsh the main pipeline event if the health report doesn't work.
            }
        }

        private void WriteLine(string fileName, string text)
        {
            lock (_locker)
            {
                _streamWriter.WriteLine(text);
            }
        }

        public void Dispose()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Dispose();
            }

            if (_fileStream != null)
            {
                _fileStream.Dispose();
            }
        }
    }
}
