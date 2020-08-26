using System;
using System.Globalization;

namespace Microsoft.Diagnostics.EventFlow.FilterEvaluators
{
    internal class HasPropertyEvaluator : FilterEvaluator
    {
        private readonly string propertyName;

        public HasPropertyEvaluator(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public override string SemanticsString
        {
            get { return string.Format(CultureInfo.InvariantCulture, "(__EqualityEvaluator:hasproperty {0})", this.propertyName); }
        }

        public override bool Evaluate(EventData e)
        {
            return EvaluateEquality(e);
        }

        protected bool EvaluateEquality(EventData e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            return e.TryGetPropertyValue(this.propertyName, out _);
        }
    }
}