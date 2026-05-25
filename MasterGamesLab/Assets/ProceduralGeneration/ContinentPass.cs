using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics; 
using Map;

public class ContinentPass : IGenerationPass
{
    //continet settings
    public float landPercentage = 0.35f;

    //how jagged
    public float noiseScale = 2.5f;

    //1 is a circle
    public int organicHarshness = 6;

    public void Execute(IMap map)
    {
        var totalTiles = map.Tiles.Count;
        var targetLandTiles = Mathf.FloorToInt(totalTiles * landPercentage);
        var currentLandTiles = 0;

        List<ITile> frontier = new List<ITile>();

        //seeds startpoints 
        var seed1 = map.Tiles[0];
        var seed2 = map.Tiles[totalTiles / 2];

        seed1.Type = Tile.TileType.Plain;
        seed2.Type = Tile.TileType.Plain;

        frontier.Add(seed1);
        frontier.Add(seed2);
        currentLandTiles += 2;

        //random offset for noise
        float3 noiseOffset = new float3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));

        while (frontier.Count > 0 && currentLandTiles < targetLandTiles)
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
                currentLandTiles++;

                frontier.Add(nextLand);
            }
        }

        Debug.Log($"Land-Tiles: {currentLandTiles}");
    }
}