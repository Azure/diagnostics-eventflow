using System;
using System.Collections.Generic;
using Validation;

namespace AirTrafficControl.Interfaces
{
    public class Dispatcher
    {
        public IReadOnlyList<Fix> ComputeFlightPath(Airport departurePoint, Airport destination)
        {
            Requires.NotNull(departurePoint, nameof(departurePoint));
            Requires.NotNull(destination, nameof(destination));
            Assumes.True(departurePoint != destination, "Departure point must be different than destination");

            Queue<CandidatePath> candidates = new Queue<CandidatePath>();
            CandidatePath currentPath = new CandidatePath();
            currentPath.FlightPath = new List<Fix>(new Fix[] { departurePoint });
            currentPath.Cost = 0;
            candidates.Enqueue(currentPath);

            while(true)
            {
                // Dequeue candidate path
                // Find all fixes reachable from the end of the candidate path
                // Remove all fixes that we have already considered
                // If one of remaining fixes is destination, construct the final answer and return
                // Otherwise, construct new candidates, incrementing cost and enqueue
            }
        }
    }

    internal class CandidatePath
    {
        public List<Fix> FlightPath { get; set; }
        public int Cost { get; set; }        
    }
}
