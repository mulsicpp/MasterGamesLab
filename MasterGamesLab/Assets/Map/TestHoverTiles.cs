using Map.GeometryGeneration;
using Map.Hoverables;
using Map.OutlineEffect;
using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        private IHoverable lastHoveredThing;

        private TileBeacon tileBeacon;

        private void Awake()
        {
        }

        private void Update()
        {
            if (lastHoveredThing != null)
            {
                switch (lastHoveredThing)
                {
                    case ITile t:
                        if (tileBeacon != null)
                        {
                            TileBeaconPool.Instance.Release(tileBeacon);
                            tileBeacon = null;
                        }

                        break;
                    case Edge edge:
                        break;
                }
            }

            var tile = Map.Instance.CurrentlyHovered;
            if (tile == null) return;

            switch (tile)
            {
                case ITile t:
                    tileBeacon = TileBeaconPool.Instance.Get();
                    tileBeacon.HighlightTile((Tile)t);
                    tileBeacon.SetCustomColor(Color.red);
                    break;
                case Edge e:
                    break;
                default:
                    break;
            }

            // tile.Active = true;

            lastHoveredThing = tile;
        }
    }
}