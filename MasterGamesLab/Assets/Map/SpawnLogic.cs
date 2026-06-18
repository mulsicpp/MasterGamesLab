using Map.Fleet;
using Map.Infrastructure;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class SpawnLogic
    {
        private Map map;
        private ProducerConsumerSpawnPoint spawnPointGenerator;

        private Good bestAvailableGood;

        public SpawnLogic(Map map)
        {
            this.map = map;
            spawnPointGenerator = new ProducerConsumerSpawnPoint(map);

            bestAvailableGood = GoodUtils.Goods[0];
        }

        public void GenerateInitalState()
        {
            var playerSpawnTiles = SpawnPointGenerator.GetFairSpawnPoints(map, map.Players.Count);

            for (int i = 0; i < map.Players.Count; i++)
            {
                map.Infrastructure.SpawnLocal(new Garage.GarageState { Common = { TileId = playerSpawnTiles[i].Id } });
                map.Fleet.SpawnLocal(
                    new Truck.TruckState
                    {
                        Common = { Exists = true, ParkedTileId = playerSpawnTiles[i].Id },
                        FreighterIndex = VehicleIndex.NONE,
                        Good = Good.None
                    }, map.Players[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                var prodTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Producer, 1, Good.Common);
                if (prodTile != null)
                {
                    map.Infrastructure.SpawnLocal(new Producer.ProducerState
                    {
                        Common = { TileId = prodTile.Id },
                        Good = Good.Common
                    });
                    spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Producer, prodTile);
                }
            }

            for (int i = 0; i < 7; i++)
            {
                var consTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Consumer, 1, Good.None);
                if (consTile != null)
                {
                    map.Infrastructure.SpawnLocal(new Consumer.ConsumerState
                    {
                        Common = { TileId = consTile.Id }
                    });
                    spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Consumer, consTile);
                }
            }
        }

        public void Tick(float tickDuration)
        {
            // TODO implement
        }

        public Consumer.ConsumerRequest GenerateConsumerRequest()
        {
            var bestGoodIndex = Array.FindIndex(GoodUtils.Goods, 0, GoodUtils.Goods.Length, g => g == bestAvailableGood);
            var good = GoodUtils.Goods[UnityEngine.Random.Range(0, bestGoodIndex + 1)];

            return new(good, GoodUtils.GoodBasePayout[good]);
        }
    }
}