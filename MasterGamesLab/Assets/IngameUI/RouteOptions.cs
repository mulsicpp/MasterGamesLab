using InGameCamera;
using Map;
using UnityEngine;

namespace UI
{
    public class RouteOptions
    {
        public Tile Destination { get; private set; } = null;
        public Route FastestRoute { get; private set; } = null;
        public Route CheapestRoute { get; private set; } = null;

        public RouteOptions()
        {
            FastestRoute = new(null, Color.orange);
            CheapestRoute = new(null, Color.green);
        }

        public void Set(Tile destination, TileId[] fastestRoute, TileId[] cheapestRoute = null)
        {
            if (fastestRoute == null)
            {
                var temp = fastestRoute;
                fastestRoute = cheapestRoute;
                cheapestRoute = temp;
            }

            if(fastestRoute == null)
            {
                Clear();
                return;
            }

            if (cheapestRoute == null || Route.AreSame(fastestRoute, cheapestRoute))
            {
                Destination?.ClearOutline();
                Destination = destination;

                FastestRoute.SetRoute(fastestRoute);
                CheapestRoute.SetRoute(null);
            }

            int divergenceIndex = 0;
            int minLength = Mathf.Min(fastestRoute.Length, cheapestRoute.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (fastestRoute[i] != cheapestRoute[i])
                {
                    // The tile behind this one was the last shared tile
                    divergenceIndex = Mathf.Max(0, i - 1);
                    break;
                }
            }

            // 4. Find where the cheapest route converges back onto the fastest route (searching backward)
            int cheapestConvergenceIndex = cheapestRoute.Length - 1;
            int fastestConvergenceIndex = fastestRoute.Length - 1;

            for (int i = divergenceIndex + 1; i < cheapestRoute.Length; i++)
            {
                for (int j = divergenceIndex + 1; j < fastestRoute.Length; j++)
                {
                    if (cheapestRoute[i] == fastestRoute[j])
                    {
                        cheapestConvergenceIndex = i;
                        fastestConvergenceIndex = j;
                        goto A;
                    }
                }
            }
            A:

            // 5. Place the alternative label right in the middle of that isolated detour section
            var cheapestPinPosition = Route.GetRouteMidpoint(cheapestRoute, divergenceIndex, cheapestConvergenceIndex);
            var fastestPinPosition = Route.GetRouteMidpoint(fastestRoute, divergenceIndex, fastestConvergenceIndex);

            var camera = MainCamera.Instance.GetComponentInChildren<Camera>();
            bool cheapestFacingLeft = camera.WorldToScreenPoint(cheapestPinPosition).x < camera.WorldToScreenPoint(fastestPinPosition).x;


            Destination?.ClearOutline();
            Destination = destination;

            FastestRoute.SetRoute(fastestRoute, fastestPinPosition, !cheapestFacingLeft);
            CheapestRoute.SetRoute(cheapestRoute, cheapestPinPosition, cheapestFacingLeft);
        }

        public void Clear()
        {
            Destination?.ClearOutline();
            Destination = null;
            FastestRoute.SetRoute(null);
            CheapestRoute.SetRoute(null);
        }
    }
}