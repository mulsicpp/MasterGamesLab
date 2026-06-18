using Map.Fleet;
using Map.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Map
{
    public class SpawnLogic
    {

        private Map map;
        private ProducerConsumerSpawnPoint spawnPointGenerator;

        private Good bestAvailableGood;

        public float Progress => (Time.time - startTime) / 300f;

        private readonly float startTime;

        private Dictionary<int, SortedSet<Good>> goodsPerContinent;

        public SpawnLogic(Map map)
        {
            this.map = map;
            spawnPointGenerator = new ProducerConsumerSpawnPoint(map);

            bestAvailableGood = GoodUtils.Goods[0];
            startTime = Time.time;

            var spawnChances = GetGoodSpawnChancePerContinent();

            goodsPerContinent = new();

            for (int i = 0; i < map.ContinentInfos.Count; i++)
                goodsPerContinent.Add(1 + i, new());

            goodsPerContinent[1].Add(Good.Common);

            //Dictionary<int, Distribution<Good>> continentDistributions = new();

            List<(Good, float)> continentProbs = new();
            for (int i = 0; i < spawnChances.GetLength(0); i++)
            {
                continentProbs.Clear();
                for (int j = 0; j < spawnChances.GetLength(1); j++)
                {
                    continentProbs.Add((GoodUtils.Goods[j], spawnChances[i, j]));
                }

                goodsPerContinent[1 + i].Add(new Distribution<Good>(continentProbs).Get());
                //continentDistributions.Add(1 + i, new(continentProbs));
            }

            //Dictionary<Good, Distribution<int>> goodDistributions = new();

            List<(int, float)> goodProbs = new();
            for (int i = 1; i < spawnChances.GetLength(1); i++)
            {
                goodProbs.Clear();
                for (int j = 0; j < spawnChances.GetLength(0); j++)
                {
                    goodProbs.Add((1 + j, spawnChances[j, i]));
                }
                goodsPerContinent[new Distribution<int>(goodProbs).Get()].Add(GoodUtils.Goods[i]);
                //goodDistributions.Add(GoodUtils.Goods[i], new(goodProbs));
            }

            foreach (var (cId, goods) in goodsPerContinent)
            {
                string goodString = goods.AsEnumerable().Select(g => g.ToString() + " ").Aggregate((s1, s2) => s1 + s2);
                Debug.Log("Continent " + cId + ": { " + goodString + "}");
            }
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

        private float[,] GetGoodSpawnChancePerContinent()
        {
            var prev = Constants.GOOD_SPAWN_CHANCE_PER_CONTINENT;
            float[,] ret;
            switch (map.ContinentInfos.Count)
            {
                case 4: return prev;
                case 3:
                    ret = new float[3, 5];
                    for (int i = 0; i < 5; i++)
                    {
                        ret[0, i] = prev[0, i];
                        ret[1, i] = prev[1, i];
                        ret[2, i] = prev[2, i] * 0.5f + prev[3, i] * 0.5f;
                    }
                    return ret;
                default:
                    ret = new float[2, 5];
                    for (int i = 0; i < 5; i++)
                    {
                        ret[0, i] = prev[0, i];
                        ret[1, i] = (prev[1, i] + prev[2, i] + prev[3, i]) / 3.0f;
                    }
                    return ret;
            }
        }
    }
}