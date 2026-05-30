using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Map;

public static class SpawnPointGenerator
{
    public static ITile[] GetFairSpawnPoints(IMap map, int playerCount = 4)
    {
        var continent1Tiles = new List<ITile>();
        var continent2Tiles = new List<ITile>();

        foreach (var tile in map.Tiles)
        {
           //only plain
            if (tile.Type == Tile.TileType.Water || tile.Type == Tile.TileType.Mountain)
                continue;

            if (tile.ContinentId == 1) continent1Tiles.Add(tile);
            else if (tile.ContinentId == 2) continent2Tiles.Add(tile);
        }

        //choose main cont with most tiles
        var targetContinent = continent1Tiles.Count > continent2Tiles.Count ? continent1Tiles : continent2Tiles;

        if (targetContinent.Count < playerCount)
        {
            return new ITile[0];
        }

        //center point of continent
        var centerPos = Vector3.zero;
        foreach (var tile in targetContinent)
        {
            centerPos += tile.PositionOnSphere;
        }
        centerPos /= targetContinent.Count;

        //radius to coastline
        var maxDistanceToCenter = 0f;
        foreach (var tile in targetContinent)
        {
            var dist = Vector3.Distance(tile.PositionOnSphere, centerPos);
            if (dist > maxDistanceToCenter) maxDistanceToCenter = dist;
        }

        //half radius
        var idealSpawnRadius = maxDistanceToCenter * 0.5f;

        var tileScores = new Dictionary<ITile, float>();
        foreach (var tile in targetContinent)
        {
            var distToCenter = Vector3.Distance(tile.PositionOnSphere, centerPos);
            //0 best
            var error = Mathf.Abs(distToCenter - idealSpawnRadius);
            tileScores[tile] = error;
        }

        var sortedCandidates = tileScores.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

        //best 1/4 of tiles to choose for distance
        var poolSize = Mathf.Max(playerCount, sortedCandidates.Count / 4);
        var ringPool = sortedCandidates.Take(poolSize).ToList();

        var spawnPoints = new List<ITile>();

        //1st player
        spawnPoints.Add(ringPool[0]);
        ringPool.RemoveAt(0);

        //rest player
        while (spawnPoints.Count < playerCount && ringPool.Count > 0)
        {
            ITile bestNextSpawn = null;
            var maxDistanceToOthers = -1f;

            foreach (var candidate in ringPool)
            {
                var minDistanceToAnySpawn = float.MaxValue;

                foreach (var spawn in spawnPoints)
                {
                    var dist = Vector3.Distance(candidate.PositionOnSphere, spawn.PositionOnSphere);
                    if (dist < minDistanceToAnySpawn)
                    {
                        minDistanceToAnySpawn = dist;
                    }
                }

                if (minDistanceToAnySpawn > maxDistanceToOthers)
                {
                    maxDistanceToOthers = minDistanceToAnySpawn;
                    bestNextSpawn = candidate;
                }
            }

            spawnPoints.Add(bestNextSpawn);
            ringPool.Remove(bestNextSpawn);
        }

        return spawnPoints.ToArray();
    }
}