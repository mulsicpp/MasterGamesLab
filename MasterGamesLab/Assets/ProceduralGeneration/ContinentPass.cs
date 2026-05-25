using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics; 
using Map;

public class ContinentPass : IGenerationPass
{
    //continet settings
    public float totalLandPercentage = 0.35f;

    //for main continent body
    public float noiseScale = 1.5f;
    public float mainNoiseScale = 1.2f;
    public int mainOrganicHarshness = 3;

  
    //for extra landmasses
    public float extraNoiseScale = 2.5f;
    public int extraOrganicHarshness = 6;

    //how big are the continents
    public Dictionary<int, int> ContinentSizes = new Dictionary<int, int>();

    public void Execute(IMap map)
    {
        var totalTiles = map.Tiles.Count;
        var targetTotalLand = Mathf.FloorToInt(totalTiles * totalLandPercentage);
        //var currentLandTiles = 0;

        //random chance for extra landmasses
        var randomChance = UnityEngine.Random.Range(0, 100);
        var extraCount = 0;
        if (randomChance > 80) extraCount = 2;       //20% 2 extra 
        else if (randomChance > 40) extraCount = 1;

        //main continent
        var mainContinentSize = Mathf.FloorToInt(targetTotalLand * 0.40f);
        var remainingLand = targetTotalLand - (mainContinentSize * 2);
        var extraContinentSize = extraCount > 0 ? (remainingLand / extraCount) : 0;

        ContinentSizes.Clear();
        //List<ITile> frontier = new List<ITile>();

        //seeds startpoints 
        var seed1 = map.Tiles[UnityEngine.Random.Range(0, totalTiles)];
        var seed2 = GetClosestTile(map, -seed1.PositionOnSphere);

        //seed1.Type = Tile.TileType.Plain;
        //seed2.Type = Tile.TileType.Plain;
        var count1 = GrowContinent(seed1, 1, mainContinentSize, mainNoiseScale, mainOrganicHarshness);
        ContinentSizes[1] = count1;

        var count2 = GrowContinent(seed2, 2, mainContinentSize, mainNoiseScale, mainOrganicHarshness);
        ContinentSizes[2] = count2;

        //extra landmasses
        for (var i = 0; i < extraCount; i++)
        {
            var continentId = 3 + i;
            var extraSeed = GetRandomWaterTileFarFromOthers(map, new List<ITile> { seed1, seed2 });
            var countExtra = GrowContinent(extraSeed, continentId, extraContinentSize, extraNoiseScale, extraOrganicHarshness);
            ContinentSizes[continentId] = countExtra;
        }

        Debug.Log($"extraContinentCount: {extraCount}");
        foreach (var kvp in ContinentSizes)
        {
            Debug.Log($"Continent {kvp.Key} has {kvp.Value} Land-Tiles.");
        }
    }

    //frontier.Add(seed1);
    //frontier.Add(seed2);
    //currentLandTiles += 2;

    //random offset for noise
    private int GrowContinent(ITile seed, int continentId, int targetSize, float noiseScale, int organicHarshness)
    {
        var frontier = new List<ITile>();
        var currentLandTiles = 0;

        if (seed.Type == Tile.TileType.Water)
        {
            seed.Type = Tile.TileType.Plain;
            seed.ContinentId = continentId; 
            frontier.Add(seed);
            currentLandTiles++;
        }

        var noiseOffset = new float3(
            UnityEngine.Random.Range(-100f, 100f),
            UnityEngine.Random.Range(-100f, 100f),
            UnityEngine.Random.Range(-100f, 100f)
        );

        while (frontier.Count > 0 && currentLandTiles < targetSize)
        {
            var bestIndex = 0;
            var bestNoise = -999f;

            //organicHarshness is the number of random samples taken from the frontier for expansion
            var samples = Mathf.Min(frontier.Count, organicHarshness);
            for (var i = 0; i < samples; i++)
            {
                var randIdx = UnityEngine.Random.Range(0, frontier.Count);
                var testTile = frontier[randIdx];

                var pos = new float3(testTile.PositionOnSphere.x, testTile.PositionOnSphere.y, testTile.PositionOnSphere.z);

                //3D Noise 
                var n = noise.snoise((pos * noiseScale) + noiseOffset);

                if (n > bestNoise)
                {
                    bestNoise = n;
                    bestIndex = randIdx;
                }
            }

            var current = frontier[bestIndex];

            //find water neighbors
            var waterNeighbors = new List<ITile>();
            foreach (var neighbor in current.Neighbors)
            {
                if (neighbor.Type == Tile.TileType.Water)
                {
                    waterNeighbors.Add(neighbor);
                }
            }

            if (waterNeighbors.Count == 0)
            {
                //remove from frontier if no water neighbors
                frontier.RemoveAt(bestIndex);
            }
            else
            {
                //expand to a random water neighbor
                var nextLand = waterNeighbors[UnityEngine.Random.Range(0, waterNeighbors.Count)];
                nextLand.Type = Tile.TileType.Plain;
                nextLand.ContinentId = continentId;
                currentLandTiles++;

                frontier.Add(nextLand);
            }
        }

        Debug.Log($"Land-Tiles: {currentLandTiles}");
        return currentLandTiles;
    }

    private ITile GetClosestTile(IMap map, Vector3 targetPos)
    {
        ITile closest = null;
        var minDist = float.MaxValue;

        foreach (var tile in map.Tiles)
        {
            var dist = Vector3.Distance(tile.PositionOnSphere, targetPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = tile;
            }
        }
        return closest;
    }

    private ITile GetRandomWaterTileFarFromOthers(IMap map, List<ITile> otherSeeds)
    {
        ITile bestTile = null;
        var maxDist = -1f;

        //50 random samples to find a water tile far from existing seeds
        for (var i = 0; i < 50; i++)
        {
            var testTile = map.Tiles[UnityEngine.Random.Range(0, map.Tiles.Count)];
            if (testTile.Type != Tile.TileType.Water) continue;

            var closestToOther = float.MaxValue;
            foreach (var other in otherSeeds)
            {
                var dist = Vector3.Distance(testTile.PositionOnSphere, other.PositionOnSphere);
                if (dist < closestToOther) closestToOther = dist;
            }

            if (closestToOther > maxDist)
            {
                maxDist = closestToOther;
                bestTile = testTile;
            }
        }

        //if no water tile found, return a random tile
        return bestTile ?? map.Tiles[UnityEngine.Random.Range(0, map.Tiles.Count)];
    }
}