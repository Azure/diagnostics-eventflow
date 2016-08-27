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

using System;
using System.Collections.Generic;
using System.Threading;

namespace Pegasus.Common
{
    public class Cursor : IEquatable<Cursor>
    {
        private static int previousStateKey = -1;

        private readonly int column;

        private readonly string fileName;

        private readonly bool inTransition;

        private readonly int line;

        private readonly int location;

        private readonly bool mutable;

        private readonly IDictionary<string, object> state;

        private readonly string subject;

        private int stateKey;

        public int Column
        {
            get
            {
                return this.column;
            }
        }

        public string FileName
        {
            get
            {
                return this.fileName;
            }
        }

        public int Line
        {
            get
            {
                return this.line;
            }
        }

        public int Location
        {
            get
            {
                return this.location;
            }
        }

        public int StateKey
        {
            get
            {
                return this.stateKey;
            }
        }

        public string Subject
        {
            get
            {
                return this.subject;
            }
        }

        public dynamic this[string key]
        {
            get
            {
                object result;
                this.state.TryGetValue(key, out result);
                return result;
            }
            set
            {
                if (!this.mutable)
                {
                    throw new InvalidOperationException();
                }
                this.stateKey = Cursor.GetNextStateKey();
                this.state[key] = value;
            }
        }

        public Cursor(string subject, int location, string fileName = null)
        {
            if (subject == null)
            {
                throw new ArgumentNullException("subject");
            }
            if (location < 0 || location > subject.Length)
            {
                throw new ArgumentOutOfRangeException("location");
            }
            this.subject = subject;
            this.location = location;
            this.fileName = fileName;
            int num = 1;
            int num2 = 1;
            bool flag = false;
            Cursor.TrackLines(this.subject, 0, location, ref num, ref num2, ref flag);
            this.line = num;
            this.column = num2;
            this.inTransition = flag;
            this.state = new Dictionary<string, object>();
            this.stateKey = Cursor.GetNextStateKey();
            this.mutable = false;
        }

        private Cursor(string subject, int location, string fileName, int line, int column, bool inTransition, IDictionary<string, object> state, int stateKey, bool mutable)
        {
            this.subject = subject;
            this.location = location;
            this.fileName = fileName;
            this.line = line;
            this.column = column;
            this.inTransition = inTransition;
            this.state = state;
            this.stateKey = stateKey;
            this.mutable = mutable;
        }

        public static bool operator !=(Cursor left, Cursor right)
        {
            return !object.Equals(left, right);
        }

        public static bool operator ==(Cursor left, Cursor right)
        {
            return object.Equals(left, right);
        }

        public Cursor Advance(int count)
        {
            if (this.mutable)
            {
                throw new InvalidOperationException();
            }
            int num = this.line;
            int num2 = this.column;
            bool flag = this.inTransition;
            Cursor.TrackLines(this.subject, this.location, count, ref num, ref num2, ref flag);
            return new Cursor(this.subject, this.location + count, this.fileName, num, num2, flag, this.state, this.stateKey, this.mutable);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Cursor);
        }

        public bool Equals(Cursor other)
        {
            return !object.ReferenceEquals(other, null) && this.location == other.location && this.subject == other.subject && this.fileName == other.fileName && this.stateKey == other.stateKey;
        }

        public override int GetHashCode()
        {
            int num = 1374496523;
            num = num * -626349353 + this.subject.GetHashCode();
            num = num * -626349353 + this.location.GetHashCode();
            num = num * -626349353 + ((this.fileName == null) ? 0 : this.fileName.GetHashCode());
            return num * -626349353 + this.stateKey;
        }

        public Cursor Touch()
        {
            return new Cursor(this.subject, this.location, this.fileName, this.line, this.column, this.inTransition, this.mutable ? new Dictionary<string, object>(this.state) : this.state, Cursor.GetNextStateKey(), this.mutable);
        }

        public Cursor WithMutability(bool mutable)
        {
            return new Cursor(this.subject, this.location, this.fileName, this.line, this.column, this.inTransition, new Dictionary<string, object>(this.state), this.stateKey, mutable);
        }

        private static int GetNextStateKey()
        {
            return Interlocked.Increment(ref Cursor.previousStateKey);
        }

        private static void TrackLines(string subject, int start, int count, ref int line, ref int column, ref bool inTransition)
        {
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                char c = subject[start + i];
                if (c == '\r' || c == '\n')
                {
                    if (inTransition)
                    {
                        inTransition = false;
                        line++;
                        column = 1;
                    }
                    else if (subject.Length <= start + i + 1)
                    {
                        line++;
                        column = 1;
                    }
                    else
                    {
                        char c2 = subject[start + i + 1];
                        if ((c == '\r' && c2 == '\n') || (c == '\n' && c2 == '\r'))
                        {
                            inTransition = true;
                            column++;
                        }
                        else
                        {
                            line++;
                            column = 1;
                        }
                    }
                }
                else if (c == '\u2028' || c == '\u2029')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
        }
    }
}