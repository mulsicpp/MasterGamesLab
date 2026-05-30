using System.Collections.Generic;
using UnityEngine;
using Map;

public class CellularAutomataPass : IGenerationPass
{
    public int iterations = 2;

    //how many same-type neighbors
    public int survivalThreshold = 2;

    //how many same-type neighbors to turn a plain 
    public int birthThreshold = 4;

    public void Execute(IMap map)
    {
        for (var i = 0; i < iterations; i++)
        {
            var nextState = new Dictionary<ITile, Tile.TileType>();

            foreach (var tile in map.Tiles)
            {
                if (tile.Type == Tile.TileType.Water) continue;

                var currentType = tile.Type;

                //surviving tiles 
                if (currentType == Tile.TileType.Forest)
                {
                    var sameTypeCount = 0;
                    foreach (var neighbor in tile.Neighbors)
                    {
                        if (neighbor.Type == currentType)
                        {
                            sameTypeCount++;
                        }
                    }

                    if (sameTypeCount < survivalThreshold)
                    {
                        nextState[tile] = Tile.TileType.Plain;
                    }
                }

                //expanding tiles
                else if (currentType == Tile.TileType.Plain)
                {
                    var forestCount = 0;
                    var mountainCount = 0;

                    foreach (var neighbor in tile.Neighbors)
                    {
                        if (neighbor.Type == Tile.TileType.Forest) forestCount++;
                        else if (neighbor.Type == Tile.TileType.Mountain) mountainCount++;
                    }

                    if (forestCount >= birthThreshold)
                    {
                        nextState[tile] = Tile.TileType.Forest;
                    }
                    else if (mountainCount >= birthThreshold)
                    {
                        nextState[tile] = Tile.TileType.Mountain;
                    }
                }
            }

            foreach (var kvp in nextState)
            {
                kvp.Key.Type = kvp.Value;
            }
        }
    }
}