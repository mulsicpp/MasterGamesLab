using System;
using InGameCamera;
using Map.OutlineEffect;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class TileBeacon : AObjectWithProcedualGeometry, ITileEffect
    {
        private const float MIN_DELTA = 0.0001f;
        private const float MAX_DELTA = 0.012f;
        private const float MIN_DIST = 2f;
        private const float MAX_DIST = 2.6f;
        private const float INSET = 0.004f;

        private void Awake()
        {
            Init();
            CurrentlyHoverable = false;
            SetBaseLayer();
        }

        public void HighlightTile(Tile tile)
        {
            ClearMeshData();
            BuildBeaconMesh(tile);
            StoreMeshData();
        }

        public void ClearEffect()
        {
            ClearMeshData();
            StoreMeshData();
        }

        private void BuildBeaconMesh(Tile tile)
        {
            var currentDistance = MainCamera.Instance.CurrentDistance;
            var t = (currentDistance - MIN_DIST) / (MAX_DIST - MIN_DIST);
            var delta = Mathf.Lerp(MIN_DELTA, MAX_DELTA, t);
            // Debug.Log($"delta: {delta}, t: {t}");

            var centerHeight = tile.Type switch
            {
                Tile.TileType.Water => tile.Chunk.Parent.Radius * (TileGeometryFactory.WATER_HEIGHT + delta),
                Tile.TileType.Plain => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + delta),
                Tile.TileType.Forest => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + delta),
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * (TileGeometryFactory.MOUNTAIN_HEIGHT + delta),
                _ => throw new ArgumentOutOfRangeException()
            };

            var perimeterHeight = tile.Type switch
            {
                Tile.TileType.Water => centerHeight,
                Tile.TileType.Plain => centerHeight,
                Tile.TileType.Forest => centerHeight,
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * (TileGeometryFactory.LAND_HEIGHT + delta),
                _ => throw new ArgumentOutOfRangeException()
            };

            BuildHexagonPrism(tile, centerHeight, perimeterHeight);
        }

        private void BuildHexagonPrism(Tile tile, float centerHeight, float perimeterHeight)
        {
            var lowerCenterVertex = tile.PositionOnSphere.normalized * centerHeight;
            var lowerLeft = new Vector4(0, 0, 0, 0);
            var lowerRight = new Vector4(0, 0, 1, 0);
            var upperLeft = new Vector4(0, 0, 0, 1);
            var upperRight = new Vector4(0, 0, 0, 1);

            // Lower ring 
            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];
                AddVertex(neighborTile.LeftVertex * perimeterHeight, i % 2 == 0 ? lowerLeft : lowerRight);
            }

            // Upper ring
            var addedVertices = Vertices.Count;
            var upperHeight = perimeterHeight + Map.Instance.TEST_BEACON_HEIGHT * Map.Instance.TileScale;
            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];
                AddVertex(neighborTile.LeftVertex * upperHeight, i % 2 == 0 ? upperLeft : upperRight);
            }

            // Triangles
            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                Triangles.Add(i);
                Triangles.Add(addedVertices + next);
                Triangles.Add(addedVertices + i);

                Triangles.Add(i);
                Triangles.Add(next);
                Triangles.Add(addedVertices + next);
            }

            // Lower inner ring
            addedVertices = Vertices.Count;
            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];
                var pos = neighborTile.LeftVertex * perimeterHeight;
                pos += (lowerCenterVertex - pos).normalized * INSET;
                AddVertex(pos, i % 2 == 0 ? lowerLeft : lowerRight);
            }

            // Ring Triangles
            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                Triangles.Add(i);
                Triangles.Add(addedVertices + next);
                Triangles.Add(addedVertices + i);

                Triangles.Add(i);
                Triangles.Add(next);
                Triangles.Add(addedVertices + next);
            }
        }
    }
}