using System;

namespace Pegasus.Common
{
    public interface ILexical
    {
        Cursor EndCursor
        {
            get;
            set;
        }

        Cursor StartCursor
        {
            get;
            set;
        }
    }
}
