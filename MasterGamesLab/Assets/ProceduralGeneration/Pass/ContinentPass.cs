using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics; 
using Map;

public class ContinentPass : IGenerationPass
{
    //continet settings
    //public float totalLandPercentage = 0.35f;
    public float totalLandPercentage = 0.48f;

    public int minContinentDistance = 3;

    //for main continent body
    //public float noiseScale = 1.5f;
    public float mainNoiseScale = 1.2f;
    public int mainOrganicHarshness = 3;

    //lower value= longer continent 
    public float mainDistancePenalty = 1.2f;
    public float extraDistancePenalty = 0.1f;

    //for extra landmasses
    public float extraNoiseScale = 2.5f;
    public int extraOrganicHarshness = 6;

    //how big are the continents
    public Dictionary<int, int> ContinentSizes = new Dictionary<int, int>();

    public void Execute(IMap map)
    {
        foreach (var t in map.Tiles) t.ContinentId = -1;

        var totalTiles = map.Tiles.Count;
        var targetTotalLand = Mathf.FloorToInt(totalTiles * totalLandPercentage);

        //random chance for extra landmasses
        var randomChance = UnityEngine.Random.Range(0, 100);
        var extraCount = 0;
        if (randomChance > 40) extraCount = 2;      
        else if (randomChance > 10) extraCount = 1;

        //main continent
        var combinedMainSize = Mathf.FloorToInt(targetTotalLand * 0.80f); //80% of land for main continent, rest for extra
        var sizeVariation = UnityEngine.Random.Range(0.40f, 0.60f);
        var size1 = Mathf.FloorToInt(combinedMainSize * sizeVariation);
        var size2 = combinedMainSize - size1;

        //var mainContinentSize = Mathf.FloorToInt(targetTotalLand * 0.40f);
        //var remainingLand = targetTotalLand - (mainContinentSize * 2);
        var remainingLand = targetTotalLand - combinedMainSize;
        var extraContinentSize = extraCount > 0 ? (remainingLand / extraCount) : 0;

        ContinentSizes.Clear();
        //List<ITile> frontier = new List<ITile>();

        //seeds startpoints 
        var seed1 = map.Tiles[UnityEngine.Random.Range(0, totalTiles)];
        var seed2 = GetClosestTile(map, -seed1.PositionOnSphere);

        //seed1.Type = Tile.TileType.Plain;
        //seed2.Type = Tile.TileType.Plain;
        var count1 = GrowContinent(seed1, 1, size1, mainNoiseScale, mainOrganicHarshness, mainDistancePenalty);
        ContinentSizes[1] = count1;

        var count2 = GrowContinent(seed2, 2, size2, mainNoiseScale, mainOrganicHarshness, mainDistancePenalty);
        ContinentSizes[2] = count2;

        //extra landmasses
        for (var i = 0; i < extraCount; i++)
        {
            var continentId = 3 + i;
            var extraSeed = GetTileFurthestFromLand(map);
            var countExtra = GrowContinent(extraSeed, continentId, extraContinentSize, extraNoiseScale, extraOrganicHarshness, extraDistancePenalty);
            ContinentSizes[continentId] = countExtra;
        }
        //small water tiles remove
        var visitedWater = new HashSet<ITile>();
        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water && !visitedWater.Contains(tile))
            {
                var waterBody = new List<ITile>();
                var queue = new Queue<ITile>();
                var neighborContinents = new Dictionary<int, int>(); 

                queue.Enqueue(tile);
                visitedWater.Add(tile);
                waterBody.Add(tile);

                //flood fill to measure waterbody size
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var neighbor in current.Neighbors)
                    {
                        if (neighbor.Type == Tile.TileType.Water && !visitedWater.Contains(neighbor))
                        {
                            visitedWater.Add(neighbor);
                            waterBody.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                        else if (neighbor.Type != Tile.TileType.Water && neighbor.ContinentId != -1)
                        {
                            if (!neighborContinents.ContainsKey(neighbor.ContinentId))
                                neighborContinents[neighbor.ContinentId] = 0;
                            neighborContinents[neighbor.ContinentId]++;
                        }
                    }
                }

