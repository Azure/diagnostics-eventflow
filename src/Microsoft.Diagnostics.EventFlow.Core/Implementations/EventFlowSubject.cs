// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    public class EventFlowSubject<SubjectType> : IObservable<SubjectType>, IObserver<SubjectType>, IDisposable, IItemWithLabels
    {
        private readonly object lockObject;
        private volatile ImmutableList<IObserver<SubjectType>> observers;
        private volatile bool shuttingDown;
        private volatile int notificationsInProgress;
        private Lazy<IDictionary<string, string>> labelsSource;
        private TimeSpan shutdownTimeout;

        public EventFlowSubject()
        {
            this.lockObject = new object();
            this.observers = ImmutableList<IObserver<SubjectType>>.Empty;
            this.shuttingDown = false;
            this.notificationsInProgress = 0;
            this.labelsSource = new Lazy<IDictionary<string, string>>(() => new Dictionary<string, string>());
            this.shutdownTimeout = TimeSpan.FromSeconds(5);
        }

        public EventFlowSubject(TimeSpan shutdownTimeout): this()
        {
            this.shutdownTimeout = shutdownTimeout;
        }

        public IDictionary<string, string> Labels { get { return this.labelsSource.Value; } }

        public void Dispose()
        {
            this.OnCompleted();
        }

        public void OnCompleted()
        {
            IEnumerable<IObserver<SubjectType>> remainingObservers = Shutdown();
            if (remainingObservers != null)
            {
                Parallel.ForEach(remainingObservers, observer => observer.OnCompleted());
            }
        }

        public void OnError(Exception error)
        {
            IEnumerable<IObserver<SubjectType>> remainingObservers = Shutdown();
            if (remainingObservers != null)
            {
                Parallel.ForEach(remainingObservers, observer => observer.OnError(error));
            }
        }

        public void OnNext(SubjectType value)
        {
            try
            {
                Interlocked.Increment(ref this.notificationsInProgress);

                var currentObservers = this.observers;
                if (currentObservers != null && !this.shuttingDown)
                {
                    foreach (var observer in currentObservers)
                    {
                        // In our library we expect observers to process notifications very fast and there is no point in notifying them in parallel.
                        // We also do not really care whether we invoke OnNext in parallel on our observers, as they are resilient to that.
                        observer.OnNext(value);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref this.notificationsInProgress);
            }
        }

        public IDisposable Subscribe(IObserver<SubjectType> observer)
        {
            Requires.NotNull(observer, nameof(observer));
            lock (this.lockObject)
            {
                if (IsCompleted)
                {
                    observer.OnCompleted();
                    return EmptyDisposable.Instance;
                }
                else
                {
                    this.observers = this.observers.Add(observer);
                    return new Subscription<SubjectType>(observer, this);
                }
            }
        }

        public void Unsubscribe(IObserver<SubjectType> observer)
        {
            lock (this.lockObject)
            {
                if (!IsCompleted)
                {
                    this.observers = this.observers.Remove(observer);
                }
            }
        }

        private bool IsCompleted
        {
            get { return this.observers == null; }
        }

        private IEnumerable<IObserver<SubjectType>> Shutdown()
        {
            IEnumerable<IObserver<SubjectType>> currentObservers;
            lock (this.lockObject)
            {
                if (this.shuttingDown)
                {
                    return null;
                }
                this.shuttingDown = true;
                currentObservers = this.observers;
                this.observers = null;
            }

            // Wait for observer notifications to complete before completing them (i.e. calling OnCompleted or OnError).
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref this.notificationsInProgress, 0,0) == 0, this.shutdownTimeout);
            return currentObservers;
        }

        private class Subscription<SubscriptionType> : IDisposable
        {
            private IObserver<SubscriptionType> observer;
            private EventFlowSubject<SubscriptionType> parentSubject;

            public Subscription(IObserver<SubscriptionType> observer, EventFlowSubject<SubscriptionType> parentSubject)
            {
                Debug.Assert(observer != null);
                Debug.Assert(parentSubject != null);
                this.parentSubject = parentSubject;
                this.observer = observer;
            }

            public void Dispose()
            {
                var current = Interlocked.Exchange<IObserver<SubscriptionType>>(ref this.observer, null);
                if (current != null)
                {
                    this.parentSubject.Unsubscribe(current);
                    this.parentSubject = null;
                }
            }
        }
    }
}
