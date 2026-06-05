using Map;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ForestMountainPass : IGenerationPass
{
    //smaller value = bigger patches, bigger value = smaller patches
    //public float noiseScale = 3.5f;

    //everything above 0.4 becomes mountain, everything below -0.25 becomes forest, everything in between is plain
    //forest
    public float forestNoiseScale = 3.5f;
    public float forestThreshold = -0.25f;

    //mountain
    public float mountainNoiseScale = 1.2f;
    public float ridgeThickness = 0.18f; //0 is thin
    public int minMountainChainLength = 5;
    public int maxMountainChainLength = 12;
    public float maskThreshold = -0.4f;

    public void Execute(IMap map)
    {
        var forestOffset = new float3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));
        var mountainOffset = new float3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));
        var mountainMaskOffset = new float3(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));

        //var mountainCount = 0;
        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water) continue;

            var pos = new float3(tile.PositionOnSphere.x, tile.PositionOnSphere.y, tile.PositionOnSphere.z);

            //var n = noise.snoise((pos * noiseScale) + noiseOffset);
            //var forestNoise = noise.snoise((pos * forestNoiseScale) + forestOffset);
            //if (forestNoise < forestThreshold)
            //{
            //    tile.Type = Tile.TileType.Forest;
            //    continue; 
            //}

            //if (n > mountainThreshold)
            //{
            //    tile.Type = Tile.TileType.Mountain;
            //    mountainCount++;
            //}
            //else if (n < forestThreshold)
            //{
            //    tile.Type = Tile.TileType.Forest;
            //    forestCount++;
            //}
            //else
            //{
            //    tile.Type = Tile.TileType.Plain;
            //}
            var ridgeNoise = math.abs(noise.snoise((pos * mountainNoiseScale) + mountainOffset));
            var maskNoise = noise.snoise((pos * 1.5f) + mountainMaskOffset);

            if (ridgeNoise < ridgeThickness && maskNoise > maskThreshold)
            {
                tile.Type = Tile.TileType.Mountain;
            }
            else
            {
                tile.Type = Tile.TileType.Plain;
            }
        }
        //no cutting off 
        var coastalMountains = new List<ITile>();
        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Mountain)
            {
                foreach (var neighbor in tile.Neighbors)
                {
                    if (neighbor.Type == Tile.TileType.Water)
                    {
                        coastalMountains.Add(tile);
                        break;
                    }
                }
            }
        }
        
        foreach (var m in coastalMountains) m.Type = Tile.TileType.Plain;
        
        //max 2 thick
        bool tooThick = true;
        int safetyCounter = 0;

        while (tooThick && safetyCounter < 50) 
        {
            tooThick = false;
            var edgeTilesToRemove = new HashSet<ITile>();

            foreach (var tile in map.Tiles)
            {
                if (tile.Type == Tile.TileType.Mountain && !edgeTilesToRemove.Contains(tile))
                {
                    int mNeighbors = 0;
                    foreach (var n in tile.Neighbors)
                        if (n.Type == Tile.TileType.Mountain) mNeighbors++;

                    //thick if 4 or more mountain neighbors
                    if (mNeighbors >= 4)
                    {
                        tooThick = true;

                        ITile thinnestNeighbor = null;
                        int minN = 99;

                        //find tile with least mountain neighbors
                        foreach (var n in tile.Neighbors)
                        {
                            if (n.Type == Tile.TileType.Mountain && !edgeTilesToRemove.Contains(n))
                            {
                                int nnCount = 0;
                                foreach (var nn in n.Neighbors)
                                    if (nn.Type == Tile.TileType.Mountain) nnCount++;

                                if (nnCount < minN)
                                {
                                    minN = nnCount;
                                    thinnestNeighbor = n;
                                }
                            }
                        }

                        if (thinnestNeighbor != null)
                        {
                            edgeTilesToRemove.Add(thinnestNeighbor);
                        }
                    }
                }
            }

            //remove all marked tiles
            foreach (var t in edgeTilesToRemove)
            {
                t.Type = Tile.TileType.Plain;
            }
            safetyCounter++;
        }

        bool chainsChanged = true;
        int chainSafety = 0;
        var validMountainCount = 0;

        //till no range too long
        while (chainsChanged && chainSafety < 100)
        {
            chainsChanged = false;
            var visitedMountains = new HashSet<ITile>();
            validMountainCount = 0;

            foreach (var tile in map.Tiles)
            {
                if (tile.Type == Tile.TileType.Mountain && !visitedMountains.Contains(tile))
                {
                    var currentChain = new List<ITile>();
                    var queue = new Queue<ITile>();

                    queue.Enqueue(tile);
                    visitedMountains.Add(tile);
                    currentChain.Add(tile);

                    //flood fill
                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();

                        foreach (var neighbor in current.Neighbors)
                        {
                            if (neighbor.Type == Tile.TileType.Mountain && !visitedMountains.Contains(neighbor))
                            {
                                visitedMountains.Add(neighbor);
                                currentChain.Add(neighbor);
                                queue.Enqueue(neighbor);
                            }
                        }
                    }

                    if (currentChain.Count < minMountainChainLength)
                    {
                        foreach (var mTile in currentChain) mTile.Type = Tile.TileType.Plain;
                        chainsChanged = true;
                        break;
                    }

                    else if (currentChain.Count > maxMountainChainLength)
                    {
                        ITile breakPoint = null;

                        foreach (var cTile in currentChain)
                        {
                            int nCount = 0;
                            foreach (var n in cTile.Neighbors) if (n.Type == Tile.TileType.Mountain) nCount++;
                            if (nCount == 2)
                            {
                                breakPoint = cTile;
                                break;
                            }
                        }

                        if (breakPoint == null) breakPoint = currentChain[currentChain.Count / 2];

                        breakPoint.Type = Tile.TileType.Plain;
                        chainsChanged = true;
                        break;
                    }
                    else
                    {
                        validMountainCount += currentChain.Count;
                    }
                }
            }
            chainSafety++;
        }

        var forestCount = 0;
        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water || tile.Type == Tile.TileType.Mountain) continue;

            var pos = new float3(tile.PositionOnSphere.x, tile.PositionOnSphere.y, tile.PositionOnSphere.z);
            var forestNoise = noise.snoise((pos * forestNoiseScale) + forestOffset);

            if (forestNoise < forestThreshold)
            {
                tile.Type = Tile.TileType.Forest;
                forestCount++;
            }
        }
        Debug.Log($"mountain Tiles: {validMountainCount} ");
        Debug.Log($"forest Tiles: {forestCount} ");
    }
}