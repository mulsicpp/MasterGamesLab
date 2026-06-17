using System.Collections.Generic;
using UnityEngine;
using Map;

public class LakePass : IGenerationPass
{
    public int TargetLakeCount = 3;
    public int MinDistanceToOcean = 8;

    public int MinLakeSize = 8;
    public int MaxLakeSize = 15;

    public void Execute(IMap map)
    {
        var distanceMap = new Dictionary<ITile, int>();
        var queue = new Queue<ITile>();

        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water)
            {
                distanceMap[tile] = 0;
                queue.Enqueue(tile);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distanceMap[current];

            foreach (var neighbor in current.Neighbors)
            {
                if (!distanceMap.ContainsKey(neighbor))
                {
                    distanceMap[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        var validInlandCenters = new List<ITile>();
        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Plain &&
                distanceMap.ContainsKey(tile) &&
                distanceMap[tile] >= MinDistanceToOcean)
            {
                validInlandCenters.Add(tile);
            }
        }

        //spawn lakes
        int spawnedLakes = 0;
        int safetyBreak = 0;

        while (spawnedLakes < TargetLakeCount && validInlandCenters.Count > 0 && safetyBreak < 100)
        {
            safetyBreak++;

            int randIndex = UnityEngine.Random.Range(0, validInlandCenters.Count);
            var lakeCenter = validInlandCenters[randIndex];

            if (lakeCenter.Type != Tile.TileType.Plain)
            {
                validInlandCenters.RemoveAt(randIndex);
                continue;
            }

            //random floodfill 
            int targetSize = UnityEngine.Random.Range(MinLakeSize, MaxLakeSize + 1);
            var lakeTiles = new HashSet<ITile> { lakeCenter };
            var frontier = new List<ITile>();

            foreach (var neighbor in lakeCenter.Neighbors)
            {
                if (neighbor.Type == Tile.TileType.Plain) frontier.Add(neighbor);
            }

            while (lakeTiles.Count < targetSize && frontier.Count > 0)
            {
                int rIdx = UnityEngine.Random.Range(0, frontier.Count);
                var nextTile = frontier[rIdx];
                frontier.RemoveAt(rIdx);

                if (lakeTiles.Contains(nextTile)) continue;

                lakeTiles.Add(nextTile);

                foreach (var n in nextTile.Neighbors)
                {
                    //mot close to ocean
                    if (n.Type == Tile.TileType.Plain && !lakeTiles.Contains(n) && distanceMap.ContainsKey(n) && distanceMap[n] >= 2)
                    {
                        frontier.Add(n);
                    }
                }
            }

            foreach (var tile in lakeTiles)
            {
                tile.Type = Tile.TileType.Water;
                tile.ContinentId = -1;
            }

            spawnedLakes++;

            validInlandCenters.RemoveAll(t => Vector3.Distance(t.PositionOnSphere, lakeCenter.PositionOnSphere) < 0.2f);
        }

    }
}