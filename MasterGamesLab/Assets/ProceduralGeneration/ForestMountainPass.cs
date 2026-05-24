using Map;
using Unity.Mathematics;
using UnityEngine;

public class ForestMountainPass : IGenerationPass
{
    //smaller value = bigger patches, bigger value = smaller patches
    public float noiseScale = 3.5f;

    //everything above 0.4 becomes mountain, everything below -0.25 becomes forest, everything in between is plain
    public float mountainThreshold = 0.4f; 
    public float forestThreshold = -0.25f; 

    public void Execute(IMap map)
    {
        var noiseOffset = new float3(
            UnityEngine.Random.Range(-100f, 100f),
            UnityEngine.Random.Range(-100f, 100f),
            UnityEngine.Random.Range(-100f, 100f)
        );

        var forestCount = 0;
        var mountainCount = 0;

        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water) continue;

            var pos = new float3(tile.PositionOnSphere.x, tile.PositionOnSphere.y, tile.PositionOnSphere.z);

            var n = noise.snoise((pos * noiseScale) + noiseOffset);

            if (n > mountainThreshold)
            {
                tile.Type = Tile.TileType.Mountain;
                mountainCount++;
            }
            else if (n < forestThreshold)
            {
                tile.Type = Tile.TileType.Forest;
                forestCount++;
            }
            else
            {
                tile.Type = Tile.TileType.Plain;
            }
        }

        Debug.Log($"forest: {forestCount}, mountain: {mountainCount}");
    }
}