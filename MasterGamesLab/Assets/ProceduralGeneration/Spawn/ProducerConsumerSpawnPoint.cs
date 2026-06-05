using System.Collections.Generic;
using UnityEngine;
using Map;

public class ProducerConsumerSpawnPoint
{
    private IMap map;

    public List<ITile> ValidProducerTiles { get; private set; }
    public List<ITile> ValidConsumerTiles { get; private set; }

    //gizmo
    public List<ITile> PlacedProducers { get; private set; }
    public List<ITile> PlacedConsumers { get; private set; }

    //distance
    public int MinDistProducerConsumer = 10;
    public int MinDistProducerProducer = 8;
    public int MinDistConsumerConsumer = 8;

    public float ConsumerGroupProbability = 0.6f;

    public ProducerConsumerSpawnPoint(IMap map)
    {
        this.map = map;
        PlacedProducers = new List<ITile>();
        PlacedConsumers = new List<ITile>();

        CalculateInlandTiles();
    }

    private void CalculateInlandTiles()
    {
        var validInlandTiles = new List<ITile>();

        foreach (var tile in map.Tiles)
        {
            if (tile.Type == Tile.TileType.Water || tile.Type == Tile.TileType.Mountain)
                continue;

            bool isTooCloseToWater = false;

            foreach (var n1 in tile.Neighbors)
            {
                if (n1.Type == Tile.TileType.Water) isTooCloseToWater = true;

                foreach (var n2 in n1.Neighbors)
                {
                    if (n2.Type == Tile.TileType.Water) isTooCloseToWater = true;
                }
            }

            if (!isTooCloseToWater)
            {
                validInlandTiles.Add(tile);
            }
        }

        ValidProducerTiles = new List<ITile>(validInlandTiles);
        ValidConsumerTiles = new List<ITile>(validInlandTiles);
    }

    public ITile GetSpawnTileProducer()
    {
        if (ValidProducerTiles.Count == 0) return null;
        return ValidProducerTiles[UnityEngine.Random.Range(0, ValidProducerTiles.Count)];
    }

    public List<ITile> GetSpawnTileConsumer()
    {
        if (ValidConsumerTiles.Count == 0) return new List<ITile>();

        var result = new List<ITile>();
        var centerTile = ValidConsumerTiles[UnityEngine.Random.Range(0, ValidConsumerTiles.Count)];
        result.Add(centerTile);

        if (UnityEngine.Random.value < ConsumerGroupProbability)
        {
            int groupSize = UnityEngine.Random.Range(2, 6);

            foreach (var neighbor in centerTile.Neighbors)
            {
                if (result.Count >= groupSize) break;

                if (ValidConsumerTiles.Contains(neighbor))
                {
                    result.Add(neighbor);
                }
            }
        }

        return result;
    }

    public void RegisterProducerSpawned(ITile tile)
    {
        //gizmo
        PlacedProducers.Add(tile);

        RemoveTilesWithinRadius(ValidProducerTiles, tile, MinDistProducerProducer);
        RemoveTilesWithinRadius(ValidConsumerTiles, tile, MinDistProducerConsumer);
    }

    public void RegisterConsumerSpawned(List<ITile> tiles)
    {
        PlacedConsumers.AddRange(tiles);

        foreach (var tile in tiles)
        {
            RemoveTilesWithinRadius(ValidConsumerTiles, tile, MinDistConsumerConsumer);
            RemoveTilesWithinRadius(ValidProducerTiles, tile, MinDistProducerConsumer);
        }
    }

    private void RemoveTilesWithinRadius(List<ITile> listToUpdate, ITile center, int radius)
    {
        var tilesToRemove = new HashSet<ITile>();
        var queue = new Queue<(ITile tile, int dist)>();
        var visited = new HashSet<ITile>();

        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            tilesToRemove.Add(current.tile);

            if (current.dist < radius)
            {
                foreach (var neighbor in current.tile.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, current.dist + 1));
                    }
                }
            }
        }

        listToUpdate.RemoveAll(t => tilesToRemove.Contains(t));
    }
}