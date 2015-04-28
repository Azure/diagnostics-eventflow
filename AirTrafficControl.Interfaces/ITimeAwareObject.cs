using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    public interface ITimeAwareObject
    {
        /// <summary>
        /// The object should compute and apply new state, based on busines rules and instructions received.
        /// </summary>
        Task TimePassed();
    }
}
