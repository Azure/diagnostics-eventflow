using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    public interface ITimeAwareObject
    {
        Task TimePassed(int currentTime);
    }
}
