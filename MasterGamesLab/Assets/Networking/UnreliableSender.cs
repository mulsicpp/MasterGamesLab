
using Map;
using Map.Fleet;
using Map.Infrastructure;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class UnreliableSender
    {
        public readonly ClientId ClientId;

        private readonly ClientRpcParams rpcParams;

        public class Packet
        {
            private int nettoSize;
            public int NettoSize => nettoSize;

            private List<Vehicle.VehicleProgressState> vehicleProgresses;

            public Packet()
            {
                nettoSize = 0;
                vehicleProgresses = new();
            }

            public void InsertState(IState state)
            {
                switch (state)
                {
                    case Vehicle.VehicleProgressState v: vehicleProgresses.Add(v); break;
                    default: return;
                }
                nettoSize += state.SerializedSize;
            }

            public void Send(ClientRpcParams rpcParams)
            {
                if (nettoSize == 0) return;
                var map = Map.Map.Instance;
                map.ApplyUnreliableStatesClientRpc(
                    Time.timeAsDouble,
                    vehicleProgresses.ToArray(),
                    rpcParams
                );
            }
        }

        private List<Packet> packets;


        public UnreliableSender() : this(ClientId.NONE) { }
        public UnreliableSender(ClientId clientId)
        {
            packets = new List<Packet>();

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

            if(packets.Count == 0)
            {
                packets.Add(new Packet());
            }

            Packet lastPacket = packets[packets.Count - 1];

            if (lastPacket.NettoSize != 0 && lastPacket.NettoSize + size > Constants.MAX_SYNC_STATE_BYTES_PER_RPC)
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
                packet.Send(rpcParams);
            Clear();
        }
    }
}