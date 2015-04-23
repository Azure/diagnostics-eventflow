using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var proxy = ActorProxy.Create<IAirTrafficControl>(ActorId.NewId(), "fabric:/AirTrafficControlApplication");

            int count = 10;
            Console.WriteLine("Setting Count to in Actor {0}: {1}", proxy.GetActorId(), count);
            proxy.SetCountAsync(count).Wait();

            Console.WriteLine("Count from Actor {1}: {0}", proxy.GetActorId(), proxy.GetCountAsync().Result);
        }
    }
}
