using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.Extensions.Diagnostics.Core.Implementations
{
    public class CsvFileHealthReport : IHealthReporter
    {
        #region Fields
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private HealthReportLevels _logLevel;
        private string _fileName;
        private FileStream _fileStream;
        #endregion

        private static Lazy<CsvFileHealthReport> defaultInstance = new Lazy<CsvFileHealthReport>(() =>
        {
            return new CsvFileHealthReport("HealthReport.csv", HealthReportLevels.Error);
        });

        public static CsvFileHealthReport Default
        {
            get
            {
                return defaultInstance.Value;
            }
        }

        public CsvFileHealthReport(string fileName, HealthReportLevels logLevel)
        {
            Validation.Requires.NotNullOrWhiteSpace(fileName, nameof(fileName));
            _fileName = fileName;
            _logLevel = logLevel;
            try
            {
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
            }
            catch (Exception)
            {
                // Crash prevention
            }

            // Header
            WriteLine(_fileName, "Timestamp,Tag,Level,Message");
        }


        public void ReportHealthy()
        {
            ReportMessage("Health.");
        }

        public void ReportMessage(string description, string tag = null)
        {
            ReportText(HealthReportLevels.Message, description, tag);
        }

        public void ReportProblem(string problemDescription, string tag = null)
        {
            ReportText(HealthReportLevels.Error, problemDescription, tag);
        }

        public void ReportWarning(string description, string tag = null)
        {
            ReportText(HealthReportLevels.Warning, description, tag);
        }

        private void ReportText(HealthReportLevels level, string text, string tag = null)
        {
            if (level < _logLevel)
            {
                return;
            }
            tag = tag ?? "Default";
            string timestamp = DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
            string message = $"{timestamp},{tag},{level},{text}";
            WriteLine(_fileName, message);
        }

        private void WriteLine(string fileName, string text)
        {
            try
            {
                _locker.EnterWriteLock();
                using (StreamWriter sw = new StreamWriter(_fileStream, Encoding.UTF8, 512, leaveOpen: true))
                {
                    sw.WriteLine(text);
                }
            }
            catch
            {
                // Crash prevention.
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            if (_fileStream != null)
            {
                _fileStream.Dispose();
            }
        }
    }
}
