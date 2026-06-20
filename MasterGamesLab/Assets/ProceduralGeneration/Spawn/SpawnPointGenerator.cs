using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Map;

public static class SpawnPointGenerator
{
    public static ITile[] SpawnInitialStructures(IMap map, int numPlayers)
    {
        numPlayers = Mathf.Clamp(numPlayers, 1, 4);
        
        ITile[] playerSpawns = GetFairSpawnPoints(map, numPlayers);

        if (playerSpawns == null || playerSpawns.Length == 0)
        {
            return new ITile[0];
        }

        for (int i = 0; i < playerSpawns.Length; i++)
        {
            var spawnTile = playerSpawns[i];

            //player spawn
        }
        return playerSpawns;
    }
    public static ITile[] GetFairSpawnPoints(IMap map, int playerCount)
    {
        var targetContinent = new List<ITile>();

        foreach (var tile in map.Tiles)
        {
           //only plain
            if (tile.Type == Tile.TileType.Water || tile.Type == Tile.TileType.Mountain)
                continue;

            if (tile.ContinentId == 1) targetContinent.Add(tile);
        }

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

        var poolSize = Mathf.Max(playerCount, Mathf.FloorToInt(sortedCandidates.Count * 0.4f));
        var ringPool = sortedCandidates.Take(poolSize).ToList();


        var centerDir = centerPos.normalized;
        var right = Vector3.Cross(centerDir, Vector3.up);
        if (right.magnitude < 0.001f) right = Vector3.Cross(centerDir, Vector3.right); 
        right.Normalize();
        var forward = Vector3.Cross(right, centerDir).normalized;

        //angle relative to centerpoint 
        var tileAngles = new Dictionary<ITile, float>();
        foreach (var tile in ringPool)
        {
            var dir = (tile.PositionOnSphere - centerPos).normalized;
            var angleRad = Mathf.Atan2(Vector3.Dot(dir, right), Vector3.Dot(dir, forward));
            tileAngles[tile] = angleRad * Mathf.Rad2Deg;
        }

        var spawnPoints = new List<ITile>();

        //1st player
        var p1 = ringPool[0];
        spawnPoints.Add(p1);
        ringPool.Remove(p1);

        float p1Angle = tileAngles[p1];

        //angle step for remaining players
        float angleStep = 360f / playerCount;

        for (int i = 1; i < playerCount; i++)
        {
            float targetAngle = p1Angle + (i * angleStep);

            ITile bestTile = null;
            float minAngleDiff = float.MaxValue;

            foreach (var candidate in ringPool)
            {
                float cAngle = tileAngles[candidate];
                float diff = Mathf.Abs(Mathf.DeltaAngle(targetAngle, cAngle));

                if (diff < minAngleDiff)
                {
                    minAngleDiff = diff;
                    bestTile = candidate;
                }
            }

            spawnPoints.Add(bestTile);
            ringPool.Remove(bestTile); 
        }
        return spawnPoints.ToArray();
    }
}