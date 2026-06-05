using Map.Hoverables;
using Map.OutlineEffect;
using UnityEngine;

namespace Map
{
    public class TestHoverTiles : MonoBehaviour
    {
        private IHoverable lastHoveredThing;

        [SerializeField] private GameObject tileOutlinerPrefab;

        private TileOutliner tileOutliner;

        private void Awake()
        {
            var go = Instantiate(tileOutlinerPrefab, transform);
            tileOutliner = go.GetComponent<TileOutliner>();
        }

        private void Update()
        {
            if (lastHoveredThing != null)
            {
                switch (lastHoveredThing)
                {
                    case ITile t:
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
                    tileOutliner.OutlineTile((Tile)t);
                    tileOutliner.SetOutlineParameters(Color.black, new Color(0, 0, 0, 0), 0);
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