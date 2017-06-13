// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Validation;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    internal class TestTargetBlock<T> : ITargetBlock<T>
    {
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        private DataflowMessageStatus consumptionMode = DataflowMessageStatus.Accepted;

        public Task Completion => this.tcs.Task;
        public readonly ConcurrentQueue<PostponedMessageRecord<T>> MessagesPostponed = new ConcurrentQueue<PostponedMessageRecord<T>>();
        public readonly ConcurrentQueue<T> MessagesConsumed = new ConcurrentQueue<T>();
        public readonly ConcurrentQueue<T> MessagesDeclined = new ConcurrentQueue<T>();

        public DataflowMessageStatus ConsumptionMode
        {
            get => this.consumptionMode;
            set
            {
                Requires.Range(value != DataflowMessageStatus.NotAvailable, nameof(ConsumptionMode));
                this.consumptionMode = value;
            }
        }

        public void Complete()
        {
            lock (this.tcs)
            {
                if (Completion.Status == TaskStatus.RanToCompletion)
                {
                    return;
                }

                this.tcs.SetResult(true);
                this.consumptionMode = DataflowMessageStatus.DecliningPermanently;
            }
        }

        public void Fault(Exception exception)
        {
            lock (this.tcs)
            {
                this.tcs.SetException(exception);
                this.consumptionMode = DataflowMessageStatus.DecliningPermanently;
            }
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            switch(this.consumptionMode)
            {
                case DataflowMessageStatus.Accepted:
                    T message;
                    bool messageConsumed = true;

                    if (consumeToAccept)
                    {
                        message = source.ConsumeMessage(messageHeader, this, out messageConsumed);
                        Assert.True(messageConsumed, "If consumeToAccept flag is set, ConsumeMessage() should have succeeded");
                    }
                    else
                    {
                        message = messageValue;
                    }
                    if (messageConsumed)
                    {
                        this.MessagesConsumed.Enqueue(messageValue);
                    }
                    break;

                case DataflowMessageStatus.Postponed:
                    this.MessagesPostponed.Enqueue(new PostponedMessageRecord<T>()
                    {
                        MessageHeader = messageHeader,
                        MessageValue = messageValue,
                        Source = source
                    });
                    break;

                case DataflowMessageStatus.Declined:
                case DataflowMessageStatus.DecliningPermanently:
                    this.MessagesDeclined.Enqueue(messageValue);
                    break;
            }

            return this.consumptionMode;
        }

        public void ConsumePostponedMessages()
        {
            while(this.MessagesPostponed.TryDequeue(out PostponedMessageRecord<T> postponedMessageRecord))
            {
                T message = postponedMessageRecord.Source.ConsumeMessage(postponedMessageRecord.MessageHeader, this, out bool messageConsumed);
                if (messageConsumed)
                {
                    this.MessagesConsumed.Enqueue(message);
                }
            }
        }
    }

    internal class PostponedMessageRecord<T>
    {
        public DataflowMessageHeader MessageHeader;
        public T MessageValue;
        public ISourceBlock<T> Source;
    }
}