                //no ocean 
                if (waterBody.Count < 40)
                {
                    int majorityContinent = 1;
                    int maxCount = -1;
                    foreach (var kvp in neighborContinents)
                    {
                        if (kvp.Value > maxCount)
                        {
                            maxCount = kvp.Value;
                            majorityContinent = kvp.Key;
                        }
                    }

                    foreach (var wTile in waterBody)
                    {
                        wTile.Type = Tile.TileType.Plain;
                        wTile.ContinentId = majorityContinent;
                    }
                }
            }
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
    private int GrowContinent(ITile seed, int continentId, int targetSize, float noiseScale, int organicHarshness, float distancePenalty)
    {
        var frontier = new List<ITile>();
        var currentLandTiles = 0;

        var seedPos = seed.PositionOnSphere;

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
        var isMainContinent = (continentId == 1 || continentId == 2);
        var otherMainContinentId = (continentId == 1) ? 2 : 1;

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
                var rawNoise = noise.snoise((pos * noiseScale) + noiseOffset);

                var distToSeed = Vector3.Distance(testTile.PositionOnSphere, seedPos);

                //distance penalty
                var n = rawNoise - (distToSeed * distancePenalty);

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
                    //var touchesOtherContinent = false;
                    //foreach (var nextDoorNeighbor in neighbor.Neighbors)
                    //{
                    //    if (nextDoorNeighbor.Type != Tile.TileType.Water &&
                    //        nextDoorNeighbor.ContinentId != -1 &&
                    //        nextDoorNeighbor.ContinentId != continentId)
                    //    {
                    //        touchesOtherContinent = true;
                    //        break; 
                    //    }
                    //}

                    //if (touchesOtherContinent) continue;

                    //if (isMainContinent)
                    //{
                    //    //3 tiles distance to the other main continent
                    //    if (IsWithinDistanceOfContinent(neighbor, otherMainContinentId, 3))
                    //    {
                    //        continue; 
                    //    }
                    //}
                    if (IsWithinDistanceOfContinent(neighbor, continentId, minContinentDistance))
                    {
                        continue; 
                    }

                    waterNeighbors.Add(neighbor);
                }
            }

            if (waterNeighbors.Count == 0)
            {
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

    private ITile GetTileFurthestFromLand(IMap map)
    {
        ITile bestTile = null;
        var maxDistToLand = -1f;
        var landTiles = new List<ITile>();

        foreach (var t in map.Tiles)
        {
            if (t.Type != Tile.TileType.Water) landTiles.Add(t);
        }

        if (landTiles.Count == 0) return map.Tiles[UnityEngine.Random.Range(0, map.Tiles.Count)];

        //100 random tiles 
        for (var i = 0; i < 100; i++)
        {
            var testTile = map.Tiles[UnityEngine.Random.Range(0, map.Tiles.Count)];
            if (testTile.Type != Tile.TileType.Water) continue;

            var closestLandDist = float.MaxValue;

            foreach (var land in landTiles)
            {
                var dist = Vector3.Distance(testTile.PositionOnSphere, land.PositionOnSphere);
                if (dist < closestLandDist) closestLandDist = dist;
            }

            //farthest away from any land tile
            if (closestLandDist > maxDistToLand)
            {
                maxDistToLand = closestLandDist;
                bestTile = testTile;
            }
        }

        return bestTile ?? map.Tiles[UnityEngine.Random.Range(0, map.Tiles.Count)];
    }

    //BFS
    private bool IsWithinDistanceOfContinent(ITile startTile, int targetContinentId, int maxDistance)
    {
        var visited = new HashSet<ITile>();
        var queue = new Queue<(ITile tile, int distance)>();

        queue.Enqueue((startTile, 0));
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            //if (current.tile.ContinentId == targetContinentId)
            //{
            //    return true;
            //}
            if (current.tile.Type != Tile.TileType.Water &&
                current.tile.ContinentId != -1 &&
                current.tile.ContinentId != targetContinentId)
            {
                return true; 
            }

            if (current.distance < maxDistance)
            {
                foreach (var neighbor in current.tile.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, current.distance + 1));
                    }
                }
            }
        }

        return false;
    }
}