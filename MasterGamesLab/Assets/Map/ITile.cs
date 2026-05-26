using Map.Infrastructure;
using System.Collections.Generic;
using Map.GeometryGeneration;
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

        int ContinentId { get; set; }

        public IReadOnlyList<Edge> Edges { get; }

        public Structure Structure { get; }

        public bool CanSpawnStructure(Structure.StructureType type);
        
        public void BuildFaces(MapChunk.ChunkGeometry cg);

        public void FillTileData(List<Vector4> tileDataList, List<Map.TreeData> treeDataList);
    }
}