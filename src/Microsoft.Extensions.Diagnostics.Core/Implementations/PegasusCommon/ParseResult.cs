using System;

namespace Pegasus.Common
{
    public class ParseResult<T> : IParseResult<T>, IEquatable<ParseResult<T>>
    {
        private readonly Cursor endCursor;

        private readonly Cursor startCursor;

        private readonly T value;

        public Cursor EndCursor
        {
            get
            {
                return this.endCursor;
            }
        }

        public Cursor StartCursor
        {
            get
            {
                return this.startCursor;
            }
        }

        public T Value
        {
            get
            {
                return this.value;
            }
        }

        public ParseResult(Cursor startCursor, Cursor endCursor, T value)
        {
            this.startCursor = startCursor;
            this.endCursor = endCursor;
            this.value = value;
        }

        public static bool operator !=(ParseResult<T> left, ParseResult<T> right)
        {
            return !object.Equals(left, right);
        }

        public static bool operator ==(ParseResult<T> left, ParseResult<T> right)
        {
            return object.Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ParseResult<T>);
        }

        public bool Equals(ParseResult<T> other)
        {
            return !object.ReferenceEquals(other, null) && this.startCursor == other.startCursor && this.endCursor == other.endCursor && object.Equals(this.value, other.value);
        }

        public override int GetHashCode()
        {
            int num = 1374496523;
            num = num * -626349353 + this.startCursor.GetHashCode();
            num = num * -626349353 + this.endCursor.GetHashCode();
            int arg_5F_0 = num * -626349353;
            int arg_5F_1;
            if (!object.ReferenceEquals(this.value, null))
            {
                T t = this.value;
                arg_5F_1 = t.GetHashCode();
            }
            else
            {
                arg_5F_1 = 0;
            }
            return arg_5F_0 + arg_5F_1;
        }
    }
}
