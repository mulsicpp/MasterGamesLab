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
                fastestRoute = cheapestRoute;
                cheapestRoute = null;
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

                UpdateFacingDirections();

                return;
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


            Destination?.ClearOutline();
            Destination = destination;

            FastestRoute.SetRoute(fastestRoute, fastestPinPosition);
            CheapestRoute.SetRoute(cheapestRoute, cheapestPinPosition);

            UpdateFacingDirections();
        }

        public void Clear()
        {
            Destination?.ClearOutline();
            Destination = null;
            FastestRoute.SetRoute(null);
            CheapestRoute.SetRoute(null);
        }

        public void UpdateFacingDirections()
        {
            if (FastestRoute.Tiles == null) return;

            if(CheapestRoute.Tiles == null)
            {
                FastestRoute.Renderer.Pin.FacingLeft = false;
                return;
            }

            var camera = MainCamera.Instance.GetComponentInChildren<Camera>();

            var fastestPosition = FastestRoute.Renderer.Pin.transform.position;
            var cheapestPosition = CheapestRoute.Renderer.Pin.transform.position;

            bool cheapestFacingLeft = camera.WorldToScreenPoint(cheapestPosition).x < camera.WorldToScreenPoint(fastestPosition).x;

            FastestRoute.Renderer.Pin.FacingLeft = !cheapestFacingLeft;
            CheapestRoute.Renderer.Pin.FacingLeft = cheapestFacingLeft;
        }
    }
}