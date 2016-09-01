using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Consumers.HealthReporterBuster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer timer;
        CsvHealthReporter reporter;
        volatile int hit = 0;
        DispatcherTimer hitReporter = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.Exit += Current_Exit;
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            if (reporter != null)
            {
                reporter.Dispose();
            }
        }

        private void btnStart_Clicked(object sender, RoutedEventArgs e)
        {
            IConfiguration section = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() {
               { "LogFileFolder", tbLogFileFolder.Text.Trim() },
               { "LogFilePrefix", tbLogFilePrefix.Text.Trim()},
               { "MinReportLevel", tbMinReportLevel.Text.Trim() }
            }).Build();

            reporter = new CustomHealthReporter(section);
            int intervalInMs;
            if (!int.TryParse(tbMessageInterval.Text, out intervalInMs))
            {
                intervalInMs = 500;
            }

            timer = new Timer(state =>
            {
                (state as CsvHealthReporter)?.ReportHealthy(DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern), "HealthReporterBuster");
                hit++;
            }, reporter, 0, intervalInMs);

            hitReporter.Interval = TimeSpan.FromMilliseconds(500);
            hitReporter.Tick += HitReporter_Tick;
            hitReporter.IsEnabled = true;

            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnSwitch.IsEnabled = true;
        }

        private void HitReporter_Tick(object sender, EventArgs e)
        {
            tbHit.Text = hit.ToString();
        }

        private void btnSwitch_Clicked(object sender, RoutedEventArgs e)
        {
            reporter.BuildNewStreamWriter();
        }

        private void btnStop_Clicked(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            if (reporter != null)
            {
                reporter.Dispose();
            }

            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnSwitch.IsEnabled = false;
        }
    }
}
