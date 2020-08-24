using System;
using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class NullPropertyEvaluator : EventPropertyExpressionEvaluator
    {
        public NullPropertyEvaluator(string propertyName, string value) : base(propertyName, value)
        {
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__EqualityEvaluator:{0} is {1})", this.propertyName, this.value); }
        }

        public override bool Evaluate(EventData e)
        {
            bool ignored;
            return EvaluateEquality(e, out ignored);
        }

        protected bool EvaluateEquality(EventData e, out bool fullyEvaluated)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            fullyEvaluated = true;

            if (!e.TryGetPropertyValue(this.propertyName, out _))
            {
                return true;
            }

            return false;
        }
    }
}