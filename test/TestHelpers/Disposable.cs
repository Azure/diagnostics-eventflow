// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class Disposable: IDisposable
    {
        private Action disposeAction;
        private bool disposed;

        public Disposable(Action dispose, Action initialize = null)
        {
            Requires.NotNull(dispose, nameof(dispose));

            this.disposed = false;
            this.disposeAction = dispose;
            if (initialize != null)
            {
                initialize();
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.disposeAction();
            }
        }
    }
}
