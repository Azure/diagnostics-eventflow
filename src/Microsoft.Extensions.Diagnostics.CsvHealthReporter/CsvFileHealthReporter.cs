// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Extensions.Diagnostics.HealthReporters
{
    // TODO: Consider making this class singleton to avoid being intialized by end users.
    public class CsvFileHealthReporter : IHealthReporter
    {
        #region Fields
        private readonly object _locker = new object();
        private HealthReportLevel _logLevel;
        private string _fileName;
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        #endregion

        // TODO: Considering expose IConfiguration instead of HealthReportLevel when there is full configuration story.
        public CsvFileHealthReporter(string fileName, HealthReportLevel logLevel)
        {
            Validation.Requires.NotNullOrWhiteSpace(fileName, nameof(fileName));
            _fileName = fileName;
            _logLevel = logLevel;

            try
            {
                _fileStream = new FileStream(_fileName, FileMode.Append);
            }
            catch (IOException)
            {
                // In case file is locked by other process, give it another shoot
                _fileName = $"{Path.GetFileNameWithoutExtension(_fileName)}_{Path.GetRandomFileName()}{Path.GetExtension(_fileName)}";
                _fileStream = new FileStream(_fileName, FileMode.Append);
            }

            _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);
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
                string timestamp = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
                string message = $"{timestamp},{context.Replace(',', '_')},{level},{text}";
                WriteLine(_fileName, message);
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
