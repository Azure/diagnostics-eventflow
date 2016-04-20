using System;
using System.Linq;
using System.Collections.Generic;
using Validation;

namespace AirTrafficControl.Interfaces
{
    public class Dispatcher
    {
        public static IList<Fix> ComputeFlightPath(Airport departurePoint, Airport destination)
        {
            Requires.NotNull(departurePoint, nameof(departurePoint));
            Requires.NotNull(destination, nameof(destination));
            Assumes.True(departurePoint != destination, "Departure point must be different than destination");

            Queue<List<Fix>> candidates = new Queue<List<Fix>>();
            List<Fix> fixesProcessed = new List<Fix>();

            var currentPath = new List<Fix>(new Fix[] { departurePoint });
            candidates.Enqueue(currentPath);
            fixesProcessed.Add(departurePoint);

            while(true)
            {
                currentPath = candidates.Dequeue();
                Fix currentPathEnd = currentPath[currentPath.Count - 1];
                var reachableFromCurrentPath = Universe.Current.GetAdjacentFixes(currentPathEnd);                

                if (reachableFromCurrentPath.Contains(destination))
                {
                    currentPath.Add(destination);
                    return currentPath.AsReadOnly();
                }

                var notConsideredYet = reachableFromCurrentPath.Except(fixesProcessed);
                foreach (var fix in notConsideredYet)
                {
                    var newPath = new List<Fix>(currentPath);
                    newPath.Add(fix);
                    fixesProcessed.Add(fix);
                    candidates.Enqueue(newPath);
                }
            }
        }
    }
}
