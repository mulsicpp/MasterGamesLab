using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        ITile lastActiveTile;

        private void Update()
        {
            if (lastActiveTile != null)
            {
                // lastActiveTile.Active = false;
                foreach (var n in lastActiveTile.Neighbors)
                {
                    n.Active = false;
                }
            }

            var tile = Map.Instance.GetCurrentlyHoveredTile();
            if (tile != null)
            {
                // tile.Active = true;
                foreach (var n in tile.Neighbors)
                {
                    n.Active = true;
                }

                lastActiveTile = tile;
            }
        }
    }
}