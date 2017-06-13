// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    /// <summary>
    /// The purpose of this class is to ensure that EventFlow pipeline does not drop events without warning through the health reporter.
    /// This is a possiblity with BroadcastBlock, which, by design, will discard the current message once a new message arrives and the 
    /// current message has been offered to all downstream blocks. This happens regardless whether the message was accepted, postponed
    /// or discarded by the downstream block. 
    /// The LossReportingPropagatorBlock works around this issue by reporting a message loss when a passing message is discarded by the downstream block,
    /// or when a message is postponed and then not available for consumption later on.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class LossReportingPropagatorBlock<T> : IPropagatorBlock<T, T>
    {
        private IHealthReporter healthReporter;
        private ITargetBlock<T> target;
        private ISourceBlock<T> source;
        private bool propagateCompletion;
        private bool completed;
        private Lazy<TaskCompletionSource<bool>> completionSource;
        private long postponements;
        private long consumptionAttempts;

        public LossReportingPropagatorBlock(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
            this.target = null;
            this.propagateCompletion = false;
            this.completed = false;
            this.completionSource = new Lazy<TaskCompletionSource<bool>>(LazyThreadSafetyMode.PublicationOnly);
            this.postponements = this.consumptionAttempts = 0;
        }

        public Task Completion { get { return this.completionSource.Value.Task; } }

        public void Complete()
        {
            this.completed = true;
            this.completionSource.Value.TrySetResult(true);

            if (this.propagateCompletion)
            {
                Debug.Assert(this.target != null);
                this.target.Complete();                
            }

            if (this.postponements > this.consumptionAttempts)
            {
                // Once we are completed, it won't be possible to consume any postponed messages and any postponed messages will effectively be lost.
                this.healthReporter.ReportThrottling();
            }
        }

        public void Fault(Exception exception)
        {
            this.completed = true;

            if (this.propagateCompletion)
            {
                Debug.Assert(this.target != null);
                this.target.Fault(exception);

                // If the pipeline is faulted, the data loss is pretty much inevitable, so it does not make that much sense to report data loss here.
                // (we theoretically could check for this.postponements > this.consumptionAttempts).
            }
            else
            {
                this.completionSource.Value.TrySetException(exception);
            }
        }

        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            Requires.NotNull(target, nameof(target));
            if (this.target != null)
            {
                throw new InvalidOperationException($"{nameof(LossReportingPropagatorBlock<T>)} only supports a single target");
            }

            this.target = target;
            if (linkOptions != null)
            {
                // Note DataflowLinkOptions.Append does not matter given that we are working with a single target only 
                // and we will not enforce DataflowLinkOptions.MaxMessages
                this.propagateCompletion = linkOptions.PropagateCompletion;
            }

            // We do not support re-linking
            return new EmptyDisposable();
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            VerifySourceInitiatedOperation(source);

            if (this.completed)
            {
                return DataflowMessageStatus.DecliningPermanently;
            }

            if (consumeToAccept)
            {
                // The target is asked to synchronously call back via ConsumeMessage(). Since we are counting consumption attempts
                // and verify that postponements == consumption attempts (that happens upon completion), 
                // we have to increment the postponements count here.
                Interlocked.Increment(ref this.postponements);
            }

            var targetResponse = this.target.OfferMessage(messageHeader, messageValue, this, consumeToAccept);
            switch (targetResponse)
            {
                case DataflowMessageStatus.Accepted:
                    // Great, nothing to do
                    break;

                case DataflowMessageStatus.Declined:
                case DataflowMessageStatus.DecliningPermanently:
                case DataflowMessageStatus.NotAvailable:
                    this.healthReporter.ReportThrottling();
                    break;

                case DataflowMessageStatus.Postponed:
                    // This is a bit of a tricky situation in the sense that we cannot determine at the moment whether the loss occurred or not.
                    // The message might be successfully consumed later, or it might no longer be available.
                    // We'll know for sure based on the success/failure of ConsumeMessage().
                    Interlocked.Increment(ref this.postponements);
                    break;
            }

            return targetResponse;
        }

        public T ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            VerifyTargetInitiatedOperation(target);

            messageConsumed = false;

            if (this.completed)
            {
                return default(T);
            }

            Interlocked.Increment(ref this.consumptionAttempts);

            T message = this.source.ConsumeMessage(messageHeader, this, out bool consumedByMe);
            if (consumedByMe)
            {
                messageConsumed = true;
                return message;
            }
            else
            {
                // Message is no longer available
                this.healthReporter.ReportThrottling();
                return default(T);
            }
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            VerifyTargetInitiatedOperation(target);

            if (!this.completed)
            {
                this.source.ReleaseReservation(messageHeader, this);
            }
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            VerifyTargetInitiatedOperation(target);

            if (this.completed)
            {
                return false;
            }

            bool reserved = this.source.ReserveMessage(messageHeader, this);
            if (!reserved)
            {
                // If we cannot reserve the message, it means it is gone before our target had a chance to consume it
                // (either overwritten, or reserved by some other target). Either way we need to warn about the loss.
                this.healthReporter.ReportThrottling();
            }
            return reserved;
        }

        private void VerifyTargetInitiatedOperation(ITargetBlock<T> target)
        {
            Verify.Operation(this.source != null, "The block must have a source");
            Verify.Operation(object.ReferenceEquals(this.target, target), "Only single target is supported");
        }

        private void VerifySourceInitiatedOperation(ISourceBlock<T> source)
        {
            Verify.Operation(this.target != null, "The block must have a target");
            // Make sure we set the source reference only once
            Interlocked.CompareExchange(ref this.source, source, null);
            Verify.Operation(object.ReferenceEquals(this.source, source), "Only single source block is supported");
        }
    }
}
