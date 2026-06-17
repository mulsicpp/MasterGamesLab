using Map;
using Map.Infrastructure;
using System.Collections.Generic;
using UnityEngine;

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

    //public float ConsumerGroupProbability = 0.6f;

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

    public ITile GetSpawnableTile(Structure.StructureType type, int continentId, Good resource)
    {
        var candidates = new List<ITile>();

        var sourceList = (type == Structure.StructureType.Producer) ? ValidProducerTiles : ValidConsumerTiles;

        foreach (var tile in sourceList)
        {
            if (tile.ContinentId == continentId)
            {
                candidates.Add(tile);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"no spawnpoint for {type} on continent {continentId}");
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public void RegisterSpawnedTile(Structure.StructureType type, ITile tile)
    {
        if (type == Structure.StructureType.Producer)
        {
            PlacedProducers.Add(tile);
            RemoveTilesWithinRadius(ValidProducerTiles, tile, MinDistProducerProducer);
            RemoveTilesWithinRadius(ValidConsumerTiles, tile, MinDistProducerConsumer);
        }
        else if (type == Structure.StructureType.Consumer)
        {
            PlacedConsumers.Add(tile);
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