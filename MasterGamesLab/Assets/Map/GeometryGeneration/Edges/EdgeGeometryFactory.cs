using System;
using System.Collections.Generic;
using Map.Blueprint;
using UI;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public static class EdgeGeometryFactory
    {
        private static float BuoyScale => 0.007f * Map.Instance.TileScale;

        private const int EDGE_RESOLUTION = 5;

        private static float RoadRadius => 0.016f * Map.Instance.TileScale;

        private static float FullRoadRadius => RoadRadius * 0.95f * Map.Instance.TileScale;

        private const float FASTEST_ROAD_NORMAL_DELTA = 0.0013f;
        private const float CHEAPEST_ROAD_NORMAL_DELTA = 0.0012f;
        private const float FASTEST_ROAD_PREVIEW_NORMAL_DELTA = 0.0011f;
        private const float CHEAPEST_ROAD_PREVIEW_NORMAL_DELTA = 0.001f;
        private const float QUEUED_ROAD_NORMAL_DELTA = 0.0008f;
        private const float QUEUED_ROAD_NORMAL_DELTA_PER_INDEX = 0.000005f;
        private const float CURRENT_ROAD_NORMAL_DELTA = 0.0006f;

        public struct ProfileValue
        {
            public float t;
            public float height;
            public float uvXValue;
        }

        private static readonly ProfileValue[] RoadProfile = new[]
        {
            new ProfileValue()
            {
                t = 0,
                height = 0,
                uvXValue = 0,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0,
                uvXValue = 1f,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0.01f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0.125f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 1f,
                height = 0.125f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 1f,
                height = 0f,
                uvXValue = 1.5f,
            },
        };

        private static readonly ProfileValue[] BlueprintRoadProfile = new[]
        {
            new ProfileValue()
            {
                t = 0,
                height = 0,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0.01f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 0.75f,
                height = 0.125f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 1f,
                height = 0.125f,
                uvXValue = 1.5f,
            },
            new ProfileValue()
            {
                t = 1f,
                height = 0f,
                uvXValue = 1.5f,
            },
        };

        public struct TileInformation
        {
            public int AmountOfRoads;
            public int AmountOfCanals;
        }

        public static Edge.PartialEdgeGeometry GenerateEdgeGeometry(Tile tile, Edge edge, TileInformation info, bool
            blueprint)
        {
            if ((!blueprint && edge.Type == Edge.EdgeType.Road) ||
                (blueprint && edge.BlueprintType == Edge.EdgeType.Road))
            {
                return BuildConnectingParametricCurves(tile, edge, blueprint,
                    blueprint ? BlueprintRoadProfile : RoadProfile);
            }

            if (blueprint && edge.BlueprintType == Edge.EdgeType.Canal)
            {
                return BuildBlueprintCanal(tile, edge, info, true);
            }

            if (edge.Type == Edge.EdgeType.Canal)
            {
                return BuildCanalGeometry(tile, edge);
            }

            return Edge.PartialEdgeGeometry.Empty;
        }

        private static Edge.PartialEdgeGeometry BuildConnectingParametricCurves(Tile tile, Edge edge, bool blueprint,
            ProfileValue[] profile)
        {
            var geometry = Edge.PartialEdgeGeometry.Empty;

            var data = edge.GetEdgeData();

            var validEdges = new List<Edge>(6);
            var selfIdx = -1;
            var hasCanal = false;
            foreach (var e in tile.Edges)
            {
                if (e.Type == Edge.EdgeType.Canal)
                {
                    hasCanal = true;
                }

                if (!((blueprint && e.BlueprintType == edge.BlueprintType) ||
                      (!blueprint && e.Type == edge.Type)))
                {
                    continue;
                }

                if (e.Id == edge.Id)
                {
                    selfIdx = validEdges.Count;
                }

                validEdges.Add(e);
            }

            var offset = blueprint ? 0.0015f : 0f;

            var curveData = new ParametricCurve.CurveData(TileGeometryFactory.LAND_HEIGHT + offset,
                TileGeometryFactory.LAND_HEIGHT + offset);

            Vector3 center;
            if (blueprint && tile.EdgesCenterBlueprint != Vector3.zero)
            {
                center = tile.EdgesCenterBlueprint;
            }
            else if (!blueprint && tile.EdgesCenter != Vector3.zero)
            {
                center = tile.EdgesCenter;
            }
            else
            {
                center = Vector3.zero;
                var numAdded = 0;
                for (var i = 0; i < validEdges.Count; i++)
                {
                    for (var j = i + 1; j < validEdges.Count; j++)
                    {
                        var e1 = validEdges[i];
                        var e2 = validEdges[j];
                        var curve = ParametricCurve.FromEdgeToEdge(e1, e2, tile, curveData);
                        var t = curve.Evaluate(0.5f);
                        center += t;
                        numAdded++;
                    }
                }

                center /= numAdded;

                if (blueprint)
                {
                    tile.EdgesCenterBlueprint = center;
                }
                else
                {
                    tile.EdgesCenter = center;
                }
            }

            var nextIdx = (selfIdx + 1) % validEdges.Count;
            var prevIdx = (selfIdx - 1 + validEdges.Count) % validEdges.Count;

            var curveToPrevious =
                validEdges.Count > 1
                    ? ParametricCurve.FromEdgeToEdge(edge, validEdges[prevIdx], tile, curveData)
                    : ParametricCurve.FromEdgeToTileCenter(edge, tile, curveData);
            var curveToNext = validEdges.Count > 1
                ? ParametricCurve.FromEdgeToEdge(edge, validEdges[nextIdx], tile, curveData)
                : ParametricCurve.FromEdgeToTileCenter(edge, tile, curveData);

            var factor = validEdges.Count > 1 ? 0.5f : 1f;
            var includeCenter = validEdges.Count > 2;

            for (var i = 0; i < EDGE_RESOLUTION; i++)
            {
                var t = (float)i / (EDGE_RESOLUTION - 1) * factor;

                var (pToPrev, normalToPrev) = GetPosAndNormal(curveToPrevious, t);
                var (pToNext, normalToNext) = GetPosAndNormal(curveToNext, t);

                data.w = i / (float)(EDGE_RESOLUTION - 1);

                AddProfileValues(pToPrev, -normalToPrev, profile, ref geometry, data, i > 0, false);
                AddProfileValues(pToNext, normalToNext, profile, ref geometry, data, i > 0, true);

                if (includeCenter && i > 0)
                {
                    if (i == 1)
                    {
                        geometry.Triangles.Add(0);
                        geometry.Triangles.Add(3 * profile.Length);
                        geometry.Triangles.Add(2 * profile.Length);
                    }
                    else
                    {
                        var start = (i - 1) * 2 * profile.Length;

                        geometry.Triangles.Add(start);
                        geometry.Triangles.Add(start + profile.Length);
                        geometry.Triangles.Add(start + 2 * profile.Length);

                        geometry.Triangles.Add(start + profile.Length);
                        geometry.Triangles.Add(start + 3 * profile.Length);
                        geometry.Triangles.Add(start + 2 * profile.Length);
                    }
                }
            }

            if (includeCenter)
            {
                geometry.Vertices.Add(center);
                geometry.UV1.Add(data);

                geometry.Triangles.Add(geometry.Vertices.Count - 1);
                geometry.Triangles.Add((EDGE_RESOLUTION - 1) * 2 * profile.Length);
                geometry.Triangles.Add((EDGE_RESOLUTION - 1) * 2 * profile.Length + profile.Length);
            }

            return geometry;
        }

        private static (Vector3, Vector3) GetPosAndNormal(ParametricCurve curve, float t)
        {
            var p = curve.Evaluate(t);
            var vel = curve.Derivative(t).normalized;
            var normal = p.normalized;
            var binormal = Vector3.Cross(vel, normal).normalized;

            return (p, binormal);
        }

        private static Edge.PartialEdgeGeometry BuildBlueprintCanal(Tile tile, Edge edge, TileInformation info,
            bool blueprint)
        {
            var tempGeo = MapChunk.ChunkGeometry.Empty;

            int startIdx, endIdx;
            var includeHalfTriangleBefore = false;
            var includeHalfTriangleAfter = false;
            if (info.AmountOfCanals == 1)
            {
                startIdx = 0;
                endIdx = tile.NeighborTiles.Count;
            }
            else
            {
                var selfIdx = -1;
                var numTiles = tile.NeighborTiles.Count;

                for (var i = 0; i < numTiles; i++)
                {
                    var neighborEdge = tile.FindEdgeTo(tile.NeighborTiles[i].Tile);
                    if (neighborEdge != null && neighborEdge.Id.Value == edge.Id.Value)
                    {
                        selfIdx = i;
                        break;
                    }
                }

                var nextCanalDist = numTiles;
                for (var i = 1; i < numTiles; i++)
                {
                    var nextIdx = (selfIdx + i) % numTiles;
                    var nextEdge = tile.FindEdgeTo(tile.NeighborTiles[nextIdx].Tile);
                    if (nextEdge == null || (blueprint
                            ? nextEdge.BlueprintType != Edge.EdgeType.Canal
                            : nextEdge.Type != Edge.EdgeType.Canal))
                    {
                        continue;
                    }

                    nextCanalDist = i;
                    break;
                }

                var prevCanalDist = numTiles;
                for (var i = 1; i < numTiles; i++)
                {
                    var prevIdx = (selfIdx - i + numTiles) % numTiles;
                    var prevEdge = tile.FindEdgeTo(tile.NeighborTiles[prevIdx].Tile);
                    if (prevEdge == null || (blueprint
                            ? prevEdge.BlueprintType != Edge.EdgeType.Canal
                            : prevEdge.Type != Edge.EdgeType.Canal))
                    {
                        continue;
                    }

                    prevCanalDist = i;
                    break;
                }

                if (nextCanalDist % 2 == 0)
                {
                    nextCanalDist--;
                    includeHalfTriangleAfter = true;
                }

                if (prevCanalDist % 2 == 0)
                {
                    prevCanalDist--;
                    includeHalfTriangleBefore = true;
                }

                startIdx = (selfIdx - prevCanalDist / 2 + numTiles) % numTiles;
                endIdx = (selfIdx + nextCanalDist / 2 + 1) % numTiles;
            }

            TileGeometryFactory.BuildCanalSection(new TileGeometryFactory.CanalSectionData
            {
                Tile = tile,
                StartIdx = startIdx,
                EndIdx = endIdx,
                IncludeHalfTriangleBefore = includeHalfTriangleBefore,
                IncludeHalfTriangleAfter = includeHalfTriangleAfter,
                IsBluePrint = blueprint,
                UV1Data = edge.GetEdgeData(),
                LandTextureCenter = Vector2.zero,
                CanalWallsTextureCenter = Vector2.zero,
                LandHeight = 1,
                WaterHeight = blueprint
                    ? TileGeometryFactory.LAND_HEIGHT + Map.Instance.TEST_ROAD_HEIGHT
                    : TileGeometryFactory.WATER_HEIGHT + 0.001f,
                IncludeWater = true,
                IncludeLand = false,
                IncludeCanalWalls = false,
                IncludeBorderToNeighbors = false,
            }, tempGeo);

            return new Edge.PartialEdgeGeometry
            {
                Vertices = tempGeo.Vertices,
                UV1 = tempGeo.TileData,
                Triangles = tempGeo.Triangles,
            };
        }

        public static RouteGeometry GenerateRoute(TileId[] tileIds, Route.RouteType type, int index,
            ParametricCurve.CurveData? curveData)
        {
            var go = GeometriesManager.Instance.GetRouteGameObject();

            var tiles = new Tile[tileIds.Length];

            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = Map.Instance.Tiles[tileIds[i]] as Tile;
            }

            // Debug.Log(go);
            var routeGeometry = go.GetComponent<RouteGeometry>();
            routeGeometry.Init(type, index);
            routeGeometry.ClearMeshData();

            var uv1 = new Vector4(routeGeometry.EntityId.Value + Map.ID_OFFSET, 0, 0, 0);

            if (tiles.Length <= 1)
            {
                return routeGeometry;
            }

            var heightOffset = type switch
            {
                Route.RouteType.Cheapest => CHEAPEST_ROAD_NORMAL_DELTA,
                Route.RouteType.Queued => QUEUED_ROAD_NORMAL_DELTA + index * QUEUED_ROAD_NORMAL_DELTA_PER_INDEX,
                Route.RouteType.Current => CURRENT_ROAD_NORMAL_DELTA,
                Route.RouteType.CheapestPreview => CHEAPEST_ROAD_PREVIEW_NORMAL_DELTA,
                Route.RouteType.FastestPreview => FASTEST_ROAD_PREVIEW_NORMAL_DELTA,
                _ => FASTEST_ROAD_NORMAL_DELTA,
            };

            var startCurve = ParametricCurve.FromTileToTileCenter(tiles[1], tiles[0], curveData);
            AddCurveData(startCurve, routeGeometry, uv1, type, heightOffset);

            for (var i = 1; i < tiles.Length - 1; i++)
            {
                var curve = ParametricCurve.FromTileToTileOverTile(tiles[i - 1], tiles[i + 1], tiles[i], curveData);
                AddCurveData(curve, routeGeometry, uv1, type, heightOffset);
            }

            var endCurve = ParametricCurve.FromTileToTileCenter(tiles[^2], tiles[^1], curveData);
            AddCurveData(endCurve, routeGeometry, uv1, type, heightOffset);

            routeGeometry.StoreMeshData();
            routeGeometry.ClearOutline();
            return routeGeometry;
        }

        private static void AddCurveData(ParametricCurve curve, RouteGeometry element, Vector4 uv1,
            Route.RouteType type, float heightOffset)
        {
            var vertexOffset = element.Vertices.Count;

            for (var i = 0; i < EDGE_RESOLUTION; i++)
            {
                var t = (float)i / (EDGE_RESOLUTION - 1);

                var (p, normal) = GetPosAndNormal(curve, t);
                p += p.normalized * heightOffset;

                var leftPoint = p + normal * FullRoadRadius;
                var rightPoint = p - normal * FullRoadRadius;

                element.AddVertex(leftPoint, uv1);
                element.AddVertex(rightPoint, uv1);

                if (i == 0)
                {
                    continue;
                }

                var i2 = vertexOffset + i * 2;
                element.AddTriangle(i2 - 2, i2, i2 - 1);
                element.AddTriangle(i2 - 1, i2, i2 + 1);
            }
        }

        private static void AddProfileValues(Vector3 center, Vector3 normal, ProfileValue[] profile,
            ref Edge.PartialEdgeGeometry geometry, Vector4 data, bool generateTriangles, bool flipWindingOrder)
        {
            var offset = profile.Length * 2;
            var startIdx = geometry.Vertices.Count;

            for (var i = 0; i < profile.Length; i++)
            {
                var profileValue = profile[i];
                var horizontalPos = center + normal * (profileValue.t * RoadRadius);
                var pos = horizontalPos + horizontalPos.normalized * (profileValue.height * RoadRadius);

                data.z = profile[i].uvXValue;

                geometry.Vertices.Add(pos);
                geometry.UV1.Add(data);

                if (generateTriangles && i > 0)
                {
                    if (!flipWindingOrder)
                    {
                        geometry.Triangles.Add(startIdx + i - 1 - offset);
                        geometry.Triangles.Add(startIdx + i - 1);
                        geometry.Triangles.Add(startIdx + i);

                        geometry.Triangles.Add(startIdx + i);
                        geometry.Triangles.Add(startIdx + i - offset);
                        geometry.Triangles.Add(startIdx + i - 1 - offset);
                    }
                    else
                    {
                        geometry.Triangles.Add(startIdx + i - 1);
                        geometry.Triangles.Add(startIdx + i - 1 - offset);
                        geometry.Triangles.Add(startIdx + i);

                        geometry.Triangles.Add(startIdx + i - offset);
                        geometry.Triangles.Add(startIdx + i);
                        geometry.Triangles.Add(startIdx + i - 1 - offset);
                    }
                }
            }
        }

        private static Edge.PartialEdgeGeometry BuildCanalGeometry(Tile tile, Edge edge)
        {
            var geometry = Edge.PartialEdgeGeometry.Empty;

            var buoyMesh = GeometriesManager.Instance.GetBuoyMesh();

            var edgePos = (edge.VertexA + edge.VertexB) * 0.5f;
            var normal = ((edgePos + tile.PositionOnSphere) * 0.5f).normalized;
            var pos = normal * TileGeometryFactory.WATER_HEIGHT;

            var data = edge.GetEdgeData();
            data.z = 1.5f;

            AddGeometryAtPosWithNormal(geometry, pos, normal, BuoyScale, buoyMesh, data);

            return geometry;
        }

        private static void AddGeometryAtPosWithNormal(Edge.PartialEdgeGeometry geometry, Vector3 pos, Vector3 normal,
            float scale, Mesh mesh, Vector4 uv1)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var uvs = mesh.uv;
            var startIdx = geometry.Vertices.Count;

            var rotation = Quaternion.FromToRotation(Vector3.up, normal);

            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                v *= scale;
                v = rotation * v;
                v += pos;
                geometry.Vertices.Add(v);

                var uv = uvs[i];
                uv1.z = uv.x;
                uv1.w = uv.y;

                geometry.UV1.Add(uv1);
            }

            foreach (var triangle in triangles)
            {
                geometry.Triangles.Add(startIdx + triangle);
            }
        }
    }
}