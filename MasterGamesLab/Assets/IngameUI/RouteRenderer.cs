using Map;
using Map.Infrastructure;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class RouteRenderer : MonoBehaviour
    {
        public Route Route { get; private set; }

        public void Init(Route route)
        {
            Route = route;
        }

        public void OnDrawGizmos()
        {
            if(Route.Tiles != null)
            {
                Gizmos.color = Route.Color.linear;
                for (int i = 0; i < Route.Tiles.Length; i++)
                {
                    Vector3 pos = Map.Map.Instance.GetProjectedPosition(Map.Map.Instance.Tiles[Route.Tiles[i]].PositionOnSphere, 1.02f);
                    Gizmos.DrawSphere(pos, 0.01f);
                }
            }
        }
    }
}