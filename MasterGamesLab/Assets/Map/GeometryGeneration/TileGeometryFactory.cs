using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public static class TileGeometryFactory
    {
        private static readonly float TanPI3 = Mathf.Tan(Mathf.PI / 3);
        private static readonly float CosPI6 = Mathf.Cos(Mathf.PI / 6);

        private const float INSET_FACTOR = 0.99f;

        private static readonly Vector2[] HexagonCoordinates =
        {
            new Vector2(-128 * TanPI3, 128) * INSET_FACTOR,
            new Vector2(0, 256) * INSET_FACTOR,
            new Vector2(128 * TanPI3, 128) * INSET_FACTOR,
            new Vector2(128 * TanPI3, -128) * INSET_FACTOR,
            new Vector2(0, -256) * INSET_FACTOR,
            new Vector2(-128 * TanPI3, -128) * INSET_FACTOR,
        };

        private static readonly float Diff = 128 * TanPI3 - 128 / CosPI6;

        private const float INSET_FACTOR_TRIANGLE = 0.98f;

        private static readonly Vector2[] TriangleCoordinates =
        {
            new Vector2(-128, Diff) * INSET_FACTOR_TRIANGLE,
            new Vector2(128, Diff) * INSET_FACTOR_TRIANGLE,
            new Vector2(0, -128 / CosPI6) * INSET_FACTOR_TRIANGLE,
        };

        private static readonly Vector2 PlainColor = new(320, 1762);
        private static readonly Vector2 ForrestColor = new(763, 1762);
        private static readonly Vector2 MountainColor = new(1207, 1762);

        private static readonly Vector2 WaterCenter = new(128 * TanPI3, 256);
        private static readonly Vector2 PlainCenter = new(2 * 128 * TanPI3, 3 * 256);
        private static readonly Vector2 ForrestCenter = new(128 * TanPI3, 5 * 256);
        private static readonly Vector2 MountainCenter = new(2500 - 128 * TanPI3, 2048 - 256);

        private static readonly Vector2 ShoreTriangleCenter = new(128, 2048 - Diff);

        private static readonly Vector2 TextureSize = new(2500, 2048);
        private static readonly Vector2 InvTextureSize = new(1f / TextureSize.x, 1f / TextureSize.y);

        private const float WATER_HEIGHT = 0.99f;
        private const float LAND_HEIGHT = 1f;
        private const float MOUNTAIN_HEIGHT = 1.04f;
        private static readonly float BorderHeight = Math.Min(WATER_HEIGHT, Math.Min(LAND_HEIGHT, MOUNTAIN_HEIGHT));

        private const int MIN_TREES_PER_TRIANGLE = 1;
        private const int MAX_TREES_PER_TRIANGLE = 2;
        private const float MIN_TREE_SCALE = 0.019f;
        private const float MAX_TREE_SCALE = 0.023f;

        public static int BuildFaces(Tile tile, MapChunk.ChunkGeometry cg)
        {
            var tileDataVec = tile.GetTileData();

            var center = tile.Type switch
            {
                Tile.TileType.Water => WaterCenter,
                Tile.TileType.Plain => PlainCenter,
                Tile.TileType.Forest => ForrestCenter,
                Tile.TileType.Mountain => MountainCenter,
                _ => throw new ArgumentOutOfRangeException()
            };

            var centerHeight = tile.Type switch
            {
                Tile.TileType.Water => tile.Chunk.Parent.Radius * WATER_HEIGHT,
                Tile.TileType.Plain => tile.Chunk.Parent.Radius * LAND_HEIGHT, //  + randomValue * 0.01f),
                Tile.TileType.Forest => tile.Chunk.Parent.Radius * LAND_HEIGHT,
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * MOUNTAIN_HEIGHT,
                _ => throw new ArgumentOutOfRangeException()
            };

            var perimeterHeight = tile.Type switch
            {
                Tile.TileType.Water => centerHeight,
                Tile.TileType.Plain => centerHeight,
                Tile.TileType.Forest => centerHeight,
                Tile.TileType.Mountain => tile.Chunk.Parent.Radius * LAND_HEIGHT,
                _ => throw new ArgumentOutOfRangeException()
            };

            var addedVertices = 0;
            if (tile.Type is Tile.TileType.Plain or Tile.TileType.Forest or Tile.TileType.Mountain)
            {
                addedVertices += BuildFullHexagon(tile, cg, center, centerHeight, perimeterHeight, tileDataVec);
            }
            else
            {
                addedVertices += BuildSegmentedHexagon(tile, cg, center, centerHeight, perimeterHeight, tileDataVec);
            }

            addedVertices += BuildTileBorders(tile, cg, perimeterHeight, tileDataVec);

            if (tile.Type == Tile.TileType.Forest)
            {
                var centerPos = tile.PositionOnSphere;
                foreach (var neighbor in tile.NeighborTiles)
                {
                    var numTreesInTriangle =
                        UnityEngine.Random.Range(MIN_TREES_PER_TRIANGLE, MAX_TREES_PER_TRIANGLE + 1);
                    for (var i = 0; i < numTreesInTriangle; i++)
                    {
                        var r1 = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f));
                        var r2 = UnityEngine.Random.Range(0f, 1f);

                        var p1 = neighbor.LeftVertex;
                        var p2 = neighbor.RightVertex;

                        var pos = (1 - r1) * centerPos + (r1 * (1 - r2)) * p1 + (r1 * r2) * p2;

                        pos = pos.normalized * tile.Chunk.Parent.Radius;

                        tile.Chunk.AddTree(new Map.TreeData
                        {
                            Position = pos,
                            Normal = pos.normalized,
                            Scale = UnityEngine.Random.Range(MIN_TREE_SCALE, MAX_TREE_SCALE),
                            Yaw = UnityEngine.Random.Range(0f, 360f),
                            Random = UnityEngine.Random.Range(0f, 1f),
                        });
                    }
                }
            }

            return addedVertices;
        }

        private static int BuildFullHexagon(Tile tile, MapChunk.ChunkGeometry cg, Vector2 center, float centerHeight,
            float perimeterHeight, Vector4 tileDataVec)
        {
            var centerVertex = tile.PositionOnSphere.normalized * centerHeight;
            var animate = tile.Type != Tile.TileType.Mountain;
            var startIdx = cg.Vertices.Count;
            var addedVertices = 0;

            cg.Vertices.Add(centerVertex);
            cg.TileData.Add(tileDataVec);
            cg.MaterialData.Add(BuildMaterialData(center, Vector2.zero, animate, tile.RandomValue));
            addedVertices++;

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];

                cg.Vertices.Add(neighborTile.LeftVertex * perimeterHeight);
                cg.TileData.Add(tileDataVec);
                cg.MaterialData.Add(BuildMaterialData(center, HexagonCoordinates[i], animate, tile.RandomValue));
                addedVertices++;
            }

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var next = (i + 1) % tile.NeighborTiles.Count;

                cg.Triangles.Add(startIdx + 1 + i);
                cg.Triangles.Add(startIdx + 1 + next);
                cg.Triangles.Add(startIdx);
            }

            return addedVertices;
        }

        private static int BuildSegmentedHexagon(Tile tile, MapChunk.ChunkGeometry cg, Vector2 center,
            float centerHeight, float perimeterHeight, Vector4 tileDataVec)
        {
            var centerVertex = tile.PositionOnSphere.normalized * centerHeight;
            var animate = tile.Type != Tile.TileType.Mountain;
            var startIdx = cg.Vertices.Count;
            var addedVertices = 0;

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];

                cg.Vertices.Add(neighborTile.LeftVertex * perimeterHeight);
                cg.Vertices.Add(neighborTile.RightVertex * perimeterHeight);
                cg.Vertices.Add(centerVertex);

                cg.TileData.Add(tileDataVec);
                cg.TileData.Add(tileDataVec);
                cg.TileData.Add(tileDataVec);

                if (neighborTile.Tile.Type != tile.Type)
                {
                    cg.MaterialData.Add(BuildMaterialData(ShoreTriangleCenter, TriangleCoordinates[0], animate,
                        tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(ShoreTriangleCenter, TriangleCoordinates[1], animate,
                        tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(ShoreTriangleCenter, TriangleCoordinates[2], animate,
                        tile.RandomValue));
                }
                else
                {
                    var next = (i + 1) % tile.NeighborTiles.Count;
                    cg.MaterialData.Add(BuildMaterialData(center, HexagonCoordinates[i], animate, tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(center, HexagonCoordinates[next], animate, tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(center, Vector2.zero, animate, tile.RandomValue));
                }

                cg.Triangles.Add(startIdx + addedVertices + 0);
                cg.Triangles.Add(startIdx + addedVertices + 1);
                cg.Triangles.Add(startIdx + addedVertices + 2);
                addedVertices += 3;
            }

            return addedVertices;
        }

        private static int BuildTileBorders(Tile tile, MapChunk.ChunkGeometry cg, float perimeterHeight,
            Vector4 tileDataVec)
        {
            var startIdx = cg.Vertices.Count;
            var addedVertices = 0;

            var borderColor = tile.Type switch
            {
                Tile.TileType.Plain => PlainColor,
                Tile.TileType.Forest => ForrestColor,
                Tile.TileType.Mountain => MountainColor,
                Tile.TileType.Water => PlainColor,
                _ => throw new ArgumentOutOfRangeException(nameof(tile.Type), tile.Type, null)
            };

            foreach (var neighborTile in tile.NeighborTiles)
            {
                if (tile.Type == Tile.TileType.Water || neighborTile.Tile.Type != Tile.TileType.Water)
                {
                    continue;
                }

                var bottomLeft = neighborTile.LeftVertex.normalized * BorderHeight;
                var bottomRight = neighborTile.RightVertex.normalized * BorderHeight;

                cg.Vertices.Add(bottomLeft);
                cg.Vertices.Add(bottomRight);
                cg.Vertices.Add(neighborTile.LeftVertex * perimeterHeight);
                cg.Vertices.Add(neighborTile.RightVertex * perimeterHeight);

                for (var j = 0; j < 4; j++)
                {
                    cg.TileData.Add(tileDataVec);
                    cg.MaterialData.Add(BuildMaterialData(borderColor, Vector2.zero, false, tile.RandomValue));
                }

                cg.Triangles.Add(startIdx + addedVertices);
                cg.Triangles.Add(startIdx + addedVertices + 1);
                cg.Triangles.Add(startIdx + addedVertices + 2);

                cg.Triangles.Add(startIdx + addedVertices + 1);
                cg.Triangles.Add(startIdx + addedVertices + 3);
                cg.Triangles.Add(startIdx + addedVertices + 2);
                addedVertices += 4;
            }

            return addedVertices;
        }

        private static Vector4 BuildMaterialData(Vector2 center, Vector2 offset, bool animate, float randomValue)
        {
            var uv = (center + offset) * InvTextureSize;
            return new Vector4(uv.x, uv.y, animate ? 1 : 0, randomValue);
        }
    }
}