// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Moq;
using Xunit;

using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Nest;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class EventFlowSubjectTests
    {
        [Fact]
        public void SubjectDeliversData()
        {
            var subject = new EventFlowSubject<int>();
            var obs1 = new Mock<IObserver<int>>();
            var obs2 = new Mock<IObserver<int>>();

            var s1 = subject.Subscribe(obs1.Object);
            var s2 = subject.Subscribe(obs2.Object);

            subject.OnNext(7);
            s2.Dispose();
            subject.OnNext(8);
            subject.OnCompleted();
            subject.OnNext(9);
            subject.Dispose();
            subject.OnNext(10);

            obs1.Verify(o => o.OnNext(It.IsAny<int>()), Times.Exactly(2));
            obs1.Verify(o => o.OnNext(It.Is<int>(i => i == 7)), Times.Exactly(1));
            obs1.Verify(o => o.OnNext(It.Is<int>(i => i == 8)), Times.Exactly(1));
            obs1.Verify(o => o.OnCompleted(), Times.Exactly(1));
            obs1.Verify(o => o.OnError(It.IsAny<Exception>()), Times.Exactly(0));

            obs2.Verify(o => o.OnNext(It.IsAny<int>()), Times.Exactly(1));
            obs1.Verify(o => o.OnNext(It.Is<int>(i => i == 7)), Times.Exactly(1));
            obs1.Verify(o => o.OnCompleted(), Times.Exactly(1));
            obs1.Verify(o => o.OnError(It.IsAny<Exception>()), Times.Exactly(0));
        }

        [Fact]
        public void SubjectCompletesImmediatelyAfterDisposed()
        {
            var subject = new EventFlowSubject<int>();
            var obs1 = new Mock<IObserver<int>>();

            subject.Dispose();
            var s1 = subject.Subscribe(obs1.Object);
            s1.Dispose(); // No exception

            obs1.Verify(o => o.OnNext(It.IsAny<int>()), Times.Exactly(0));
            obs1.Verify(o => o.OnCompleted(), Times.Exactly(1));
            obs1.Verify(o => o.OnError(It.IsAny<Exception>()), Times.Exactly(0));

        }

        [Fact]
        public async Task SubjectWaitsOnSlowObservers()
        {
            // Use shutdown timeout long enough to be "infinite" from the test standpoint,
            // but short enough for the test run to finish in reasonable time 
            // in case something goes seriously wrong.
            var ShutdownTimeout = TimeSpan.FromMinutes(15);
            
            var subject = new EventFlowSubject<int>(ShutdownTimeout);
            var observer = new SlowObserver<int>();
            subject.Subscribe(observer);

            var nextDataTask = Task.Run(() => subject.OnNext(0));
            Assert.True(observer.OnNextStartedEvent.WaitOne(TimeSpan.FromSeconds(5)), "Five seconds should be plenty to start the OnNext() task");
            var completionTask = Task.Run(() => subject.OnCompleted());

            await Task.WhenAll(nextDataTask, completionTask);

            Assert.True(observer.OnNextExitTimestamp != DateTime.MinValue, "OnNext() was never called!");
            Assert.True(observer.OnCompletedEntryTimestamp != DateTime.MinValue, "OnCompleted was never called");
            Assert.True(observer.OnCompletedEntryTimestamp >= observer.OnNextExitTimestamp, "OnCompleted() should not have been called when OnNext() was in progress");
        }

        private class SlowObserver<T> : IObserver<T>
        {
            public DateTime OnCompletedEntryTimestamp { get; private set; } = DateTime.MinValue;
            public DateTime OnNextExitTimestamp { get; private set; } = DateTime.MinValue;
            public AutoResetEvent OnNextStartedEvent { get; private set; } = new AutoResetEvent(initialState: false);

            public void OnCompleted()
            {
                OnCompletedEntryTimestamp = DateTimePrecise.UtcNow;
            }

            public void OnError(Exception error)
            {
                throw new Exception("Errors are not expected");
            }

            public void OnNext(T value)
            {
                OnNextStartedEvent.Set();
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                OnNextExitTimestamp = DateTimePrecise.UtcNow;
            }
        }
    }
}
