using Map.Fleet;
using Map.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using Utils;

namespace Map
{
    public class SpawnLogic
    {

        private Map map;
        private ProducerConsumerSpawnPoint spawnPointGenerator;

        private List<Good> availableGoods;
        private List<int> availableContinents;

        public float Progress => (Time.time - startTime) / 300f;

        private readonly float startTime;

        private Dictionary<int, SortedSet<Good>> goodsPerContinent;

        private Dictionary<Good, List<Producer>> producersPerGood;
        private Dictionary<int, List<Consumer>> consumerPerContinent;

        private List<Consumer> readyConsumers;
        private List<Consumer> busyConsumers;

        private float consumerRequestCooldown;

        private List<(float, Action)> progressEvents;

        public SpawnLogic(Map map)
        {
            this.map = map;
            spawnPointGenerator = new ProducerConsumerSpawnPoint(map);

            availableGoods = new();
            availableContinents = new();

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
                string goodString = goods.AsEnumerable().Select(g => g.ToString()).Aggregate((s1, s2) => s1 + ", " + s2);
                Debug.Log("Continent " + cId + ": { " + goodString + " }");
            }

            producersPerGood = new();

            foreach (var good in GoodUtils.Goods)
            {
                producersPerGood.Add(good, new());
            }

            consumerPerContinent = new();

            foreach (var continentId in map.ContinentInfos.Keys)
            {
                consumerPerContinent.Add(continentId, new());
            }

            readyConsumers = new();
            busyConsumers = new();

            consumerRequestCooldown = NextConsumerRequestCooldown();

            progressEvents = new()
            {
                (0.1f, () => Debug.Log("Progress reached 10%")),
                (0.2f, () => Debug.Log("Progress reached 20%")),
            };

            for (int i = 0; i < GoodUtils.Goods.Length; i++)
            {
                var iCopy = i;
                progressEvents.Add(((float)i / GoodUtils.Goods.Length, () => EnableGood(GoodUtils.Goods[iCopy])));
            }

            for (int i = 0; i < map.ContinentInfos.Count; i++)
            {
                var iCopy = i;
                progressEvents.Add(((float)i / map.ContinentInfos.Count, () => EnableContinent(iCopy + 1)));
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

            foreach (var (continentId, goods) in goodsPerContinent)
            {
                foreach (var good in goods)
                {
                    var prodTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Producer, continentId, good);
                    if (prodTile != null)
                    {
                        var producer = map.Infrastructure.SpawnLocal(new Producer.ProducerState
                        {
                            Common = { TileId = prodTile.Id },
                            Good = Good.None
                        });
                        spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Producer, prodTile);
                        producersPerGood[good].Add(producer as Producer);
                    }
                }
            }

            for (int i = 0; i < 10 + map.Players.Count * 6; i++)
            {
                var consTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Consumer);
                if (consTile != null)
                {
                    var consumer = map.Infrastructure.SpawnLocal(new Consumer.ConsumerState
                    {
                        Common = { TileId = consTile.Id }
                    });
                    spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Consumer, consTile);
                    consumerPerContinent[consTile.ContinentId].Add(consumer as Consumer);
                }
            }
        }

        public void Tick(float tickDuration)
        {
            List<(float, Action)> newProgressEvents = new();
            foreach (var (p, e) in progressEvents)
            {
                if(p <= Progress)
                {
                    e.Invoke();
                } else
                {
                    newProgressEvents.Add((p, e));
                }
            }

            progressEvents = newProgressEvents;


            if((consumerRequestCooldown -= tickDuration) <= 0)
            {
                Debug.Log("Generating consimer request! Available consumers: " + readyConsumers.Count + "  Available goods: " + availableGoods.Count);
                consumerRequestCooldown = NextConsumerRequestCooldown();
                if (readyConsumers.Count > 0)
                {
                    var consumer = readyConsumers[UnityEngine.Random.Range(0, readyConsumers.Count)];
                    GenerateConsumerRequest(consumer);
                }
            }
        }

        public void ClearConsumerRequest(Consumer consumer)
        {
            consumer.Request = new(Good.None, 0);
            if (busyConsumers.Remove(consumer))
                readyConsumers.Add(consumer);
        }

        public void GenerateConsumerRequest(Consumer consumer)
        {
            if (availableGoods.Count == 0) return;

            var good = availableGoods[UnityEngine.Random.Range(0, availableGoods.Count)];

            if(readyConsumers.Remove(consumer))
            {
                consumer.Request = new(good, GoodUtils.GoodBasePayout[good]);
                consumer.SetupPayoutIncrease();
                busyConsumers.Add(consumer);
            }
        }

        private float NextConsumerRequestCooldown()
        {
            return UnityEngine.Random.Range(Constants.MIN_CONSUMER_REQUEST_COOLDOWN, Constants.MAX_CONSUMER_REQUEST_COOLDOWN);
        }

        private void EnableGood(Good good)
        {
            availableGoods.Add(good);
            foreach (var producer in producersPerGood[good])
            {
                producer.Good = good;
            }
        }

        private void EnableContinent(int continentId)
        {
            availableContinents.Add(continentId);
            readyConsumers.AddRange(consumerPerContinent[continentId]);
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