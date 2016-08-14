using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.Extensions.Diagnostics.Core.Implementations
{
    public class CsvFileHealthReport : IHealthReporter
    {
        private static readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private static Lazy<CsvFileHealthReport> defaultInstance = new Lazy<CsvFileHealthReport>(() =>
        {
            return new CsvFileHealthReport("HealthReport.csv");
        });

        public static CsvFileHealthReport Default
        {
            get
            {
                return defaultInstance.Value;
            }
        }

        private string fileName;
        public CsvFileHealthReport(string fileName)
        {
            Validation.Requires.NotNullOrWhiteSpace(fileName, nameof(fileName));
            this.fileName = fileName;
            // Header
            ReportText(Level.Message, "== Message ==", "== Category ==");
        }

        private enum Level
        {
            Message,
            Warning,
            Error
        }

        public void ReportHealthy()
        {
            ReportMessage("Health.");
        }

        public void ReportMessage(string description, string category = null)
        {
            ReportText(Level.Message, description, category);
        }

        public void ReportProblem(string problemDescription, string category = null)
        {
            ReportText(Level.Error, problemDescription, category);
        }

        public void ReportWarning(string description, string category = null)
        {
            ReportText(Level.Warning, description, category);
        }

        private void ReportText(Level level, string text, string category = null)
        {
            category = category ?? "Default";

            string timestamp = DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
            string message = $"{timestamp},{category},{level},{text}";
            try
            {
                _locker.EnterWriteLock();
                string logFileName = this.fileName;
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
