using InGameCamera;
using Map;
using Map.Fleet;
using Map.GeometryGeneration.Edges;
using UnityEngine;

namespace UI
{
    public class RouteOptions
    {
        public Tile Destination { get; private set; } = null;
        public Route FastestRoute { get; private set; } = null;
        public Route CheapestRoute { get; private set; } = null;
        public Tile LoadTile { get; private set; } = null;

        public Tile VisualDestination => LoadTile ?? Destination;

        public RouteOptions(bool preview)
        {
            if (preview)
            {
                FastestRoute = new(Route.RouteType.FastestPreview);
                FastestRoute.Renderer.PinVisible = false;
                CheapestRoute = new(Route.RouteType.CheapestPreview);
                CheapestRoute.Renderer.PinVisible = false;
            }
            else
            {
                FastestRoute = new(Route.RouteType.Fastest);
                CheapestRoute = new(Route.RouteType.Cheapest);
            }
        }

        public void Set(Vehicle vehicle, Tile destination, TileId[] fastestRoute, TileId[] cheapestRoute = null, Tile loadTile = null)
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
                VisualDestination?.ClearOutline();
                Destination = destination;

                FastestRoute.SetRoute(vehicle, fastestRoute);
                CheapestRoute.SetRoute(null, null);

                LoadTile = loadTile;

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


            VisualDestination?.ClearOutline();

            Destination = destination;
            LoadTile = loadTile;

            FastestRoute.SetRoute(vehicle, fastestRoute, fastestPinPosition);
            CheapestRoute.SetRoute(vehicle, cheapestRoute, cheapestPinPosition);

            UpdateFacingDirections();
        }

        public void Clear()
        {
            VisualDestination?.ClearOutline();
            Destination = null;
            FastestRoute.SetRoute(null, null);
            CheapestRoute.SetRoute(null, null);
            LoadTile = null;
        }

        public void UpdateFacingDirections()
        {
            if (FastestRoute.TileIds == null) return;

            if(CheapestRoute.TileIds == null)
            {
                FastestRoute.Renderer.Pin.FacingLeft = false;
                return;
            }

            var camera = MainCamera.Instance.GetComponentInChildren<Camera>();

            var fastestPosition = FastestRoute.Renderer.Pin.transform.position;
            var cheapestPosition = CheapestRoute.Renderer.Pin.transform.position;

            bool cheapestFacingLeft = camera.WorldToScreenPoint(cheapestPosition).x < camera.WorldToScreenPoint(fastestPosition).x;

            FastestRoute.Renderer.Pin.FacingLeft = cheapestFacingLeft;
            CheapestRoute.Renderer.Pin.FacingLeft = !cheapestFacingLeft;
        }
    }
}