// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    // These test do not verify EventFlow functionality, but rather they confirm assumptions about TPL Dataflow library,
    // upon which the EventFlow pipeline is built.
    public class DataflowTests
    {
        private static readonly DataflowLinkOptions PropagateCompletion = new DataflowLinkOptions() { PropagateCompletion = true };
        private static readonly TimeSpan MessageArrivalTimeout = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan CompletionTimeout = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan CompletionInProgressVerificationTimeout = TimeSpan.FromMilliseconds(2000);
        private static readonly TimeSpan BurstTimeout = TimeSpan.FromSeconds(10);

        [Fact]
        public Task BufferBlockKeepsPostponedMessages()
        {
            return BlockKeepsPostponedMessages(() => new BufferBlock<int>(new DataflowBlockOptions()
                { BoundedCapacity = 3 }));
        }

        [Fact]
        public Task BufferBlockKeepsDeclinedMessages()
        {
            return BlockKeepsDeclinedMessages(
                () => new BufferBlock<int>(new DataflowBlockOptions() { BoundedCapacity = 3 }),
                (block) => ((BufferBlock<int>)block).Count);
        }

        [Fact]
        public Task BufferBlockWillCompleteTarget()
        {
            return BlockWillCompleteTarget(() => new BufferBlock<int>(new DataflowBlockOptions()
                { BoundedCapacity = 3 }));
        }

        [Fact]
        public Task BufferBlockStuckCompletionCannotBeCancelled()
        {
            return StuckCompletionCannotBeCancelled(
                (cancellationToken) => new BufferBlock<int>(new DataflowBlockOptions()
                    { BoundedCapacity = 3, CancellationToken = cancellationToken }),
                (block) => ((BufferBlock<int>) block).Count);
        }

        [Fact]
        public async Task BatchBlockKeepsPostponedMessages()
        {
            BatchBlock<int> bb = new BatchBlock<int>( batchSize: 2,
                dataflowBlockOptions: new GroupingDataflowBlockOptions() { BoundedCapacity = 3 });

            TestTargetBlock<int[]> testTarget = new TestTargetBlock<int[]>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Postponed;
            bb.LinkTo(testTarget, PropagateCompletion);

            // Assumption: if the target of the BatchBlock is postponing messages, 
            // BatchBlock will accept incoming messages until it runs out of capacity.
            Assert.True(bb.Post(1));
            Assert.True(bb.Post(2));

            // Still able to accept one more message
            Assert.True(bb.Post(3));

            // Out of capacity. 
            Assert.False(bb.Post(4));

            // Send() a message to give the block a chance to postpone it
            bb.SendAsync(5).Forget();

            // First batch offered, but postponed
            bool gotFirstBatch = await TaskUtils.PollWaitAsync(() => testTarget.MessagesPostponed.Count == 1, MessageArrivalTimeout);
            Assert.True(gotFirstBatch);

            // Assumption: once the BatchBlock target stops postponing, BatchBlock will keep pushing data to target 
            // until it runs out of buffered messages.
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            testTarget.ConsumePostponedMessages();

            // The second, postponed message should allow the block to complete two batches
            bool gotTwoBatches = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count == 2, MessageArrivalTimeout);
            Assert.True(gotTwoBatches);
            Assert.True(testTarget.MessagesConsumed.All((m) => m.Length == 2));
        }        

        [Fact]
        public async Task BatchBlockKeepsDeclinedMessages()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            BatchBlock<int> bb = new BatchBlock<int>(batchSize: 2,
                dataflowBlockOptions: new GroupingDataflowBlockOptions() { BoundedCapacity = 3, CancellationToken = cts.Token });

            TestTargetBlock<int[]> testTarget = new TestTargetBlock<int[]>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Declined;
            bb.LinkTo(testTarget, PropagateCompletion);

            // Assumption: BatchBlock will keep incoming messages even when its target is declining them
            Assert.True(bb.Post(1));
            Assert.True(bb.Post(2));
            Assert.True(bb.Post(3));

            // The block has run out of capacity
            Assert.False(bb.Post(4));
            Assert.False(bb.Post(5));

            // This message will be postponed (and, in fact, released when we ask the block to complete)
            bb.SendAsync(6).Forget();

            // The messages are buffered and there is one batch ready to be consumed (the second one is not full)
            Assert.Equal(1, bb.OutputCount);

            // Wait till the block offers a message
            // Assumption: only one message will be offered, the block will not offer more messages if the target declines
            bool oneMessageOffered = await TaskUtils.PollWaitAsync(() => testTarget.MessagesDeclined.Count == 1, MessageArrivalTimeout);
            Assert.True(oneMessageOffered);
            Assert.True(testTarget.MessagesConsumed.Count == 0);
            Assert.True(testTarget.MessagesPostponed.Count == 0);

            // Assumption: the block will NOT try to deliver declined messages again when asked to complete.
            // The fact that the buffer is not empty will prevent it from completing
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            bb.Complete();
            bool someMessagesDelivered = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count > 0, MessageArrivalTimeout);
            Assert.False(someMessagesDelivered);

            // Because we asked the BatchBlock for completion, it now formed a second, undersize batch
            Assert.Equal(2, bb.OutputCount);

            // Completion task should still be running
            await Task.WhenAny(bb.Completion, Task.Delay(CompletionTimeout));
            Assert.False(bb.Completion.IsCompleted);

            // Assumption: BatchBlock will not start target's completion until it itself completes
            await Task.WhenAny(testTarget.Completion, Task.Delay(CompletionTimeout));
            Assert.True(testTarget.Completion.IsNotStarted());
        }

        [Fact]
        public async Task BatchBlockWillCompleteTarget()
        {
            BatchBlock<int> bb = new BatchBlock<int>(batchSize: 2,
                dataflowBlockOptions: new GroupingDataflowBlockOptions() { BoundedCapacity = 3 });

            TestTargetBlock<int[]> testTarget = new TestTargetBlock<int[]>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            bb.LinkTo(testTarget, PropagateCompletion);

            // Rapidly send 50 messages
            Task.WaitAll(Enumerable.Range(0, 50).Select((i) => bb.SendAsync(i)).ToArray(), BurstTimeout);

            bb.Complete();

            // Completion should run to successful conclusion
            await Task.WhenAny(bb.Completion, Task.Delay(CompletionTimeout));
            Assert.Equal(TaskStatus.RanToCompletion, bb.Completion.Status);

            // Assumption: BufferBlock should also have completed its target
            await Task.WhenAny(testTarget.Completion, Task.Delay(CompletionTimeout));
            Assert.Equal(TaskStatus.RanToCompletion, testTarget.Completion.Status);

            // Assumption: we should have gotten 25 batches
            bool allMessagesReceived = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count == 25, MessageArrivalTimeout);
            Assert.True(allMessagesReceived);
        }

        [Fact]
        public async Task BatchBlockStuckCompletionCannotBeCancelled()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            BatchBlock<int> bb = new BatchBlock<int>(batchSize: 2,
                dataflowBlockOptions: new GroupingDataflowBlockOptions() {
                    BoundedCapacity = 3, CancellationToken = cts.Token });

            TestTargetBlock<int[]> testTarget = new TestTargetBlock<int[]>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Declined;
            bb.LinkTo(testTarget, PropagateCompletion);

            Assert.True(bb.Post(1));
            Assert.True(bb.Post(2));
            Assert.True(bb.Post(3));

            bb.Complete();

            // Completion task should still be running
            // Assumption: BatchBlock will not start target's completion until it itself completes
            await Task.WhenAny(bb.Completion, testTarget.Completion, Task.Delay(CompletionInProgressVerificationTimeout));
            Assert.False(bb.Completion.IsCompleted);
            Assert.True(testTarget.Completion.IsNotStarted());

            // Assumption: cancellation does not affect the completion of the block (unfortunately!)
            cts.Cancel();
            await Task.WhenAny(bb.Completion, testTarget.Completion, Task.Delay(CompletionInProgressVerificationTimeout));
            Assert.False(bb.Completion.IsCompleted);
            Assert.True(testTarget.Completion.IsNotStarted());
            
            // The block still has 2 batches: one full, one undersize
            Assert.Equal(2, bb.OutputCount);
        }

        [Fact]
        public Task TransformBlockKeepsPostponedMessages()
        {
            return BlockKeepsPostponedMessages(() => new TransformBlock<int, int>(
                transform: (int a) => a,
                dataflowBlockOptions: new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    MaxDegreeOfParallelism = 2,
                    SingleProducerConstrained = false
                }));
        }

        [Fact]
        public Task TransformBlockKeepsDeclinedMessages()
        {
            return BlockKeepsDeclinedMessages(
                () => new TransformBlock<int, int>(
                transform: (int a) => a,
                dataflowBlockOptions: new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    MaxDegreeOfParallelism = 2,
                    SingleProducerConstrained = false
                }),
                (block) => ((TransformBlock<int, int>) block).OutputCount);
        }

        [Fact]
        public Task TransformBlockWillCompleteTarget()
        {
            return BlockWillCompleteTarget(() => new TransformBlock<int, int>(
                transform: (int a) => a,
                dataflowBlockOptions: new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    MaxDegreeOfParallelism = 2,
                    SingleProducerConstrained = false
                }));
        }

        [Fact]
        public Task TransformBlockStuckCompletionCannotBeCancelled()
        {
            return StuckCompletionCannotBeCancelled(
                (cancellationToken) => new TransformBlock<int, int>(
                    transform: (int a) => a,
                    dataflowBlockOptions: new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = 3,
                        MaxDegreeOfParallelism = 2,
                        SingleProducerConstrained = false,
                        CancellationToken = cancellationToken
                    }),
                (block) => ((TransformBlock<int, int>)block).OutputCount);
        }

        [Fact]
        public async Task BroadcastBlockOverwritesElementsAfterAllTargetsHaveBeenOffered()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            BroadcastBlock<int> bb = new BroadcastBlock<int>((i) => i,
                new DataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    CancellationToken = cts.Token
                });

            TestTargetBlock<int> t1 = new TestTargetBlock<int>();
            t1.ConsumptionMode = DataflowMessageStatus.Accepted;
            TestTargetBlock<int> t2 = new TestTargetBlock<int>();
            t2.ConsumptionMode = DataflowMessageStatus.Declined;
            TestTargetBlock<int> t3 = new TestTargetBlock<int>();
            t3.ConsumptionMode = DataflowMessageStatus.Postponed;

            bb.LinkTo(t1, PropagateCompletion);
            bb.LinkTo(t2, PropagateCompletion);
            bb.LinkTo(t3, PropagateCompletion);

            // Rapidly send 50 messages
            Task.WaitAll(Enumerable.Range(0, 50).Select((i) => bb.SendAsync(i)).ToArray(), BurstTimeout);

            // Assumption: all 50 messages will go through, even though t2 is declining them and t3 is postponing.
            bool target1ConsumedAllMessages = await TaskUtils.PollWaitAsync(() => t1.MessagesConsumed.Count == 50, MessageArrivalTimeout);
            Assert.True(target1ConsumedAllMessages);
            bool target2RejectedAllMessages = await TaskUtils.PollWaitAsync(() => t2.MessagesDeclined.Count == 50, MessageArrivalTimeout);
            Assert.True(target2RejectedAllMessages);
            bool target3PostponedAllMessages = await TaskUtils.PollWaitAsync(() => t3.MessagesPostponed.Count == 50, MessageArrivalTimeout);
            Assert.True(target3PostponedAllMessages);
        }

        [Fact]
        public Task BroadcastBlockWillCompleteTarget()
        {
            return BlockWillCompleteTarget(() => new BroadcastBlock<int>((i) => i,
                new DataflowBlockOptions
                {
                    BoundedCapacity = 3
                }));
        }

        [Fact]
        public async Task BroadcastBlockDoesNotRememberMoreThanOnePostponedMessage()
        {
            BroadcastBlock<int> bb = new BroadcastBlock<int>((i) => i,
                new DataflowBlockOptions
                {
                    BoundedCapacity = 3
                });

            TestTargetBlock<int> target = new TestTargetBlock<int>();
            target.ConsumptionMode = DataflowMessageStatus.Postponed;
            bb.LinkTo(target, PropagateCompletion);

            bb.Post(1);
            bb.Post(2);
            await TaskUtils.PollWaitAsync(() => target.MessagesPostponed.Count == 2, MessageArrivalTimeout);

            target.ConsumePostponedMessages();
            // Assumption: only one message (the last one) will be successfully consumed. The previous message was overwritten, 
            // so when the target inquires about it, it is gone.
            Assert.Single(target.MessagesConsumed);
        }

        [Fact]
        public async Task ActionBlockBuffersAndPostponesMessagesWhenActionSlow()
        {
            int processedMessageCount = 0;
            TimeSpan ProcessingTime = TimeSpan.FromMilliseconds(50);
            TimeSpan ProcessingTimeTimesTen = TimeSpan.FromMilliseconds(500);

            ActionBlock<int> ab = new ActionBlock<int>(
                (msgId) => 
                {
                    Task.Delay(ProcessingTime);
                    Interlocked.Increment(ref processedMessageCount);
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    MaxDegreeOfParallelism = 2,
                    SingleProducerConstrained = false
                });

            // Send 10 messages as fast as possible
            Task.WaitAll(Enumerable.Range(0, 10).Select((i) => ab.SendAsync(i)).ToArray(), BurstTimeout);

            // Assumption: all will be eventually processed. 
            bool allProcessed = await TaskUtils.PollWaitAsync(() => processedMessageCount == 10, ProcessingTimeTimesTen + MessageArrivalTimeout);
            Assert.True(allProcessed);
        }

        [Fact]
        public async Task ActionBlockWillProcessAllAcceptedMessagesBeforeCompletion()
        {
            int processedMessageCount = 0;
            TimeSpan ProcessingTime = TimeSpan.FromMilliseconds(50);
            TimeSpan ProcessingTimeTimesTen = TimeSpan.FromMilliseconds(500);

            ActionBlock<int> ab = new ActionBlock<int>(
                (msgId) =>
                {
                    Task.Delay(ProcessingTime);
                    Interlocked.Increment(ref processedMessageCount);
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 3,
                    MaxDegreeOfParallelism = 2,
                    SingleProducerConstrained = false
                });

            // Send 10 messages as fast as possible
            Task.WaitAll(Enumerable.Range(0, 10).Select((i) => ab.SendAsync(i)).ToArray(), BurstTimeout);

            // Wait for completion and ensure that it does not time out and that all messages were processed before completion.
            ab.Complete();
            await Task.WhenAny(ab.Completion, Task.Delay(ProcessingTimeTimesTen + MessageArrivalTimeout));
            Assert.True(ab.Completion.IsCompleted);
            Assert.Equal(10, processedMessageCount);
        }

        private async Task BlockKeepsDeclinedMessages(Func<ISourceBlock<int>> BlockFactory, Func<ISourceBlock<int>, int> OutputCount)
        {
            ISourceBlock<int> block = BlockFactory();
            ITargetBlock<int> blockT = (ITargetBlock<int>)block;

            TestTargetBlock<int> testTarget = new TestTargetBlock<int>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Declined;
            block.LinkTo(testTarget, PropagateCompletion);

            // Assumption: block will keep incoming messages even when its target is declining them
            Assert.True(blockT.Post(1));
            Assert.True(blockT.Post(2));
            Assert.True(blockT.Post(3));

            // The block has run out of capacity
            Assert.False(blockT.Post(4));

            // This message will be postponed (and, in fact, released when we ask the block to complete)
            blockT.SendAsync(5).Forget();            

            // Wait till the block offers a message
            // Assumption: only one message will be offered, the block will not offer more messages if the target declines
            bool oneMessageOffered = await TaskUtils.PollWaitAsync(() => testTarget.MessagesDeclined.Count == 1, MessageArrivalTimeout);
            Assert.True(oneMessageOffered);
            Assert.True(testTarget.MessagesConsumed.Count == 0);
            Assert.True(testTarget.MessagesPostponed.Count == 0);

            // Use poll waiting because the item count is not always immediately updated by the block
            bool threeMessagesBuffered = await TaskUtils.PollWaitAsync(() => OutputCount(block) == 3, MessageArrivalTimeout);
            Assert.True(threeMessagesBuffered);

            // Assumption: the block will try NOT to deliver declined messages again when asked to complete.
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            block.Complete();
            bool someMessagesDelivered = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count > 0, MessageArrivalTimeout);
            Assert.False(someMessagesDelivered);

            // Completion task should still be running
            // And the block should not start target's completion until it itself completes
            await Task.WhenAny(block.Completion, testTarget.Completion, Task.Delay(CompletionInProgressVerificationTimeout));
            Assert.False(block.Completion.IsCompleted);
            Assert.True(testTarget.Completion.IsNotStarted());
        }

        private async Task BlockKeepsPostponedMessages(Func<ISourceBlock<int>> BlockFactory)
        {
            // This test requires longer timeout than most of the other tests in this suite, otherwise it occasionally fails on slower machines
            // If it fails nevertheless, switch to custom TaskScheduler.
            TimeSpan longArrivalTimeout = TimeSpan.FromMilliseconds(MessageArrivalTimeout.TotalMilliseconds * 10.0);

            ISourceBlock<int> block = BlockFactory();
            ITargetBlock<int> blockT = (ITargetBlock<int>)block;

            TestTargetBlock<int> testTarget = new TestTargetBlock<int>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Postponed;
            block.LinkTo(testTarget, PropagateCompletion);

            // Assumption: if the target of the block is postponing messages, 
            // The block will accept incoming messages until it runs out of capacity.
            Assert.True(blockT.Post(1));
            Assert.True(blockT.Post(2));
            Assert.True(blockT.Post(3));

            // Out of capacity
            Assert.False(blockT.Post(4));

            // However SendAsync() will allow postponing the message, so the message will be eventually delivered
            blockT.SendAsync(5).Forget();

            // Wait till the block offers a message
            // Assumption: only one message will be offered, the block will not offer more messages if the target postpones
            bool messageOffered = await TaskUtils.PollWaitAsync(() => testTarget.MessagesPostponed.Count == 1, longArrivalTimeout);
            Assert.True(messageOffered);

            // Assumption: once the block target stops postponing, the block will keep pushing data to target 
            // until it runs out of buffered messages.
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            testTarget.ConsumePostponedMessages();
            bool gotAllMessages = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count == 4, longArrivalTimeout);
            Assert.True(gotAllMessages, "We should have gotten 4 messages");
            Assert.Equal(testTarget.MessagesConsumed.OrderBy((i) => i), new int[] { 1, 2, 3, 5 });
        }

        private async Task BlockWillCompleteTarget(Func<ISourceBlock<int>> BlockFactory)
        {
            ISourceBlock<int> block = BlockFactory();

            TestTargetBlock<int> testTarget = new TestTargetBlock<int>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Accepted;
            block.LinkTo(testTarget, PropagateCompletion);

            // Rapidly send 50 messages
            Task.WaitAll(Enumerable.Range(0, 50).Select((i) => ((ITargetBlock<int>)block).SendAsync(i)).ToArray(), BurstTimeout);

            block.Complete();

            // Completion should run to successful conclusion
            await Task.WhenAny(block.Completion, Task.Delay(CompletionTimeout));
            Assert.Equal(TaskStatus.RanToCompletion, block.Completion.Status);

            // Assumption: the block should also have completed its target
            await Task.WhenAny(testTarget.Completion, Task.Delay(CompletionTimeout));
            Assert.Equal(TaskStatus.RanToCompletion, testTarget.Completion.Status);

            // Assumption: we should have gotten 50 messages
            bool allMessagesReceived = await TaskUtils.PollWaitAsync(() => testTarget.MessagesConsumed.Count == 50, MessageArrivalTimeout);
            Assert.True(allMessagesReceived);
        }

        private async Task StuckCompletionCannotBeCancelled(Func<CancellationToken, ISourceBlock<int>> BlockFactory, Func<ISourceBlock<int>, int> OutputCount)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            ISourceBlock<int> block = BlockFactory(cts.Token);
            ITargetBlock<int> blockT = (ITargetBlock<int>)block;

            TestTargetBlock<int> testTarget = new TestTargetBlock<int>();
            testTarget.ConsumptionMode = DataflowMessageStatus.Declined;
            block.LinkTo(testTarget, PropagateCompletion);

            Assert.True(blockT.Post(1));
            Assert.True(blockT.Post(2));

            block.Complete();

            // Completion task should still be running
            // Also, we assume that BufferBlock will not start target's completion until it itself completes
            await Task.WhenAny(block.Completion, testTarget.Completion, Task.Delay(CompletionInProgressVerificationTimeout));
            Assert.False(block.Completion.IsCompleted);
            Assert.True(testTarget.Completion.IsNotStarted());

            // Assumption: cancellation does not affect the completion of the block (unfortunately!)
            cts.Cancel();
            await Task.WhenAny(block.Completion, testTarget.Completion, Task.Delay(CompletionInProgressVerificationTimeout));
            Assert.False(block.Completion.IsCompleted);
            Assert.True(testTarget.Completion.IsNotStarted());
            Assert.Equal(2, OutputCount(block));
        }
    }
}
