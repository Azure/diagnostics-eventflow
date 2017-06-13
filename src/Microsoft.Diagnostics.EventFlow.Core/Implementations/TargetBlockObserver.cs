// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    internal class TargetBlockObserver<TInput> : IObserver<TInput>
    {
        private readonly ITargetBlock<TInput> target;
        private readonly IHealthReporter healthReporter;

        public TargetBlockObserver(ITargetBlock<TInput> target, IHealthReporter healthReporter)
        {
            Requires.NotNull(target, nameof(target));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.target = target;
            this.healthReporter = healthReporter;
        }

        internal Task<bool> SendAsyncToTarget(TInput value)
        {
            return this.target.SendAsync<TInput>(value);
        }

        void IObserver<TInput>.OnCompleted()
        {
            this.target.Complete();
        }

        void IObserver<TInput>.OnError(Exception error)
        {
            this.target.Fault(error);
        }

        void IObserver<TInput>.OnNext(TInput value)
        {
            if (!target.Post(value))
            {
                healthReporter.ReportThrottling();
            }
        }
    }
}
