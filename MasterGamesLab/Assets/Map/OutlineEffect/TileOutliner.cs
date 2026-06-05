using System;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map.OutlineEffect
{
    public class TileOutliner : AOutlinableObject
    {
        private const float DELTA = 0.001f;

        private void Awake()
        {
            Init();
        }

        public void OutlineTile(Tile tile)
        {
            ClearMeshData();
            BuildTileGeometry(tile);
            StoreMeshData();
        }

        public void ClearOutline()
        {
            ClearMeshData();
            StoreMeshData();
        }

        private void BuildTileGeometry(Tile tile)
        {
            var centerHeight = tile.Type switch
            {
                Tile.TileType.Water => tile.Chunk.Parent.Radius * (TileGeometryFactory.WATER_HEIGHT + DELTA),
                Tile.TileType.Plain => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + DELTA),
                Tile.TileType.Forest => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + DELTA),
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * (TileGeometryFactory.MOUNTAIN_HEIGHT + DELTA),
                _ => throw new ArgumentOutOfRangeException()
            };

            var perimeterHeight = tile.Type switch
            {
                Tile.TileType.Water => centerHeight,
                Tile.TileType.Plain => centerHeight,
                Tile.TileType.Forest => centerHeight,
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + DELTA),
                _ => throw new ArgumentOutOfRangeException()
            };

            var lowerCenterVertex = tile.PositionOnSphere.normalized * centerHeight;

            AddVertex(lowerCenterVertex, Vector4.zero);

            foreach (var neighborTile in tile.NeighborTiles)
            {
                AddVertex(neighborTile.LeftVertex * perimeterHeight, Vector4.zero);
            }

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                Triangles.Add(1 + i);
                Triangles.Add(1 + next);
                Triangles.Add(0);
            }
        }

        private void BuildHexagonPrism(Tile tile, float lowerHeight, float upperHeight)
        {
            var lowerCenterVertex = tile.PositionOnSphere.normalized * lowerHeight;

            AddVertex(lowerCenterVertex, Vector4.zero);

            foreach (var neighborTile in tile.NeighborTiles)
            {
                AddVertex(neighborTile.LeftVertex * lowerHeight, Vector4.zero);
            }

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                Triangles.Add(1 + next);
                Triangles.Add(1 + i);
                Triangles.Add(0);
            }

            // Upper Hexagon
            var addedVertices = Vertices.Count;
            var upperCenterVertex = tile.PositionOnSphere.normalized * upperHeight;

            AddVertex(upperCenterVertex, Vector4.zero);

            foreach (var neighborTile in tile.NeighborTiles)
            {
                AddVertex(neighborTile.LeftVertex * upperHeight, Vector4.zero);
            }

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                Triangles.Add(addedVertices + 1 + i);
                Triangles.Add(addedVertices + 1 + next);
                Triangles.Add(addedVertices);

                Triangles.Add(1 + i);
                Triangles.Add(addedVertices + 1 + next);
                Triangles.Add(addedVertices + 1 + i);

                Triangles.Add(1 + i);
                Triangles.Add(1 + next);
                Triangles.Add(addedVertices + 1 + next);
            }
        }
    }
}