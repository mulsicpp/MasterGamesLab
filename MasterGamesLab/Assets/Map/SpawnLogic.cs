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

        private SortedSet<Good> availableGoods;

        private float progress;
        public float Progress => Mathf.Clamp01(progress);


        private Dictionary<int, SortedSet<Good>> goodsPerContinent;

        private List<Consumer> readyConsumers;
        // private List<Consumer> busyConsumers;

        private Distribution<int> consumerContinentDistribution;

        private float consumerRequestCooldown;

        private List<(float, Action)> progressEvents;

        public SpawnLogic(Map map)
        {
            this.map = map;
            spawnPointGenerator = new ProducerConsumerSpawnPoint(map);

            availableGoods = new();

            progress = 0;

            var spawnChances = GetGoodSpawnChancePerContinent();

            goodsPerContinent = new();

            for (int i = 0; i < map.ContinentInfos.Count; i++)
                goodsPerContinent.Add(1 + i, new());

            goodsPerContinent[1].Add(Good.Common);
            goodsPerContinent[1].Add(Good.Uncommon);
            goodsPerContinent[1].Add(Good.Rare);

            //Dictionary<int, Distribution<Good>> continentDistributions = new();

            List<(Good, float)> continentProbs = new();
            for (int i = 0; i < spawnChances.GetLength(0); i++)
            {
                continentProbs.Clear();
                for (int j = 0; j < spawnChances.GetLength(1); j++)
                {
                    continentProbs.Add((GoodUtils.Goods[j], spawnChances[i, j]));
                }

                goodsPerContinent[1 + i].Add(new Distribution<Good>(continentProbs).GetRandom());
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
                goodsPerContinent[new Distribution<int>(goodProbs).GetRandom()].Add(GoodUtils.Goods[i]);
                //goodDistributions.Add(GoodUtils.Goods[i], new(goodProbs));
            }

            foreach (var (cId, goods) in goodsPerContinent)
            {
                string goodString = goods.AsEnumerable().Select(g => g.ToString()).Aggregate((s1, s2) => s1 + ", " + s2);
                Debug.Log("Continent " + cId + ": { " + goodString + " }");
            }

            readyConsumers = new();
            // busyConsumers = new();

            consumerRequestCooldown = NextConsumerRequestCooldown();

            progressEvents = new()
            {
                (0.1f, () => Debug.Log("Progress reached 10%")),
                (0.2f, () => Debug.Log("Progress reached 20%")),
            };

            List<(int, float)> continentProbabilities = map.ContinentInfos.Select(info => (info.Key, (float)info.Value.Size)).ToList();
            consumerContinentDistribution = new(continentProbabilities);

            for (int i = 1; i < GoodUtils.Goods.Length; i++)
            {
                var iCopy = i;
                progressEvents.Add(((float)i / GoodUtils.Goods.Length, () => EnableGood(GoodUtils.Goods[iCopy])));
            }

            int dynamicConsumerCount = Constants.TotalConsumerCount - Constants.StartConsumerCount;

            for (int i = 0; i < dynamicConsumerCount; i++)
            {
                var iCopy = i;
                progressEvents.Add(((float)(i + 1) / (dynamicConsumerCount + 1), () => SpawnConsumer((float)(iCopy + Constants.StartConsumerCount) / Constants.TotalConsumerCount)));
            }

        }

        public void GenerateInitalState()
        {
            var playerSpawnTiles = SpawnPointGenerator.GetFairSpawnPoints(map, map.Players.Count);

            for (int i = 0; i < map.Players.Count; i++)
            {
                map.Infrastructure.SpawnLocal(new CarPark.CarParkState { Common = { TileId = playerSpawnTiles[i].Id } });
                map.Fleet.SpawnLocal(
                    new Truck.TruckState
                    {
                        Common = { Exists = true, ParkedTileId = playerSpawnTiles[i].Id },
                        FreighterIndex = VehicleIndex.NONE,
                        Good = Good.None
                    }, map.Players[i]);
            }

            foreach (var good in goodsPerContinent[1])
                SpawnProducer(1, good);

            for (int i = 0; i < Constants.StartConsumerCount; i++)
                SpawnConsumer((float)i / Constants.TotalConsumerCount);

            foreach (var consumer in readyConsumers)
            {
                GenerateConsumerRequest(consumer);
            }
        }

        public void Tick(float tickDuration)
        {
            progress += tickDuration / 600.0f;
            List<(float, Action)> newProgressEvents = new();
            foreach (var (p, e) in progressEvents)
            {
                if (p <= Progress)
                {
                    e.Invoke();
                }
                else
                {
                    newProgressEvents.Add((p, e));
                }
            }

            progressEvents = newProgressEvents;


            if ((consumerRequestCooldown -= tickDuration) <= 0)
            {
                // Debug.Log("Generating consumer request! Available consumers: " + readyConsumers.Where(c => c.Request.Good == Good.None).Count() + "/" + readyConsumers.Count + "  Available goods: " + availableGoods.Count);
                consumerRequestCooldown = NextConsumerRequestCooldown();
                if (readyConsumers.Count > 0)
                {
                    GenerateConsumerRequest(FindRandomConsumer());
                }
            }
        }

        public void FastForward(float delta)
        {
            progress += delta;
        }

        public Consumer FindRandomConsumer()
        {
            return readyConsumers.Count > 0 ? readyConsumers[UnityEngine.Random.Range(0, readyConsumers.Count)] : null;
        }

        public void ClearConsumerRequest(Consumer consumer)
        {
            if (consumer == null) return;
            consumer.Request = new(Good.None, 0, 0);
        }

        public void GenerateConsumerRequest(Consumer consumer)
        {
            if (consumer == null || availableGoods.Count == 0 || consumer.Request.Good != Good.None) return;

            var good = availableGoods.ToArray()[UnityEngine.Random.Range(0, availableGoods.Count)];
            var payout = CalculatePayout(consumer, good);
            consumer.Request = new(good, payout, payout);
            consumer.SetupPayoutIncrease();
        }

        private int CalculatePayout(Consumer consumer, Good good)
        {
            var basecost = GoodUtils.GoodBasePayout[good];

            var movementProfile = goodsPerContinent[consumer.Tile.ContinentId].Contains(good) ? MovementProfileRegistry.ConsumerPayoutOwnContinent : MovementProfileRegistry.ConsumerPayoutForeignContinent;
            var path = Pathfinding.FindPath(consumer.Tile, tile => tile.Structure is Producer p && p.Good == good, movementProfile);

            int distancecost = 0;
            foreach (var tile in path)
            {
                distancecost += Map.Instance.Tiles[tile].Type switch
                {
                    Tile.TileType.Water => Constants.WATER_SHIPPING_COST,
                    _ => Constants.NORMAL_SHIPPING_COST,
                };
            }

            int randomCost = UnityEngine.Random.Range(Constants.MIN_RANDOM_COST, Constants.MAX_RANDOM_COST + 1);
            return basecost + distancecost + randomCost - 10;
        }

        private float NextConsumerRequestCooldown()
        {
            return UnityEngine.Random.Range(Constants.MIN_CONSUMER_REQUEST_COOLDOWN, Constants.MAX_CONSUMER_REQUEST_COOLDOWN) / map.Players.Count;
        }

        private void EnableGood(Good good)
        {
            foreach (var (continentId, goods) in goodsPerContinent)
            {
                if (continentId > 1 && goods.Contains(good))
                {
                    SpawnProducer(continentId, good);
                }
            }
        }

        private void SpawnProducer(int continentId, Good good)
        {
            var prodTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Producer, continentId, good);
            if (prodTile != null)
            {
                map.Infrastructure.SpawnLocal(new Producer.ProducerState
                {
                    Common = { TileId = prodTile.Id },
                    Good = good
                });
                spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Producer, prodTile);
                availableGoods.Add(good);
            }
        }

        private void SpawnConsumer(float value)
        {
            int cont = consumerContinentDistribution.GetFromValue(value);
            var consTile = spawnPointGenerator.GetSpawnableTile(Structure.StructureType.Consumer, cont);
            if (consTile != null)
            {
                var consumer = map.Infrastructure.SpawnLocal(new Consumer.ConsumerState
                {
                    Common = { TileId = consTile.Id }
                });
                spawnPointGenerator.RegisterSpawnedTile(Structure.StructureType.Consumer, consTile);
                readyConsumers.Add(consumer as Consumer);
            }
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