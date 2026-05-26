using Map.Fleet;
using Map.Infrastructure;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public interface ITile
    {
        public TileId Id { get; }

        public Vector3 PositionOnSphere { get; }

        public Tile.TileType Type { get; set; }

        public bool Active { get; set; }

        public IReadOnlyList<ITile> Neighbors { get; }

        public IReadOnlyList<Edge> Edges { get; }

        public Structure Structure { get; }

        public void BuildFaces(List<Vector3> vertices, List<int> triangles, List<Vector4> tileData,
            List<Vector4> materialData);

        public int CountEdgesWithType(Edge.EdgeType type);
        public Edge FindEdgeTo(ITile other);

        public bool CanSpawnStructure(Structure.StructureType type);
        public bool CanSpawnVehicle(Vehicle.VehicleType type);
    }
}