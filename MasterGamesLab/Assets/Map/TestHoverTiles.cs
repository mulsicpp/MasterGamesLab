using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        void Update()
        {
            var tile = Map.Instance.GetCurrentlyHoveredTile();
            if (tile != null)
            {
                Map.Instance.ActiveTiles = new List<Tile> { tile };
            }
        }
    }
}