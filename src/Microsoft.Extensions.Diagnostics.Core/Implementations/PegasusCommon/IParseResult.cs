using System;

namespace Pegasus.Common
{
    public interface IParseResult<out T>
    {
        Cursor EndCursor
        {
            get;
        }

        Cursor StartCursor
        {
            get;
        }

        T Value
        {
            get;
        }
    }
}
