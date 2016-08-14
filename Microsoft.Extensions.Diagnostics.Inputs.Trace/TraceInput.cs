using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Inputs
{
    public class TraceInput : DefaultTraceListener, IObservable<EventData>, IDisposable
    {
        public static readonly string TraceTag = nameof(TraceInput);

        private SimpleSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;
        public TraceInput(IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
            this.healthReporter.ReportMessage($"{nameof(TraceInput)} initializing.", TraceTag);

            subject = new SimpleSubject<EventData>();

            // TODO: Understand configure and apply event filters.

            Trace.Listeners.Add(this);
            this.healthReporter.ReportMessage($"{nameof(TraceInput)} initialized.", TraceTag);
        }

        public override void WriteLine(string message)
        {
            try
            {
                this.healthReporter.ReportMessage("Start writing line of message.", TraceTag);
                EventData data = new EventData()
                {
                    ProviderName = nameof(TraceInput),
                    Message = message
                };
                this.subject.OnNext(data);
                this.healthReporter.ReportMessage("Finish writing line of message.", TraceTag);
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportProblem($"Fail to write message. Error: {ex.Message}", TraceTag);
            }
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            try
            {
                healthReporter.ReportMessage("Subscribing", TraceTag);
                return subject.Subscribe(observer);
            }
            finally
            {
                healthReporter.ReportMessage("Subscribed", TraceTag);
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.healthReporter.ReportMessage("Disposing.", TraceTag);
            base.Dispose(disposing);
            subject.Dispose();
            this.healthReporter.ReportMessage("Disposed.", TraceTag);
        }
    }
}
