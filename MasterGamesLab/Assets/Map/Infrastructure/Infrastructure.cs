using System;
using System.Collections.Generic;
using Unity.Services.Qos.V2.Models;

namespace Map.Infrastructure
{
    public class Infrastructure : IReadOnlyInfrastructure
    {
        public IReadOnlyList<Producer> Producers => producers;
        public IReadOnlyList<Consumer> Consumers => consumers;
        // public IReadOnlyList<Garage> Garages => garages;
        // public IReadOnlyList<Port> Ports => ports;
        // public IReadOnlyList<TrainStation> TrainStations => trainStations;

        private Producer[] producers;
        private Consumer[] consumers;
        // Garage[] garages;
        // Port[] ports;
        // TrainStation[] trainStations;

        public Infrastructure()
        {
            producers = new Producer[Constants.MAX_PRODUCER_COUNT];
            for (var i = 0; i < producers.Length; i++) producers[i] = new Producer(new StructureIndex((byte)i));

            consumers = new Consumer[Constants.MAX_CONSUMER_COUNT];
            for (var i = 0; i < consumers.Length; i++) consumers[i] = new Consumer(new StructureIndex((byte)i));
        }

        public int GetFirstAvailableStructureOffset(Structure.StructureType type)
        {
            Structure[] structures = null;

            switch (type)
            {
                case Structure.StructureType.Producer: structures = producers; break;
                case Structure.StructureType.Consumer: structures = consumers; break;
                // case Structure.StructureType.Garage: structures = Garages; break;
                // case Structure.StructureType.Port: structures = Ports; break;
                // case Structure.StructureType.TrainStation: structures = TrainStations; break;
            }

            if (structures == null) return -1;

            for (int i = 0; i < structures.Length; i++)
            {
                if (!structures[i].Exists)
                    return i;
            }
            return -1;
        }

        public void SetNetObject<T>(T netData) where T : Structure.INetData
        {
            if (netData is Producer.NetData p) SetProducer(p.Index, p.TileId, p.Good);
            else if (netData is Consumer.NetData c) SetConsumer(c.Index, c.TileId, c.RequestedGood);
            else throw new ArgumentException("Given Structure.INetData is not supported: " + netData.GetType().FullName);
        }



        public void SetProducer(StructureIndex index, TileId tileId, Good good)
        {
            if (index >= producers.Length) return;

            var producer = producers[index];

            producer.Tile = tileId != TileId.NONE && Map.Instance.Tiles[tileId] is Tile t ? t : null;
            producer.Good = good;
        }

        public void SetConsumer(StructureIndex index, TileId tileId, Good requestedGood)
        {
            if (index >= consumers.Length) return;

            var consumer = consumers[index];

            consumer.Tile = tileId != TileId.NONE && Map.Instance.Tiles[tileId] is Tile t ? t : null;
            consumer.RequestedGood = requestedGood;
        }
    }
}