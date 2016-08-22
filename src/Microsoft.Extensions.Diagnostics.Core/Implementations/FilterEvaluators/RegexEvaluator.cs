//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Diagnostics.FilterEvaluators
{
    internal class RegexEvaluator : EventPropertyExpressionEvaluator
    {
        private const string Iso8601DurationFormat = "o";

        // If it takes more than 100 ms to determine whether the regex matches the property value, we consider an unsuccessful match.
        // This way, if the user types in some horrible regular expression, the pipeline throughput won't be affected too much.
        public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

        private Regex regex;

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__RegexEvaluator:{0}~={1})", this.propertyName, this.value); }
        }

        public RegexEvaluator(string propertyName, string value) : base(propertyName, value)
        {
            try
            {
                // Even though compiled regular expressions execute faster, we do not use them here.
                // This is because each compiled regex involves an overhead of about 10kB of unrecoverable memory.
                this.regex = new Regex(this.value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
            }
            catch { }
        }

        public override bool Evaluate(EventData e)
        {
            if (this.regex == null)
            {
                // Invalid regex means no match.
                return false;
            }

            object propertyValue;
            if (!e.TryGetPropertyValue(this.propertyName, out propertyValue))
            {
                return false;
            }

            string propertyValueString;
            if (propertyValue is string)
            {
                propertyValueString = (string)propertyValue;
            }
            else if (propertyValue is DateTimeOffset)
            {
                propertyValueString = ((DateTimeOffset)propertyValue).ToString(Iso8601DurationFormat, CultureInfo.InvariantCulture);
            }
            else if (propertyValue is DateTime)
            {
                propertyValueString = ((DateTime)propertyValue).ToString(Iso8601DurationFormat, CultureInfo.InvariantCulture);
            }
            else if (propertyValue is Guid)
            {
                propertyValueString = ((Guid)propertyValue).ToString();
            }
            else
            {
                // This evaluator does not handle any other types.
                return false;
            }

            bool isMatch = false;
            try
            {
                isMatch = this.regex.IsMatch(propertyValueString);
            }
            catch { }

            return isMatch;
        }
    }
}
