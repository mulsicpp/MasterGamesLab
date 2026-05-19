using System.Collections.Generic;
using System.Reflection;
using Map;
using Map.GeometryGeneration;

public class GraphSetupPass : IGenerationPass
{
    public void Execute(MapData data)
    {
        FieldInfo centerField = typeof(Tile).GetField("center", BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var tile in data.Map.Tiles)
        {
            //initially set all tiles to water
            tile.Type = Tile.TileType.Water;

            //midpoint of hexagon
            Point centerPoint = (Point)centerField.GetValue(tile);

            //neighboring tiles of the current tile
            HashSet<Tile> actualNeighbors = new HashSet<Tile>();

            //triangle neighbors of the midpoint of the current tile
            foreach (Triangle triangle in centerPoint.Neighbors)
            {
                foreach (Point p in triangle.Points)
                {
                    if (p.Id != tile.Id)
                    {
                        //point.Id is the same as the tile.Id of the tile that contains this point
                        actualNeighbors.Add(data.Map.Tiles[p.Id]);
                    }
                }
            }
            data.TileNeighbors[tile] = new List<Tile>(actualNeighbors);
        }
    }
}