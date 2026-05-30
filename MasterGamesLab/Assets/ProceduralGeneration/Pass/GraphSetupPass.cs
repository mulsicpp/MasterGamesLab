using Map;

public class GraphSetupPass : IGenerationPass
{
    public void Execute(IMap map)
    {
        foreach (var tile in map.Tiles)
        {
            // Setze initial alle Tiles auf Wasser
            tile.Type = Tile.TileType.Water;
        }
    }
}