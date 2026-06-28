using System;
using Unity.Netcode;

namespace Map.Fleet
{
    public struct VehicleAction: INetworkSerializable
    {
        public enum ActionType : byte
        {
            DriveRoute,
            LoadTruck,
            UnloadTruck,
            WaitForTruck
        }

        public ActionType Type;
        public TileId TargetTileId;
        public TileId[] RouteIds;

        public VehicleAction(ActionType type, TileId targetTileId, TileId[] routeIds = null)
        {
            Type = type;
            TargetTileId = targetTileId;
            RouteIds = routeIds;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            TileId[] routeIds = null;
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref TargetTileId);
            if (serializer.IsWriter)
            {
                routeIds = RouteIds ?? new TileId[] { };
                serializer.SerializeValue(ref routeIds);
            }
            else
            {
                serializer.SerializeValue(ref routeIds);
                RouteIds = routeIds.Length > 0 ? routeIds : null;
            }
        }
    }
}