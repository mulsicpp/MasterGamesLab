using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        Tile lastActiveTile;

        private void Update()
        {
            if (lastActiveTile != null)
            {
                lastActiveTile.Active = false;
            }

            var tile = Map.Instance.GetCurrentlyHoveredTile();
            if (tile != null)
            {
                tile.Active = true;
                lastActiveTile = tile;
            }
        }
    }
}