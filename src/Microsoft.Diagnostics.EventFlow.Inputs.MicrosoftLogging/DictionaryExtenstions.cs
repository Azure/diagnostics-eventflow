using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{

    internal static class DictionaryExtenstions
    {
        internal class AddResult
        {
            public readonly bool KeyChanged;
            public readonly string OldKey;
            public readonly string NewKey;

            public AddResult()
            {
                KeyChanged = false;
            }

            public AddResult(string oldKey, string newKey)
            {
                this.KeyChanged = true;
                this.OldKey = oldKey;
                this.NewKey = newKey;
            }
        }

        public static AddResult AddOrDuplicate(this IDictionary<string, object> dictionary, KeyValuePair<string, object> pair)
        {
            return AddOrDuplicate(dictionary, pair.Key, pair.Value);
        }

        public static AddResult AddOrDuplicate(this IDictionary<string, object> dictionary,  string key,  object value)
        {
            Validation.Requires.NotNull(dictionary, nameof(dictionary));
            Validation.Requires.NotNull(key, nameof(key));

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return new AddResult();
            }

            Random random = new Random(DateTime.Now.Millisecond);
            string newKey = key + "_";
            //update property key till there are no such key in dict
            do
            {
                newKey += random.Next(0, 10);
            }
            while (dictionary.ContainsKey(newKey));

            dictionary.Add(newKey, value);
            return new AddResult(key, newKey);
        }
    }
}
