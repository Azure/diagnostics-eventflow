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
            // Header
            ReportText(HealthReportLevels.Message, "== Message ==", "== Category ==");
        }


        public void ReportHealthy()
        {
            ReportMessage("Health.");
        }

        public void ReportMessage(string description, string category = null)
        {
            ReportText(HealthReportLevels.Message, description, category);
        }

        public void ReportProblem(string problemDescription, string category = null)
        {
            ReportText(HealthReportLevels.Error, problemDescription, category);
        }

        public void ReportWarning(string description, string category = null)
        {
            ReportText(HealthReportLevels.Warning, description, category);
        }

        private void ReportText(HealthReportLevels level, string text, string category = null)
        {
            if (level < _logLevel)
            {
                return;
            }

            category = category ?? "Default";

            string timestamp = DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
            string message = $"{timestamp},{category},{level},{text}";
            try
            {
                _locker.EnterWriteLock();
                string logFileName = this._fileName;
                using (FileStream fs = new FileStream(logFileName, FileMode.Append))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(message);
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
    }
}
