// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class TemporaryFile : IDisposable
    {
        public TemporaryFile() :  this(Path.GetTempPath())
        { }

        public TemporaryFile(string directory)
        {
            Create(Path.Combine(directory, Path.GetRandomFileName()));
        }

        public void Dispose()
        {
            Delete();
        }

        public void Write(string contents)
        {
            if (FilePath == null)
            {
                throw new ObjectDisposedException(nameof(TemporaryFile));
            }

            if (string.IsNullOrEmpty(contents))
            {
                return;
            }

            File.AppendAllText(FilePath, contents);
        }

        public string FilePath { get; private set; }

        private void Create(string path)
        {
            FilePath = path;
            using (File.Create(FilePath)) { };
        }

        private void Delete()
        {
            if (FilePath == null) return;
            File.Delete(FilePath);
            FilePath = null;
        }
    }
}
