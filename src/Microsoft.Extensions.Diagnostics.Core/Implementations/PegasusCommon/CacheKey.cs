// ------------------------------------------------------------
// Copyright © 2014 John Gietzen

// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------

using System.Diagnostics;

namespace Pegasus.Common
{
    [DebuggerDisplay("{ruleName}:{location}:{stateKey}")]
    public class CacheKey
    {
        private readonly int hash;

        private readonly int location;

        private readonly string ruleName;

        private readonly int stateKey;

        public CacheKey(string ruleName, int stateKey, int location)
        {
            this.ruleName = ruleName;
            this.stateKey = stateKey;
            this.location = location;
            int num = -2128831035;
            num = (num * 16777619 ^ ((this.ruleName == null) ? 0 : this.ruleName.GetHashCode()));
            num = (num * 16777619 ^ this.stateKey);
            num = (num * 16777619 ^ this.location);
            this.hash = num;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }
            CacheKey cacheKey = obj as CacheKey;
            return !object.ReferenceEquals(cacheKey, null) && (this.location == cacheKey.location && this.stateKey == cacheKey.stateKey) && this.ruleName == cacheKey.ruleName;
        }

        public override int GetHashCode()
        {
            return this.hash;
        }
    }
}
