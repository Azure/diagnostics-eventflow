using System;
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
