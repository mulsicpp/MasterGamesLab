using Map.GeometryGeneration.Edges;
using System;
using System.Collections.Generic;
using UI;

namespace Map
{
    public class EntityIdManager
    {
        public readonly Range TileRange;
        public readonly Range EdgeRange;
        public readonly Range VehicleRange;
        public readonly Range StructureRange;
        public readonly Range SelectableRouteRange;
        public readonly Range VehicleActionQueueRange;

        private IMapEntity[] entities;

        public EntityIdManager()
        {
            var map = Map.Instance;

            var selectableRouteCount = Enum.GetValues(typeof(Route.RouteType)).Length;

            TileRange = 0..map.Tiles.Count;
            EdgeRange = TileRange.End..(TileRange.End.Value + map.Edges.Count);
            VehicleRange = EdgeRange.End..(EdgeRange.End.Value + map.Fleet.Vehicles.Count);
            StructureRange = VehicleRange.End..(VehicleRange.End.Value + map.Infrastructure.Structures.Count);
            SelectableRouteRange = StructureRange.End..(StructureRange.End.Value + selectableRouteCount);
            VehicleActionQueueRange = SelectableRouteRange.End..(SelectableRouteRange.End.Value + Constants.MAX_VEHICLE_ACTION_COUNT_PER_VEHICLE);

            List<IMapEntity> tempEntities = new List<IMapEntity>();
            tempEntities.AddRange(map.Tiles);
            tempEntities.AddRange(map.Edges);
            tempEntities.AddRange(map.Fleet.Vehicles);
            tempEntities.AddRange(map.Infrastructure.Structures);
            tempEntities.AddRange(new IMapEntity[selectableRouteCount]);
            tempEntities.AddRange(new IMapEntity[Constants.MAX_VEHICLE_ACTION_COUNT_PER_VEHICLE]);

            entities = tempEntities.ToArray();
        }

        public IMapEntity this[EntityId id]
        {
            get => entities[id];
            set => entities[id] = value;
        }
    }
}