using Map.Hoverables;
using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        private IHoverable lastHoveredThing;

        private void Update()
        {
            if (lastHoveredThing != null)
            {
                switch (lastHoveredThing)
                {
                    case ITile t:
                        // lastActiveTile.Active = false;
                        foreach (var n in t.Neighbors)
                        {
                            n.Active = false;
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
                    foreach (var n in t.Neighbors)
                    {
                        n.Active = true;
                    }

                    break;
                case Edge e:
                    Debug.Log("Edge is hovered");
                    break;
                default:
                    break;
            }

            // tile.Active = true;

            lastHoveredThing = tile;
        }
    }
}