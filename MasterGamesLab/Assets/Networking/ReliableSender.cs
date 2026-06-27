
using Map;
using Map.Fleet;
using Map.Infrastructure;
using Player;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class ReliableSender
    {

        public readonly bool IncrementTimestamp;
        public readonly ClientId ClientId;

        private readonly ClientRpcParams rpcParams;

        public class Packet
        {
            private int nettoSize;
            public int NettoSize => nettoSize;

            private List<Player.Player.PlayerState> players;

            private List<Edge.EdgeState> edges;

            private List<Producer.ProducerState> producers;
            private List<Consumer.ConsumerState> consumers;
            private List<CarPark.CarParkState> carParks;
            private List<Port.PortState> ports;

            private List<Truck.TruckState> trucks;
            private List<Freighter.FreighterState> freighters;

            public Packet() {
                nettoSize = 0;
                players = new();
                edges = new();
                producers = new();
                consumers = new();
                carParks = new();
                ports = new();
                trucks = new();
                freighters = new();
            }

            public void InsertState(IState state)
            {
                switch (state)
                {
                    case Player.Player.PlayerState pl: players.Add(pl); break;
                    case Edge.EdgeState e: edges.Add(e); break;
                    case Producer.ProducerState p: producers.Add(p); break;
                    case Consumer.ConsumerState c: consumers.Add(c); break;
                    case CarPark.CarParkState g: carParks.Add(g); break;
                    case Port.PortState pt: ports.Add(pt); break;
                    case Truck.TruckState t: trucks.Add(t); break;
                    case Freighter.FreighterState f: freighters.Add(f); break;
                    default: return;
                }
                nettoSize += state.SerializedSize;
            }

            public void Send(ClientRpcParams rpcParams)
            {
                if (nettoSize == 0) return;
                Map.Map.Instance.ApplyReliableStatesClientRpc(
                    Map.Map.Instance.Timestamp,
                    Time.timeAsDouble,
                    players.ToArray(),
                    edges.ToArray(),
                    producers.ToArray(),
                    consumers.ToArray(),
                    carParks.ToArray(),
                    ports.ToArray(),
                    trucks.ToArray(),
                    freighters.ToArray(),
                    rpcParams
                );
            }
        }

        private List<Packet> packets;


        public ReliableSender(bool incrementTimestamp) : this(incrementTimestamp, ClientId.NONE) { }
        public ReliableSender(bool incrementTimestamp, ClientId clientId)
        {
            packets = new List<Packet>();

            IncrementTimestamp = incrementTimestamp;
            ClientId = clientId;

            if (ClientId != ClientId.NONE)
                rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { clientId },
                    }
                };
            else
                rpcParams = default;
        }

        public void Clear()
        {
            packets.Clear();
        }

        public void Add(IState state)
        {
            int size = state.SerializedSize;

            if (packets.Count == 0)
            {
                packets.Add(new Packet());
            }

            Packet lastPacket = packets[packets.Count - 1];

            if (lastPacket.NettoSize != 0 && lastPacket.NettoSize + size > Constants.MAX_NETTO_BYTES_PER_RPC)
            {
                packets.Add(new Packet());
                lastPacket = packets[packets.Count - 1];
            }

            lastPacket.InsertState(state);
        }

        public void AddStates<T>(IEnumerable<T> states, Predicate<T> condition = null) where T : struct, IState
        {
            if (condition == null)
                foreach (var state in states) Add(state);
            else
                foreach (var state in states)
                {
                    if (condition(state))
                        Add(state);
                }
        }

        public void AddObjects<T, U>(IEnumerable<T> objects, Predicate<T> condition = null) where U : struct, IState where T : ISynchableObject<U>
        {
            if (condition == null)
                foreach (var obj in objects) Add(obj.State);
            else
                foreach (var obj in objects)
                {
                    if (condition(obj))
                        Add(obj.State);
                }
        }

        public void Send()
        {
            foreach (var packet in packets)
            {
                if (IncrementTimestamp) Map.Map.Instance.Timestamp = Map.Map.Instance.Timestamp.Next();
                packet.Send(rpcParams);
            }
            Clear();
        }
    }
}