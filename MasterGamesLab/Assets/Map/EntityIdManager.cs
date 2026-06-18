
using System;
using System.CodeDom;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace Map
{
    public class EntityIdManager
    {

        public readonly Range TileRange;
        public readonly Range EdgeRange;
        public readonly Range VehicleRange;
        public readonly Range StructureRange;

        private IMapEntity[] entities;

        public EntityIdManager()
        {
            var map = Map.Instance;

            TileRange = 0..map.Tiles.Count;
            EdgeRange = TileRange.End..(TileRange.End.Value + map.Edges.Count);
            VehicleRange = EdgeRange.End..(EdgeRange.End.Value + map.Fleet.Vehicles.Count);
            StructureRange = VehicleRange.End..(VehicleRange.End.Value + map.Infrastructure.Structures.Count);

            List<IMapEntity> tempEntities = new List<IMapEntity>();
            tempEntities.AddRange(map.Tiles);
            tempEntities.AddRange(map.Edges);
            tempEntities.AddRange(map.Fleet.Vehicles);
            tempEntities.AddRange(map.Infrastructure.Structures);

            entities = tempEntities.ToArray();
        }

        public IMapEntity this[EntityId id]
        {
            get => entities[id];
        }
    }
}