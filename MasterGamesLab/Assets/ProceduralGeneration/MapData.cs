using System.Collections.Generic;
using Map;

public class MapData
{
    public IMap Map { get; private set; }
    public Dictionary<ITile, List<ITile>> TileNeighbors { get; set; }

    public MapData(IMap map)
    {
        Map = map;
        TileNeighbors = new Dictionary<ITile, List<ITile>>();
    }
}