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

    public void Execute(MapData data)
    {
        int totalTiles = data.Map.Tiles.Count;
        int targetLandTiles = Mathf.FloorToInt(totalTiles * landPercentage);
        int currentLandTiles = 0;

        List<ITile> frontier = new List<ITile>();

        //seeds startpoints 
        ITile seed1 = data.Map.Tiles[0];
        ITile seed2 = data.Map.Tiles[totalTiles / 2];

        seed1.Type = Tile.TileType.Plain;
        seed2.Type = Tile.TileType.Plain;

        frontier.Add(seed1);
        frontier.Add(seed2);
        currentLandTiles += 2;

        //random offset for noise
        float3 noiseOffset = new float3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));

        while (frontier.Count > 0 && currentLandTiles < targetLandTiles)
        {
            int bestIndex = 0;
            float bestNoise = -999f;

            //organicHarshness is the number of random samples taken from the frontier for expansion
            int samples = Mathf.Min(frontier.Count, organicHarshness);
            for (int i = 0; i < samples; i++)
            {
                int randIdx = UnityEngine.Random.Range(0, frontier.Count);
                ITile testTile = frontier[randIdx];

                float3 pos = new float3(testTile.PositionOnSphere.x, testTile.PositionOnSphere.y, testTile.PositionOnSphere.z);

                //3D Noise 
                float n = noise.snoise((pos * noiseScale) + noiseOffset);

                if (n > bestNoise)
                {
                    bestNoise = n;
                    bestIndex = randIdx;
                }
            }

            ITile current = frontier[bestIndex];

            //find water neighbors
            List<ITile> waterNeighbors = new List<ITile>();
            foreach (ITile neighbor in current.Neighbors)
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
                ITile nextLand = waterNeighbors[UnityEngine.Random.Range(0, waterNeighbors.Count)];
                nextLand.Type = Tile.TileType.Plain;
                currentLandTiles++;

                frontier.Add(nextLand);
            }
        }

        Debug.Log($"Land-Tiles: {currentLandTiles}");
    }
}