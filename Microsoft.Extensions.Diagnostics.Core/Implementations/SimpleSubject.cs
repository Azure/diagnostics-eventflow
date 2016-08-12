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

namespace Microsoft.Extensions.Diagnostics
{
    public class SimpleSubject<SubjectType> : IObservable<SubjectType>, IObserver<SubjectType>, IDisposable
    {
        private readonly object lockObject = new object();
        private volatile ImmutableList<IObserver<SubjectType>> observers = ImmutableList<IObserver<SubjectType>>.Empty;
        private volatile bool notifyingObservers = false;

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
            var currentObservers = this.observers;
            if (currentObservers != null)
            {
                try
                {
                    this.notifyingObservers = true;

                    foreach (var observer in currentObservers)
                    {
                        // In our library we expect observers to process notifications very fast and there is no point in notifying them in parallel.
                        // We also do not really care whether we invoke OnNext in parallel on our observers, as they are resilient to that.
                        observer.OnNext(value);
                    }
                }
                finally
                {
                    this.notifyingObservers = false;
                }
            }
        }

        public IDisposable Subscribe(IObserver<SubjectType> observer)
        {
            Requires.NotNull(observer, nameof(observer));
            lock(this.lockObject)
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
            lock(this.lockObject)
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
                currentObservers = this.observers;
                this.observers = null;                
            }

            // Wait for observer notifications to complete before completing them (i.e. calling OnCompleted or OnError).
            SpinWait.SpinUntil(() => !this.notifyingObservers, TimeSpan.FromSeconds(5));
            return currentObservers;
        }

        private class Subscription<SubscriptionType> : IDisposable
        {
            private IObserver<SubscriptionType> observer;
            private SimpleSubject<SubscriptionType> parentSubject;

            public Subscription(IObserver<SubscriptionType> observer, SimpleSubject<SubscriptionType> parentSubject)
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
