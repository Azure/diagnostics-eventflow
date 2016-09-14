// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class DiagnosticPipelineTests
    {
        [Fact]
        public void ConstructorShouldRequireHealthReport()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                DiagnosticPipeline pipeline = new DiagnosticPipeline(
                    null,
                    new List<TraceInput>(),
                    null,
                    new List<EventSink>());
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }

        [Fact]
        public async Task ShouldPassOneInputToOneOutput()
        {
            // Setup
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            Mock<IOutput> mockOutput = new Mock<IOutput>();
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000
            };

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(mockOutput.Object, null) },
                settings))
            {

                // Execrise
                unitTestInput.SendMessage("Test information");

                // Give it a small delay to let the pipeline process through.
                await Task.Delay(100);

                // Verify
                mockOutput.Verify(o => o.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                    It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task UsableIfBufferOverflowOccurs()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            UnitTestOutput unitTestOutput = new UnitTestOutput();
            unitTestOutput.SendEventsDelay = TimeSpan.FromMilliseconds(100);
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000,
                PipelineBufferSize = 1,
                MaxConcurrency = 1,
                MaxEventBatchSize = 1
            };
            const int TestBatchSize = 6;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(unitTestOutput, null) },
                settings))
            {
                // Six events in quick succession will cause a buffer overflow 
                // (we have a buffer of 1 set for the pipeline, but the pipeline has 3 blocks, so the actual buffer space is 3).
                // There should be no ill effects from that on the input--not catching any exceptions. 
                for (int i = 0; i < TestBatchSize; i++)
                {
                    unitTestInput.SendMessage($"Message {i}");
                }

                // Wait for the pipeline to drain. 
                await Task.Delay(TimeSpan.FromMilliseconds(TestBatchSize * (unitTestOutput.SendEventsDelay.TotalMilliseconds + settings.MaxBatchDelayMsec)));

                // At least one message should get to the output.
                Assert.True(unitTestOutput.CallCount > 0);

                // We should get a warning about throttling
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.Is<string>(s => s == EventFlowContextIdentifiers.Throttling)), Times.AtLeastOnce());

                // Pipeline should still be usable after this. Let's try to send a message through it.
                unitTestOutput.CallCount = 0;
                healthReporterMock.ResetCalls();
                unitTestInput.SendMessage("Final message");

                // Wait for the message to be processed (use double batch delay time for extra padding)
                await Task.Delay(TimeSpan.FromMilliseconds(unitTestOutput.SendEventsDelay.TotalMilliseconds + settings.MaxBatchDelayMsec * 2));

                // The message should have come through.
                Assert.Equal(1, unitTestOutput.CallCount);

                // There should be no new warnings or errors
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public async Task UsableIfExceptionInGlobalFilterOccurs()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            UnitTestOutput unitTestOutput = new UnitTestOutput();
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000,
                MaxEventBatchSize = 2
            };
            UnitTestFilter unitTestFilter = new UnitTestFilter();
            unitTestFilter.EvaluationFailureCondition = "Trouble == true";
            const int TestBatchSize = 6;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                new IFilter[] { unitTestFilter },
                new EventSink[] { new EventSink(unitTestOutput, null) },
                settings))
            {
                // Half of the events should cause filtering to fail with an exception
                for (int i = 0; i < TestBatchSize; i++)
                {
                    if (i % 2 == 0)
                    {
                        unitTestInput.SendData(new Dictionary<string, object> { ["Trouble"] = true });
                    }
                    else
                    {
                        unitTestInput.SendMessage("Hi!");
                    }
                }

                // Wait for the pipeline to drain. 
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                // We should have got good events and warnings about bad events
                Assert.Equal(TestBatchSize / 2, unitTestOutput.CallCount);
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.Is<string>(s => s == EventFlowContextIdentifiers.Filtering)), Times.Exactly(TestBatchSize / 2));
            }
        }

        [Fact]
        public async Task UsableIfExceptionInOutputSpecificFilterOccurs()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            UnitTestOutput unitTestOutput = new UnitTestOutput();
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000,
                MaxEventBatchSize = 2
            };
            UnitTestFilter unitTestFilter = new UnitTestFilter();
            unitTestFilter.EvaluationFailureCondition = "Trouble == true";
            const int TestEventCount = 6;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(unitTestOutput, new IFilter[] { unitTestFilter }) },
                settings))
            {
                // Half of the events should cause filtering to fail with an exception
                for (int i = 0; i < TestEventCount; i++)
                {
                    if (i % 2 == 0)
                    {
                        unitTestInput.SendData(new Dictionary<string, object> { ["Trouble"] = true });
                    }
                    else
                    {
                        unitTestInput.SendMessage("Hi!");
                    }
                }

                // Wait for the pipeline to drain. 
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                // We should have got good events and warnings about bad events
                Assert.Equal(TestEventCount / 2, unitTestOutput.EventCount);
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.Is<string>(s => s == EventFlowContextIdentifiers.Filtering)), Times.Exactly(TestEventCount / 2));
            }
        }

        [Fact]
        public async Task CanDisposePipelineStuckInAFilter()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            UnitTestOutput unitTestOutput = new UnitTestOutput();

            const int CompletionTimeoutMsec = 1000;
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = CompletionTimeoutMsec,
                MaxEventBatchSize = 1,
                PipelineBufferSize = 1,
                MaxConcurrency = 1
            };
            UnitTestFilter unitTestFilter = new UnitTestFilter();
            unitTestFilter.EvaluationDelay = TimeSpan.MaxValue;
            Stopwatch stopwatch;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                new IFilter[] { unitTestFilter },
                new EventSink[] { new EventSink(unitTestOutput, null) },
                settings))
            {
                // Saturate the pipeline
                for (int i = 0; i < 10; i++)
                {
                    unitTestInput.SendMessage("Hi!");
                }

                // Wait a little bit for the pipeline to process messages and get stuck in the filter. 
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                stopwatch = Stopwatch.StartNew();
            }

            stopwatch.Stop();

            // We should have received no events on the output side--everything should have been stuck in the filter (or dropped because of buffer overflow)
            Assert.Equal(0, unitTestOutput.CallCount);

            // Ensure the pipeline stops within the timeout (plus some padding)
            Assert.InRange(stopwatch.ElapsedMilliseconds, 0, CompletionTimeoutMsec + 200);
        }

        [Fact]
        public async Task CanDisposePipelineStuckInAnOutput()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            UnitTestOutput unitTestOutput = new UnitTestOutput();
            unitTestOutput.SendEventsDelay = TimeSpan.MaxValue;
            unitTestOutput.DisregardCancellationToken = true;

            const int CompletionTimeoutMsec = 1000;
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = CompletionTimeoutMsec,
                MaxEventBatchSize = 1,
                PipelineBufferSize = 1,
                MaxConcurrency = 1
            };
            Stopwatch stopwatch;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(unitTestOutput, null) },
                settings))
            {
                // Saturate the pipeline
                for (int i = 0; i < 10; i++)
                {
                    unitTestInput.SendMessage("Hi!");
                }

                // Wait a little bit for the pipeline to process messages and get stuck in the output. 
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                stopwatch = Stopwatch.StartNew();
            }

            stopwatch.Stop();

            // We should have received not sent any data successfully
            Assert.Equal(0, unitTestOutput.CallCount);

            // Ensure the pipeline stops within the timeout (plus some padding)
            Assert.InRange(stopwatch.ElapsedMilliseconds, 0, CompletionTimeoutMsec + 200);
        }

        [Fact]
        public async Task UsableIfExceptionInOutputOccurs()
        {
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();

            UnitTestOutput unitTestOutput = new UnitTestOutput();
            unitTestOutput.FailureCondition = (transmissionSequenceNumber) => transmissionSequenceNumber % 2 == 0;

            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000,
                MaxEventBatchSize = 2
            };
            const int TestEventCount = 32;

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(unitTestOutput, null) },
                settings))
            {
                // Half of the events should cause output to fail with an exception
                for (int i = 0; i < TestEventCount; i++)
                {
                    unitTestInput.SendMessage("Hi!");
                }

                // Wait for the pipeline to drain. 
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                // We should have at least TestEventCount / MaxEventBatchSize calls to the output
                int expectedMinCallCount = TestEventCount / settings.MaxEventBatchSize;
                Assert.InRange(unitTestOutput.CallCount, expectedMinCallCount, TestEventCount);
                // Half of these calls (modulo 1) should have failed
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.Is<string>(s => s == EventFlowContextIdentifiers.Output)),
                    Times.Between(unitTestOutput.CallCount / 2, unitTestOutput.CallCount / 2 + 1, Range.Inclusive));
            }
        }
    }
}
