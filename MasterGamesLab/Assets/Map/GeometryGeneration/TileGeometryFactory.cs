using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace Map.GeometryGeneration
{
    public static class TileGeometryFactory
    {
        public struct CanalSectionData
        {
            public Tile Tile;
            public int StartIdx;
            public int EndIdx;
            public bool IncludeHalfTriangleBefore; // to split a section between two edges.
            public bool IncludeHalfTriangleAfter; // Does NOT work with IncludeBorderToNeighbors
            public bool IsBluePrint;
            public Vector4 UV1Data;
            public Vector2 LandTextureCenter;
            public Vector2 CanalWallsTextureCenter;
            public float LandHeight;
            public float WaterHeight;
            public bool IncludeWater;
            public bool IncludeLand;
            public bool IncludeCanalWalls;
            public bool IncludeBorderToNeighbors;
        }

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

        private const float INSET_FACTOR_CANAL = 0.98f;

        private static readonly Vector2 PlainColor = new(320, 1762);
        private static readonly Vector2 ForrestColor = new(763, 1762);
        private static readonly Vector2 MountainColor = new(1207, 1762);

        private static readonly Vector2 WaterCenter = new(128 * TanPI3, 256);
        private static readonly Vector2 PlainCenter = new(2 * 128 * TanPI3, 3 * 256);
        private static readonly Vector2 ForrestCenter = new(128 * TanPI3, 5 * 256);
        private static readonly Vector2 MountainCenter = new(2500 - 128 * TanPI3, 2048 - 256);

        private static readonly Vector2 ShoreTriangleCenter = new(128, 2048 - Diff);

        private static readonly Vector2 CanalRectangleOrigin = new(0, 2405);
        private static readonly float CanalRectangleWidth = 280;
        private static readonly float CanalRectangleHeight = 190;
        private static readonly Vector2 CanalTriangleCenter = new(128, 2048 - Diff + 262);

        private static readonly Vector2 TextureSize = new(2500, 2500);
        private static readonly Vector2 InvTextureSize = new(1f / TextureSize.x, 1f / TextureSize.y);

        public const float WATER_HEIGHT = 0.99f;
        public const float LAND_HEIGHT = 1f;
        public const float MOUNTAIN_HEIGHT = 1.04f;
        private static readonly float BorderHeight = Math.Min(WATER_HEIGHT, Math.Min(LAND_HEIGHT, MOUNTAIN_HEIGHT));

        private const int MIN_TREES_PER_TRIANGLE = 1;
        private const int MAX_TREES_PER_TRIANGLE = 2;
        private const float MIN_TREE_SCALE = 0.019f;
        private const float MAX_TREE_SCALE = 0.023f;

        private static float canalInsetLand;
        private static float canalInsetWater;
        private static float canalRandomStrength;

        private static Vector2 canalTextureTopBeginning;
        private static Vector2 canalTextureTopLowerMiddle;
        private static Vector2 canalTextureTopUpperMiddle;
        private static Vector2 canalTextureTopEnd;
        private static Vector2 canalTextureBottomBeginning;
        private static Vector2 canalTextureBottomLowerMiddle;
        private static Vector2 canalTextureBottomUpperMiddle;
        private static Vector2 canalTextureBottomEnd;
        private static Vector2 canalTextureMiddleEnd;

        // <topBeginning>------<topLowerMiddle>------<topUpperMiddle>------<topEnd>
        //                                                                 <middleEnd>
        // <bottomBeginning>---<bottomLowerMiddle>---<bottomUpperMiddle>---<bottomEnd>

        private const float SIN_30 = 0.5f;
        private const float SIN_60 = 0.866025403784f;
        private const float COS_30 = 0.866025403784f;
        private const float COS_60 = 0.5f;

        public static void SetCanalInset(float insetLand, float insetWater, float random)
        {
            canalInsetLand = insetLand;
            canalInsetWater = insetWater;
            canalRandomStrength = random;

            var bTick = (1 - insetLand) / SIN_30;
            var percentageAlongVertexToCenter = 1f - bTick;

            canalTextureTopBeginning = new Vector2(0, CanalRectangleHeight * 0.5f);
            canalTextureBottomBeginning = new Vector2(0, -CanalRectangleHeight * 0.5f);

            canalTextureTopLowerMiddle = new Vector2(percentageAlongVertexToCenter * COS_30 * CanalRectangleWidth,
                CanalRectangleHeight * 0.5f);
            canalTextureBottomLowerMiddle = new Vector2(percentageAlongVertexToCenter * COS_30 * CanalRectangleWidth,
                -CanalRectangleHeight * 0.5f);

            canalTextureTopUpperMiddle = new Vector2(bTick / COS_30 * CanalRectangleWidth, CanalRectangleHeight * 0.5f);
            canalTextureBottomUpperMiddle =
                new Vector2(bTick / COS_30 * CanalRectangleWidth, -CanalRectangleHeight * 0.5f);

            canalTextureTopEnd = new Vector2(CanalRectangleWidth, CanalRectangleHeight * 0.5f);
            canalTextureBottomEnd = new Vector2(CanalRectangleWidth, -CanalRectangleHeight * 0.5f);

            canalTextureMiddleEnd = new Vector2(CanalRectangleWidth, 0);
        }

        private static (float percentageAlongVertexToCenter, float percentageAlongCenterLineToCenter, float
            percentageAlongEdgeToVertex) CalculatePercentages(float inset)
        {
            var bTick = (1 - inset) / SIN_30;
            var percentageAlongVertexToCenter = 1f - bTick;
            var percentageAlongEdgeToVertex = (0.5f - percentageAlongVertexToCenter * COS_60) * 2f;
            var percentageAlongCenterLineToCenter = (SIN_60 - bTick * 0.5f / COS_30) * (1f / SIN_60);

            return (percentageAlongVertexToCenter, percentageAlongCenterLineToCenter, percentageAlongEdgeToVertex);
        }

        public static MapChunk.TileGeometryInformation BuildFaces(Tile tile, MapChunk.ChunkGeometry cg)
        {
            var tileDataVec = tile.GetTileData();

            var textureCenter = tile.Type switch
            {
                Tile.TileType.Water => WaterCenter,
                Tile.TileType.Plain => PlainCenter,
                Tile.TileType.Forest => ForrestCenter,
                Tile.TileType.Mountain => MountainCenter,
                _ => throw new ArgumentOutOfRangeException()
            };

            var textureBorderColor = tile.Type switch
            {
                Tile.TileType.Plain => PlainColor,
                Tile.TileType.Forest => ForrestColor,
                Tile.TileType.Mountain => MountainColor,
                Tile.TileType.Water => PlainColor,
                _ => throw new ArgumentOutOfRangeException(nameof(tile.Type), tile.Type, null)
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
            var starTreeIdx = cg.TreeData.Count;
            var hasCanals = tile.Edges.Any(edge => edge.Type == Edge.EdgeType.Canal);

            if (hasCanals && tile.Type != Tile.TileType.Water)
            {
                addedVertices += BuildCanalSection(new CanalSectionData
                {
                    Tile = tile,
                    StartIdx = 0,
                    EndIdx = tile.NeighborTiles.Count,
                    IncludeHalfTriangleBefore = false,
                    IncludeHalfTriangleAfter = false,
                    IsBluePrint = false,
                    UV1Data = tileDataVec,
                    LandTextureCenter = textureCenter,
                    CanalWallsTextureCenter = textureBorderColor,
                    LandHeight = LAND_HEIGHT,
                    WaterHeight = WATER_HEIGHT,
                    IncludeWater = true,
                    IncludeLand = true,
                    IncludeCanalWalls = true,
                    IncludeBorderToNeighbors = true,
                }, cg);
            }
            else
            {
                if (tile.Type is Tile.TileType.Plain or Tile.TileType.Forest or Tile.TileType.Mountain)
                {
                    addedVertices += BuildFullHexagon(tile, cg, textureCenter, centerHeight, perimeterHeight,
                        tileDataVec);
                }
                else
                {
                    addedVertices +=
                        BuildSegmentedHexagon(tile, cg, textureCenter, centerHeight, perimeterHeight, tileDataVec);
                }

                addedVertices += BuildTileBorders(tile, cg, perimeterHeight, tileDataVec, textureBorderColor);

                if (tile.Type == Tile.TileType.Forest)
                {
                    var shouldHaveTrees = tile.Structure == null &&
                                          tile.Edges.All(edge => edge.Type == Edge.EdgeType.None);

                    if (shouldHaveTrees)
                    {
                        AddTrees(tile, cg);
                    }
                }
            }

            return new MapChunk.TileGeometryInformation
            {
                NumVertices = addedVertices,
                StartTreeIdx = starTreeIdx,
                EndTreeIdx = cg.TreeData.Count,
            };
        }

        private static int BuildFullHexagon(Tile tile, MapChunk.ChunkGeometry cg, Vector2 center, float centerHeight,
            float perimeterHeight, Vector4 tileDataVec)
        {
            var centerVertex = tile.PositionOnSphere.normalized * centerHeight;
            var animate = tile.Type != Tile.TileType.Mountain;
            var startIdx = cg.Vertices.Count;
            var addedVertices = 0;

            cg.AddVertex(centerVertex, tileDataVec, BuildMaterialData(center, Vector2.zero, animate, tile.RandomValue));
            addedVertices++;

            for (var i = 0; i < tile.NeighborTiles.Count; i++)
            {
                var neighborTile = tile.NeighborTiles[i];

                cg.AddVertex(neighborTile.LeftVertex * perimeterHeight, tileDataVec,
                    BuildMaterialData(center, HexagonCoordinates[i], animate, tile.RandomValue));
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

                if (neighborTile.Tile.Type != tile.Type &&
                    tile.FindEdgeTo(neighborTile.Tile)?.Type != Edge.EdgeType.Canal)
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
                    cg.MaterialData.Add(BuildMaterialData(center, HexagonCoordinates[next], animate, tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(center, HexagonCoordinates[i], animate, tile.RandomValue));
                    cg.MaterialData.Add(BuildMaterialData(center, Vector2.zero, animate, tile.RandomValue));
                }

                cg.Triangles.Add(startIdx + addedVertices + 0);
                cg.Triangles.Add(startIdx + addedVertices + 2);
                cg.Triangles.Add(startIdx + addedVertices + 1);
                addedVertices += 3;
            }

            return addedVertices;
        }

        private static int BuildTileBorders(Tile tile, MapChunk.ChunkGeometry cg, float perimeterHeight,
            Vector4 tileDataVec, Vector2 borderColor)
        {
            var startIdx = cg.Vertices.Count;
            var addedVertices = 0;

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
                cg.Triangles.Add(startIdx + addedVertices + 2);
                cg.Triangles.Add(startIdx + addedVertices + 1);

                cg.Triangles.Add(startIdx + addedVertices + 2);
                cg.Triangles.Add(startIdx + addedVertices + 3);
                cg.Triangles.Add(startIdx + addedVertices + 1);
                addedVertices += 4;
            }

            return addedVertices;
        }

        private static void AddTrees(Tile tile, MapChunk.ChunkGeometry cg)
        {
            var prng = new System.Random(tile.Id.Value);

            var centerPos = tile.PositionOnSphere;
            foreach (var neighbor in tile.NeighborTiles)
            {
                var numTreesInTriangle = prng.Next(MIN_TREES_PER_TRIANGLE, MAX_TREES_PER_TRIANGLE + 1);
                for (var i = 0; i < numTreesInTriangle; i++)
                {
                    var r1 = Mathf.Sqrt((float)prng.NextDouble());
                    var r2 = (float)prng.NextDouble();

                    var p1 = neighbor.LeftVertex;
                    var p2 = neighbor.RightVertex;

                    var pos = (1 - r1) * centerPos + (r1 * (1 - r2)) * p1 + (r1 * r2) * p2;

                    pos = pos.normalized * tile.Chunk.Parent.Radius;

                    cg.TreeData.Add(new Map.TreeData
                    {
                        Position = pos,
                        Normal = pos.normalized,
                        Scale = (float)prng.NextDouble() * (MAX_TREE_SCALE - MIN_TREE_SCALE) +
                                MIN_TREE_SCALE,
                        Yaw = (float)prng.NextDouble() * 360f,
                        Random = (float)prng.NextDouble(),
                        Active = tile.Active ? 1 : 0,
                    });
                }
            }
        }

        public static int BuildCanalSection(CanalSectionData data, MapChunk.ChunkGeometry cg)
        {
            //            tileCenter
            //                *
            //               / \
            //              /   \ UpperPointsToCenter
            //             /  *  \ PointOnCenterLine
            //            /       \ LowerPointsToCenter
            //           /|       |\
            //          / |       | \
            //         /__|___*___|__\
            // edgeLeft  edgeCenter edgeRight
            //            |      | 
            //           PointsOnEdge

            var (pctVertTileLand, pctCenterLineTileLand, pctEdgeCenterVertLand) = CalculatePercentages(canalInsetLand);
            var (pctVertTileWater, pctCenterLineTileWater, pctEdgeCenterVertWater) =
                CalculatePercentages(canalInsetWater);

            var tile = data.Tile;
            var uvCenter = data.LandTextureCenter;
            var uvCenterWall = data.CanalWallsTextureCenter;
            var uv1 = data.UV1Data;
            var landHeight = tile.Chunk.Parent.Radius * data.LandHeight;
            var waterHeight = tile.Chunk.Parent.Radius * data.WaterHeight;

            var tileCenterW = tile.PositionOnSphere.normalized * waterHeight;
            var tileCenterL = tile.PositionOnSphere.normalized * landHeight;

            var startIdx = cg.Vertices.Count;
            var currentIdx = startIdx;

            cg.AddVertex(tileCenterW, uv1,
                BuildMaterialData(CanalRectangleOrigin, canalTextureMiddleEnd, true, tile.RandomValue));
            currentIdx++;

            var neighbors = tile.NeighborTiles;
            var offsets = GetRandoms(tile.Id.Value, 3 * neighbors.Count * 2, GetRandomOffset);

            var forStartIdx = data.IncludeHalfTriangleBefore ? data.StartIdx - 1 : data.StartIdx;
            var forEndIdx = data.EndIdx;

            if (forStartIdx < 0)
            {
                forStartIdx += neighbors.Count;
                forEndIdx += neighbors.Count;
            }

            if (forEndIdx <= forStartIdx)
            {
                forEndIdx += neighbors.Count;
            }

            if (data.IncludeHalfTriangleAfter)
            {
                forEndIdx++;
            }

            for (var i = forStartIdx; i < forEndIdx; i++)
            {
                var onlyLeftSubTriangle = i == forStartIdx && data.IncludeHalfTriangleBefore;
                var onlyRightSubTriangle = i == forEndIdx - 1 && data.IncludeHalfTriangleAfter;

                var iCurr = i % neighbors.Count;
                var iNext = (iCurr + 1) % neighbors.Count;
                var iPrev = (iCurr - 1 + neighbors.Count) % neighbors.Count;

                var prevEdge = tile.FindEdgeTo(neighbors[iPrev].Tile);
                var edge = tile.FindEdgeTo(neighbors[iCurr].Tile);
                var nextEdge = tile.FindEdgeTo(neighbors[iNext].Tile);

                var (edgeLeftL, edgeRightL, edgeCenterL) = GetPoints(neighbors[iCurr], landHeight);
                var (edgeLeftW, edgeRightW, edgeCenterW) = GetPoints(neighbors[iCurr], waterHeight);

                var randomsEdge = GetRandoms(edge != null ? edge.Id.Value : 0, 2 * 2, GetRandomValue);
                if (edge != null && Vector3.Dot(Vector3.Cross(edge.VertexA, edge.VertexB), tileCenterW - edgeCenterW) <
                    0f)
                {
                    (randomsEdge[0], randomsEdge[1]) = (randomsEdge[1], randomsEdge[0]);
                    (randomsEdge[2], randomsEdge[3]) = (randomsEdge[3], randomsEdge[2]);
                }

                var prevCanal = IsCanal(prevEdge);
                var thisCanal = IsCanal(edge);
                var nextCanal = IsCanal(nextEdge);

                // Water things (lower)
                var (lowerLeftW, lowerRightW) = GetLowerPointsToCenter(edgeLeftW, edgeRightW, tileCenterW, offsets,
                    iCurr, iNext, pctVertTileWater, true, waterHeight);

                var onCenterLineW = GetPointOnCenterLine(edgeCenterW, tileCenterW, offsets, iCurr,
                    pctCenterLineTileWater, true, waterHeight);

                var (upperLeftW, upperRightW) = GetUpperPointsToCenter(edgeLeftW, edgeRightW, tileCenterW, offsets,
                    iCurr, iNext, canalInsetWater, true, waterHeight);

                // Land things (upper)
                var (lowerLeftL, lowerRightL) = GetLowerPointsToCenter(edgeLeftL, edgeRightL, tileCenterL, offsets,
                    iCurr, iNext, pctVertTileLand, true, landHeight);

                var onCenterLineL = GetPointOnCenterLine(edgeCenterL, tileCenterL, offsets, iCurr,
                    pctCenterLineTileLand, true, landHeight);

                var (upperLeftL, upperRightL) = GetUpperPointsToCenter(edgeLeftL, edgeRightL, tileCenterL, offsets,
                    iCurr, iNext, canalInsetLand, true, landHeight);

                // Land things UVs
                var normal = tileCenterL.normalized;
                var vL = edgeLeftL - tileCenterL;
                var vR = edgeRightL - tileCenterL;

                var uvLeft = HexagonCoordinates[iNext];
                var uvRight = HexagonCoordinates[iCurr];
                var uvEdgeCenter = (uvLeft + uvRight) * 0.5f;

                // Project 3D displacements directly onto the 2D HexagonCoordinates basis using Barycentric math
                var crossLr = Vector3.Cross(vL, vR);
                var invDotLrNormal = 1f / Vector3.Dot(crossLr, normal);

                var uvLowerLeft = CalculateUVOffset(lowerLeftL);
                var uvLowerRight = CalculateUVOffset(lowerRightL);
                var uvOnCenterLine = CalculateUVOffset(onCenterLineL);
                var uvUpperLeft = CalculateUVOffset(upperLeftL);
                var uvUpperRight = CalculateUVOffset(upperRightL);

                if (thisCanal)
                {
                    var (onEdgeLeftW, onEdgeRightW) =
                        GetPointsOnEdge(edgeLeftW, edgeRightW, edgeCenterW, randomsEdge, pctEdgeCenterVertWater, true);

                    var (onEdgeLeftL, onEdgeRightL) =
                        GetPointsOnEdge(edgeLeftL, edgeRightL, edgeCenterL, randomsEdge, pctEdgeCenterVertLand, true);

                    if (data.IncludeWater)
                    {
                        cg.AddVertex(onEdgeLeftW, uv1,
                            BuildMaterialData(CanalRectangleOrigin, canalTextureTopBeginning, true,
                                tile.RandomValue));

                        cg.AddVertex(lowerLeftW, uv1,
                            BuildMaterialData(CanalRectangleOrigin, canalTextureTopLowerMiddle, true,
                                tile.RandomValue));

                        cg.AddVertex(lowerRightW, uv1,
                            BuildMaterialData(CanalRectangleOrigin, canalTextureBottomLowerMiddle, true,
                                tile.RandomValue));

                        cg.AddVertex(onEdgeRightW, uv1,
                            BuildMaterialData(CanalRectangleOrigin, canalTextureBottomBeginning, true,
                                tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 3 });
                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 2, currentIdx + 3 });

                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, startIdx, currentIdx + 2 });
                        currentIdx += 4;
                    }

                    if (data.IncludeLand)
                    {
                        var uvOnEdgeLeft = CalculateUVOffset(onEdgeLeftL);
                        var uvOnEdgeRight = CalculateUVOffset(onEdgeRightL);

                        cg.AddVertex(edgeLeftL, uv1,
                            BuildMaterialData(uvCenter, uvLeft, true, tile.RandomValue));

                        cg.AddVertex(lowerLeftL, uv1,
                            BuildMaterialData(uvCenter, uvLowerLeft, true, tile.RandomValue));

                        cg.AddVertex(onEdgeLeftL, uv1,
                            BuildMaterialData(uvCenter, uvOnEdgeLeft, true, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 2 });
                        currentIdx += 3;

                        cg.AddVertex(edgeRightL, uv1,
                            BuildMaterialData(uvCenter, uvRight, true, tile.RandomValue));

                        cg.AddVertex(lowerRightL, uv1,
                            BuildMaterialData(uvCenter, uvLowerRight, true, tile.RandomValue));

                        cg.AddVertex(onEdgeRightL, uv1,
                            BuildMaterialData(uvCenter, uvOnEdgeRight, true, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx + 2, currentIdx + 1, currentIdx });
                        currentIdx += 3;
                    }

                    if (data.IncludeCanalWalls)
                    {
                        cg.AddVertex(onEdgeLeftW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(lowerLeftW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false,
                                tile.RandomValue));

                        cg.AddVertex(onEdgeLeftL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(lowerLeftL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 2, currentIdx + 1 });
                        cg.Triangles.AddRange(new List<int> { currentIdx + 2, currentIdx + 3, currentIdx + 1 });
                        currentIdx += 4;

                        // ---

                        cg.AddVertex(onEdgeRightW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(lowerRightW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(onEdgeRightL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(lowerRightL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 3, currentIdx + 2 });
                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 2, currentIdx + 0 });
                        currentIdx += 4;
                    }

                    if (data.IncludeBorderToNeighbors)
                    {
                        if (tile.Type == Tile.TileType.Water || neighbors[iCurr].Tile.Type != Tile.TileType.Water)
                        {
                            continue;
                        }

                        cg.AddVertex(onEdgeLeftW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(onEdgeLeftL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false,
                                tile.RandomValue));

                        cg.AddVertex(edgeLeftW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(edgeLeftL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 2, currentIdx + 3 });
                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 3, currentIdx + 1 });
                        currentIdx += 4;

                        // ---

                        cg.AddVertex(onEdgeRightW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(onEdgeRightL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(edgeRightW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(edgeRightL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 2 });
                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 3, currentIdx + 2 });
                        currentIdx += 4;
                    }
                }
                else if (prevCanal || nextCanal)
                {
                    if (data.IncludeWater)
                    {
                        // Right part of the triangle
                        if (!onlyLeftSubTriangle)
                        {
                            if (prevCanal)
                            {
                                cg.AddVertex(onCenterLineW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureTopUpperMiddle, true,
                                        tile.RandomValue));

                                cg.AddVertex(lowerRightW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureTopLowerMiddle, true,
                                        tile.RandomValue));
                            }
                            else
                            {
                                cg.AddVertex(onCenterLineW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureBottomUpperMiddle, true,
                                        tile.RandomValue));

                                cg.AddVertex(upperRightW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureBottomEnd, true,
                                        tile.RandomValue));
                            }

                            cg.Triangles.AddRange(new List<int> { currentIdx, startIdx, currentIdx + 1 });
                            currentIdx += 2;
                        }

                        // Left part of the triangle
                        if (!onlyRightSubTriangle)
                        {
                            if (nextCanal)
                            {
                                cg.AddVertex(onCenterLineW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureBottomUpperMiddle, true,
                                        tile.RandomValue));

                                cg.AddVertex(lowerLeftW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureBottomLowerMiddle, true,
                                        tile.RandomValue));
                            }
                            else
                            {
                                cg.AddVertex(onCenterLineW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureTopUpperMiddle, true,
                                        tile.RandomValue));

                                cg.AddVertex(upperLeftW, uv1,
                                    BuildMaterialData(CanalRectangleOrigin, canalTextureTopEnd, true,
                                        tile.RandomValue));
                            }

                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, startIdx });
                            currentIdx += 2;
                        }
                    }

                    if (data.IncludeLand)
                    {
                        cg.AddVertex(edgeCenterL, uv1,
                            BuildMaterialData(uvCenter, uvEdgeCenter, true, tile.RandomValue));

                        cg.AddVertex(onCenterLineL, uv1,
                            BuildMaterialData(uvCenter, uvOnCenterLine, true, tile.RandomValue));

                        // Right part of the triangle
                        if (prevCanal)
                        {
                            cg.AddVertex(lowerRightL, uv1,
                                BuildMaterialData(uvCenter, uvLowerRight, true, tile.RandomValue));
                        }
                        else
                        {
                            cg.AddVertex(upperRightL, uv1,
                                BuildMaterialData(uvCenter, uvUpperRight, true, tile.RandomValue));
                        }

                        cg.AddVertex(edgeRightL, uv1,
                            BuildMaterialData(uvCenter, uvRight, true, tile.RandomValue));

                        if (!onlyLeftSubTriangle)
                        {
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 2 });
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 2, currentIdx + 3 });
                        }

                        // Left part of the triangle
                        if (nextCanal)
                        {
                            cg.AddVertex(lowerLeftL, uv1,
                                BuildMaterialData(uvCenter, uvLowerLeft, true, tile.RandomValue));
                        }
                        else
                        {
                            cg.AddVertex(upperLeftL, uv1,
                                BuildMaterialData(uvCenter, uvUpperLeft, true, tile.RandomValue));
                        }

                        cg.AddVertex(edgeLeftL, uv1,
                            BuildMaterialData(uvCenter, uvLeft, true, tile.RandomValue));

                        if (!onlyRightSubTriangle)
                        {
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 5, currentIdx + 4 });
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 4, currentIdx + 1 });
                        }

                        currentIdx += 6;
                    }

                    if (data.IncludeCanalWalls)
                    {
                        cg.AddVertex(onCenterLineW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(onCenterLineL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        // Right part of the triangle
                        if (prevCanal)
                        {
                            cg.AddVertex(lowerRightW, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                            cg.AddVertex(lowerRightL, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));
                        }
                        else
                        {
                            cg.AddVertex(upperRightW, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                            cg.AddVertex(upperRightL, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));
                        }

                        if (!onlyLeftSubTriangle)
                        {
                            cg.Triangles.AddRange(new List<int> { currentIdx + 2, currentIdx + 3, currentIdx });
                            cg.Triangles.AddRange(new List<int> { currentIdx + 3, currentIdx + 1, currentIdx });
                        }

                        // Left part of the triangle
                        if (nextCanal)
                        {
                            cg.AddVertex(lowerLeftW, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                            cg.AddVertex(lowerLeftL, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));
                        }
                        else
                        {
                            cg.AddVertex(upperLeftW, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                            cg.AddVertex(upperLeftL, uv1,
                                BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));
                        }

                        if (!onlyRightSubTriangle)
                        {
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 4 });
                            cg.Triangles.AddRange(new List<int> { currentIdx + 4, currentIdx + 1, currentIdx + 5 });
                        }

                        currentIdx += 6;
                    }
                }
                else
                {
                    if (data.IncludeWater)
                    {
                        cg.AddVertex(upperLeftW, uv1,
                            BuildMaterialData(CanalTriangleCenter, TriangleCoordinates[0], true, tile.RandomValue));

                        cg.AddVertex(upperRightW, uv1,
                            BuildMaterialData(CanalTriangleCenter, TriangleCoordinates[1], true, tile.RandomValue));

                        cg.AddVertex(tileCenterW, uv1,
                            BuildMaterialData(CanalTriangleCenter, TriangleCoordinates[2], true, tile.RandomValue));

                        if (!(onlyLeftSubTriangle || onlyRightSubTriangle))
                        {
                            cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 2, currentIdx + 1 });
                            currentIdx += 3;
                        }
                        else
                        {
                            cg.AddVertex((upperRightW + upperLeftW) * 0.5f, uv1,
                                BuildMaterialData(CanalTriangleCenter,
                                    (TriangleCoordinates[0] + TriangleCoordinates[1]) * 0.5f, true, tile.RandomValue));

                            cg.Triangles.AddRange(onlyLeftSubTriangle
                                ? new List<int> { currentIdx, currentIdx + 2, currentIdx + 3 }
                                : new List<int> { currentIdx + 1, currentIdx + 3, currentIdx + 2 });

                            currentIdx += 4;
                        }
                    }

                    if (data.IncludeLand)
                    {
                        cg.AddVertex(upperLeftL, uv1,
                            BuildMaterialData(uvCenter, uvUpperLeft, true, tile.RandomValue));

                        cg.AddVertex(upperRightL, uv1,
                            BuildMaterialData(uvCenter, uvUpperRight, true, tile.RandomValue));

                        cg.AddVertex(edgeLeftL, uv1,
                            BuildMaterialData(uvCenter, uvLeft, true, tile.RandomValue));

                        cg.AddVertex(edgeRightL, uv1,
                            BuildMaterialData(uvCenter, uvRight, true, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 2 });
                        cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 3, currentIdx + 2 });
                        currentIdx += 4;
                    }

                    if (data.IncludeCanalWalls)
                    {
                        cg.AddVertex(upperLeftW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(upperLeftL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(upperRightW, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.AddVertex(upperRightL, uv1,
                            BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 2, currentIdx + 3 });
                        cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 3, currentIdx + 1 });
                        currentIdx += 4;
                    }
                }

                if (!thisCanal && data.IncludeBorderToNeighbors)
                {
                    if (tile.Type == Tile.TileType.Water || neighbors[iCurr].Tile.Type != Tile.TileType.Water)
                    {
                        continue;
                    }

                    cg.AddVertex(edgeLeftW, uv1,
                        BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                    cg.AddVertex(edgeLeftL, uv1,
                        BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                    cg.AddVertex(edgeRightW, uv1,
                        BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                    cg.AddVertex(edgeRightL, uv1,
                        BuildMaterialData(uvCenterWall, Vector3.zero, false, tile.RandomValue));

                    cg.Triangles.AddRange(new List<int> { currentIdx, currentIdx + 1, currentIdx + 2 });
                    cg.Triangles.AddRange(new List<int> { currentIdx + 1, currentIdx + 3, currentIdx + 2 });
                    currentIdx += 4;
                }

                continue;

                Vector2 CalculateUVOffset(Vector3 p)
                {
                    var dir = p - tileCenterL;
                    var a = Vector3.Dot(Vector3.Cross(dir, vR), normal) * invDotLrNormal;
                    var b = Vector3.Dot(Vector3.Cross(vL, dir), normal) * invDotLrNormal;
                    return a * uvLeft + b * uvRight;
                }
            }

            return currentIdx - startIdx;

            bool IsCanal(Edge e) =>
                e != null && ((!data.IsBluePrint && e.Type == Edge.EdgeType.Canal) ||
                              (data.IsBluePrint && e.BlueprintType == Edge.EdgeType.Canal));
        }

        private static Vector4 BuildMaterialData(Vector2 center, Vector2 offset, bool animate, float randomValue)
        {
            var uv = (center + offset) * InvTextureSize;
            return new Vector4(uv.x, uv.y, animate ? 1 : 0, randomValue);
        }

        private static void AddVertex(this MapChunk.ChunkGeometry cg, Vector3 vertex, Vector4 tileData,
            Vector4 materialData)
        {
            cg.Vertices.Add(vertex);
            cg.TileData.Add(tileData);
            cg.MaterialData.Add(materialData);
        }

        private static (Vector3 left, Vector3 right, Vector3 center) GetPoints(Tile.NeighborTile nt, float height)
        {
            var left = nt.LeftVertex.normalized * height;
            var right = nt.RightVertex.normalized * height;
            var center = (left + right) / 2;

            return (left, right, center);

            /*var dir = Vector3.Cross(left, right);
            if (Vector3.Dot(dir, nt.Tile.PositionOnSphere - center) < 0f)
            {
                (left, right) = (right, left);
            }*/
        }

        private static (Vector3 left, Vector3 right) GetPointsOnEdge(Vector3 edgeLeft, Vector3 edgeRight,
            Vector3 edgeCenter, List<float> randoms, float percentageAlongEdge, bool water)
        {
            var dirLeft = edgeLeft - edgeCenter;
            var dirRight = edgeRight - edgeCenter;

            var idxR1 = water ? 0 : 2;
            var idxR2 = water ? 1 : 3;
            var left = edgeCenter + dirLeft * percentageAlongEdge + dirLeft.normalized * randoms[idxR1];
            var right = edgeCenter + dirRight * percentageAlongEdge + dirRight.normalized * randoms[idxR2];

            return (left, right);
        }

        private static (Vector3 left, Vector3 right) GetLowerPointsToCenter(Vector3 edgeLeft, Vector3 edgeRight,
            Vector3 tileCenter, List<Vector3> offsets, int i, int iNext, float pctVertexCenter, bool water,
            float height)
        {
            var offset = offsets.Count / 2;

            var idxR2 = water ? (i + 0) * 3 + 0 : offset + (i + 0) * 3 + 0;
            var idxR1 = water ? (iNext) * 3 + 0 : offset + (i + 1) * 3 + 0;

            var left = (edgeLeft + (tileCenter - edgeLeft) * pctVertexCenter + offsets[idxR1]).normalized * height;
            var right = (edgeRight + (tileCenter - edgeRight) * pctVertexCenter + offsets[idxR2]).normalized * height;

            return (left, right);
        }

        private static (Vector3 left, Vector3 right) GetUpperPointsToCenter(Vector3 edgeLeft, Vector3 edgeRight,
            Vector3 tileCenter, List<Vector3> offsets, int i, int iNext, float inset, bool water, float height)
        {
            var offset = offsets.Count / 2;

            var idxR2 = water ? (i + 0) * 3 + 1 : offset + (i + 0) * 3 + 1;
            var idxR1 = water ? (iNext) * 3 + 1 : offset + (i + 1) * 3 + 1;

            var left = (edgeLeft + (tileCenter - edgeLeft) * inset + offsets[idxR1]).normalized * height;
            var right = (edgeRight + (tileCenter - edgeRight) * inset + offsets[idxR2]).normalized * height;

            return (left, right);
        }

        private static Vector3 GetPointOnCenterLine(Vector3 edgeCenter, Vector3 tileCenter, List<Vector3> offsets,
            int i, float pctAlongCenterLine, bool water, float height)
        {
            var offset = offsets.Count / 2;
            var idxR1 = water ? (i + 0) * 3 + 2 : offset + (i + 0) * 3 + 2;

            return (edgeCenter + (tileCenter - edgeCenter) * pctAlongCenterLine + offsets[idxR1]).normalized * height;
        }

        private static List<T> GetRandoms<T>(int seed, int count, Func<System.Random, T> generator)
        {
            var list = new List<T>(count);
            var random = new System.Random(seed);

            for (var i = 0; i < count; i++)
            {
                list.Add(generator(random));
            }

            return list;
        }

        private static float GetRandomValue(System.Random random)
        {
            return ((float)random.NextDouble() - 0.5f) * canalRandomStrength;
        }

        private static Vector3 GetRandomOffset(System.Random random)
        {
            float x = 0f, y = 0f, z = 0f;
            var sqrMag = 0f;

            const int maxIterations = 20;
            var iterations = 0;
            do
            {
                x = (float)random.NextDouble() * 2f - 1f;
                y = (float)random.NextDouble() * 2f - 1f;
                z = (float)random.NextDouble() * 2f - 1f;
                sqrMag = x * x + y * y + z * z;

                iterations++;
            } while (sqrMag is > 1f or 0f && iterations < maxIterations);

            if (sqrMag > 1f)
            {
                var invMag = 1f / (float)Math.Sqrt(sqrMag);
                x *= invMag;
                y *= invMag;
                z *= invMag;
            }

            const float compensationFactor = 0.456f;
            var scale = canalRandomStrength * compensationFactor;
            return new Vector3(x * scale, y * scale, z * scale);
        }
    }
}