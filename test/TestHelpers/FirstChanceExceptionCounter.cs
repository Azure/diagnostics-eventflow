// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class FirstChanceExceptionCounter : IDisposable
    {
        private Type exceptionType;
        private int count;
        private bool disposed;
        private int threadId;
        

        public FirstChanceExceptionCounter(Type exceptionType)
        {
            this.exceptionType = exceptionType;
            this.count = 0;
            this.disposed = false;
            this.threadId = Thread.CurrentThread.ManagedThreadId;

            // First-chance exception tracking is not supported with NETSTANDARD1.6, 
            // so we always report zero exceptions for tests using that version of .NET Standard.
            // That's acceptable since tests using this class also run with full framework,
            // and with .NET Standard 2.0-based framework, and these frameworks will provide enough coverage.
#if !NETSTANDARD1_6
            AppDomain.CurrentDomain.FirstChanceException += OnException;
#endif
        }

#if !NETSTANDARD1_6
        private void OnException(object sender, FirstChanceExceptionEventArgs e)
        {
            // We don't want to count all exceptions from the AppDomain, because some might be coming from a different test,
            // running in parallel with the one that we are counting the exceptions for.
            // Obviously relying on managed thread ID to filter the incoming exceptions won't work for async code,
            // but right now we don't have a need for using this class to test async code.
            if (Thread.CurrentThread.ManagedThreadId == this.threadId && (this.exceptionType == null || e.Exception.GetType() == exceptionType))
            {
                this.count++;
            }
        }
#endif

        public int Count => this.count;

        public void Dispose()
        {
#if !NETSTANDARD1_6
            if (!this.disposed)
            {
                AppDomain.CurrentDomain.FirstChanceException -= OnException;
            }
#endif
            this.disposed = true;
        }
    }
}
