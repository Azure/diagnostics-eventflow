// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
#if NET451
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class EventFlowLoggerScope
    {
        internal EventFlowLoggerScope(object state)
        {
            State = state;
        }

        public Object State { get; private set; }
        public EventFlowLoggerScope Parent { get; private set; }

#if NET451
        private static readonly string FieldKey = $"{typeof(EventFlowLoggerScope).FullName}_{AppDomain.CurrentDomain.Id}";
        public static EventFlowLoggerScope Current
        {
            get
            {
                ObjectHandle handle = (ObjectHandle)CallContext.LogicalGetData(FieldKey);

                // Unwrap the scope if it was set in the same AppDomain (as FieldKey is AppDomain-specific). 
                if (handle != null)
                {
                    return (EventFlowLoggerScope)handle.Unwrap();
                }

                return null;
            }
            private set
            {
                CallContext.LogicalSetData(FieldKey, new ObjectHandle(value));
            }
        }
#else
        private static AsyncLocal<EventFlowLoggerScope> _value = new AsyncLocal<EventFlowLoggerScope>();
        public static EventFlowLoggerScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value;
            }
        }
#endif

        public static IDisposable Push(object state)
        {
            var temp = Current;
            Current = new EventFlowLoggerScope(state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        public override string ToString()
        {
            return State?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }
    }
}