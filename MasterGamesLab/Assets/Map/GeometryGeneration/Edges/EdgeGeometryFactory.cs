using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public static class EdgeGeometryFactory
    {
        private const float EDGE_WIDTH = 0.01f;
        private const float EDGE_HEIGHT = 0.005f;

        private const float EDGE_HANDLE_DISTANCE = 0.025f;
        private const float ROAD_HEIGHT = 0.01f;
        private const int EDGE_RESOLUTION = 5;
        private const float ROAD_RADIUS = 0.01f;
        private const float FULL_ROAD_RADIUS = ROAD_RADIUS * 0.9f;
        
        private const float FASTEST_ROAD_NORMAL_DELTA = 0.0015f;
        private const float CHEAPEST_ROAD_NORMAL_DELTA = 0.001f;

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
                return info.AmountOfRoads <= 2
                    ? BuildParametricCurve(tile, edge, blueprint)
                    : BuildConnectingParametricCurves(tile, edge, blueprint);
            }

            if (edge.Type == Edge.EdgeType.Canal || edge.BlueprintType == Edge.EdgeType.Canal)
            {
                return BuildBlueprintCanal(tile, edge, info, blueprint);
            }

            return Edge.PartialEdgeGeometry.Empty;
            // return BuildBackup(tile, edge);
        }

        public static Edge.PartialEdgeGeometry BuildParametricCurve(Tile tile, Edge edge, bool blueprint)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv1 = new List<Vector4>();
            var data = edge.GetEdgeData();

            var otherFound = false;
            Edge other = null;
            foreach (var e in tile.Edges)
            {
                if (!((blueprint && e.BlueprintType == edge.BlueprintType) ||
                      (!blueprint && e.Type == edge.Type)))
                {
                    continue;
                }

                if (e.Id != edge.Id)
                {
                    other = e;
                    otherFound = true;
                }
            }

            var curve = otherFound
                ? ParametricCurve.FromEdgeToEdge(edge, other, tile)
                : ParametricCurve.FromEdgeToTileCenter(edge, tile);

            var factor = otherFound ? 0.5f : 1f;

            for (var i = 0; i < EDGE_RESOLUTION; i++)
            {
                var t = (float)i / (EDGE_RESOLUTION - 1) * factor;

                var (p, normal) = GetPosAndNormal(curve, t);

                var leftPoint = p + normal * ROAD_RADIUS;
                var rightPoint = p - normal * ROAD_RADIUS;

                vertices.Add(leftPoint);
                vertices.Add(rightPoint);

                uv1.Add(data);
                uv1.Add(data);

                if (i == 0)
                {
                    continue;
                }

                var i2 = i * 2;
                triangles.Add(i2 - 2);
                triangles.Add(i2);
                triangles.Add(i2 - 1);

                triangles.Add(i2 - 1);
                triangles.Add(i2);
                triangles.Add(i2 + 1);
            }

            return new Edge.PartialEdgeGeometry
            {
                Vertices = vertices,
                UV1 = uv1,
                Triangles = triangles
            };
        }

        public static Edge.PartialEdgeGeometry BuildConnectingParametricCurves(Tile tile, Edge edge, bool blueprint)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv1 = new List<Vector4>();
            var data = edge.GetEdgeData();

            var validEdges = new List<Edge>(6);
            var selfIdx = -1;
            foreach (var e in tile.Edges)
            {
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
                        var curve = ParametricCurve.FromEdgeToEdge(e1, e2, tile);
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
            var curveToPrevious = ParametricCurve.FromEdgeToEdge(edge, validEdges[prevIdx], tile);
            var curveToNext = ParametricCurve.FromEdgeToEdge(edge, validEdges[nextIdx], tile);

            for (var i = 0; i < EDGE_RESOLUTION; i++)
            {
                var t = (float)i / (EDGE_RESOLUTION - 1) * 0.5f;

                var (pToPrev, normalToPrev) = GetPosAndNormal(curveToPrevious, t);
                var (pToNext, normalToNext) = GetPosAndNormal(curveToNext, t);


                var leftPoint = pToNext + normalToNext * ROAD_RADIUS;
                var rightPoint = pToPrev - normalToPrev * ROAD_RADIUS;

                vertices.Add(leftPoint);
                vertices.Add(rightPoint);

                uv1.Add(data);
                uv1.Add(data);

                if (i == 0)
                {
                    continue;
                }

                var i2 = i * 2;
                triangles.Add(i2 - 2);
                triangles.Add(i2);
                triangles.Add(i2 - 1);

                triangles.Add(i2 - 1);
                triangles.Add(i2);
                triangles.Add(i2 + 1);
            }

            vertices.Add(center);
            uv1.Add(data);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 2);

            return new Edge.PartialEdgeGeometry
            {
                Vertices = vertices,
                UV1 = uv1,
                Triangles = triangles
            };
        }

        public static (Vector3, Vector3) GetPosAndNormal(ParametricCurve curve, float t)
        {
            var p = curve.Evaluate(t);
            var vel = curve.Derivative(t).normalized;
            var normal = p.normalized;
            var binormal = Vector3.Cross(vel, normal).normalized;

            return (p, binormal);
        }

        public static Edge.PartialEdgeGeometry BuildBackup(Tile tile, Edge edge)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv1 = new List<Vector4>();
            var data = edge.GetEdgeData();

            Vector3 a = tile.PositionOnSphere;

            // Treat the edge vertices as arbitrary points A and B
            Vector3 vertexA = edge.VertexA; // Rename to VertexA in your Edge class when ready
            Vector3 vertexB = edge.VertexB; // Rename to VertexB in your Edge class when ready

            // Calculate outward direction
            Vector3 b = (vertexA + vertexB) / 2f;
            Vector3 dir = (b - a).normalized;
            Vector3 up = a.normalized;

            // In a left-handed system, Cross(Up, Forward) yields Right
            Vector3 rightDir = Vector3.Cross(up, dir).normalized;

            // Dynamically assign true Left and Right by checking their position against the Right vector
            Vector3 l, r;
            if (Vector3.Dot(vertexA - a, rightDir) > 0f)
            {
                // vertexA is on the positive side of the Right vector -> It is the Right Vertex
                r = vertexA;
                l = vertexB;
            }
            else
            {
                // vertexA is on the negative side -> It is the Left Vertex
                l = vertexA;
                r = vertexB;
            }

            // 'side' remains our right-facing vector for width calculations
            Vector3 side = rightDir;

            float w = EDGE_WIDTH;
            float h = EDGE_HEIGHT;

            Vector3 vL = (l - a).normalized;
            Vector3 vR = (r - a).normalized;

            // Distance from A along vL/vR to intersect the parallel side lines of the road
            // dist = (w/2) / sin(angle between vL and dir)
            float sinL = Vector3.Cross(vL, dir).magnitude;
            float sinR = Vector3.Cross(vR, dir).magnitude;

            float sL = (sinL > 0.001f) ? (w / 2f) / sinL : 0f;
            float sR = (sinR > 0.001f) ? (w / 2f) / sinR : 0f;

            Vector3 pLBot = a + vL * sL;
            Vector3 pRBot = a + vR * sR;

            // Since 'side' points Right, subtract to go Left, add to go Right
            Vector3 bLBot = b - side * (w / 2f);
            Vector3 bRBot = b + side * (w / 2f);

            Vector3 upOffset = up * h;

            // Vertices
            // 0, 1: Center A bottom, top
            vertices.Add(a);
            uv1.Add(data);
            vertices.Add(a + upOffset);
            uv1.Add(data);

            // 2, 3: Taper Point Left bottom, top
            vertices.Add(pLBot);
            uv1.Add(data);
            vertices.Add(pLBot + upOffset);
            uv1.Add(data);

            // 4, 5: Taper Point Right bottom, top
            vertices.Add(pRBot);
            uv1.Add(data);
            vertices.Add(pRBot + upOffset);
            uv1.Add(data);

            // 6, 7: Edge Point Left bottom, top
            vertices.Add(bLBot);
            uv1.Add(data);
            vertices.Add(bLBot + upOffset);
            uv1.Add(data);

            // 8, 9: Edge Point Right bottom, top
            vertices.Add(bRBot);
            uv1.Add(data);
            vertices.Add(bRBot + upOffset);
            uv1.Add(data);

            // Triangles (CW winding for top)
            // Top Face (With corrected CW winding from earlier)
            // Wedge: A_top(1), P_L_top(3), P_R_top(5)
            triangles.Add(1);
            triangles.Add(3);
            triangles.Add(5);
            // Rectangle: P_L_top(3), B_R_top(9), P_R_top(5)
            triangles.Add(3);
            triangles.Add(9);
            triangles.Add(5);
            // Rectangle: P_L_top(3), B_L_top(7), B_R_top(9)
            triangles.Add(3);
            triangles.Add(7);
            triangles.Add(9);

            // Side Faces
            // Left Taper: A(0), P_L_top(3), A_top(1)
            triangles.Add(0);
            triangles.Add(3);
            triangles.Add(1);
            // Left Taper: A(0), P_L_bot(2), P_L_top(3)
            triangles.Add(0);
            triangles.Add(2);
            triangles.Add(3);

            // Right Taper: A(0), A_top(1), P_R_top(5)
            triangles.Add(0);
            triangles.Add(1);
            triangles.Add(5);
            // Right Taper: A(0), P_R_top(5), P_R_bot(4)
            triangles.Add(0);
            triangles.Add(5);
            triangles.Add(4);

            // Left Straight: P_L_bot(2), B_L_top(7), P_L_top(3)
            triangles.Add(2);
            triangles.Add(7);
            triangles.Add(3);
            // Left Straight: P_L_bot(2), B_L_bot(6), B_L_top(7)
            triangles.Add(2);
            triangles.Add(6);
            triangles.Add(7);

            // Right Straight: P_R_bot(4), P_R_top(5), B_R_top(9)
            triangles.Add(4);
            triangles.Add(5);
            triangles.Add(9);
            // Right Straight: P_R_bot(4), B_R_top(9), B_R_bot(8)
            triangles.Add(4);
            triangles.Add(9);
            triangles.Add(8);

            // Edge End Face
            // B_L_bot(6), B_R_top(9), B_L_top(7)
            triangles.Add(6);
            triangles.Add(9);
            triangles.Add(7);
            // B_L_bot(6), B_R_bot(8), B_R_top(9)
            triangles.Add(6);
            triangles.Add(8);
            triangles.Add(9);

            return new Edge.PartialEdgeGeometry
            {
                Vertices = vertices,
                UV1 = uv1,
                Triangles = triangles
            };
        }

        public static Edge.PartialEdgeGeometry BuildBlueprintCanal(Tile tile, Edge edge, TileInformation info,
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

        public static RouteGeometry GenerateRoute(TileId[] tiles, RouteGeometry.RouteType type)
        {
            var go = GeometriesManager.Instance.GetRouteGameObject();

            Debug.Log(go);
            var routeGeometry = go.GetComponent<RouteGeometry>();
            routeGeometry.Init(type);
            routeGeometry.ClearMeshData();

            var uv1 = new Vector4(routeGeometry.EntityId.Value + Map.ID_OFFSET, 0, 0, 0);

            if (tiles.Length <= 1)
            {
                return routeGeometry;
            }


            var startCurve = ParametricCurve.FromTileToTileCenter(Map.Instance.Tiles[tiles[1]] as Tile,
                Map.Instance.Tiles[tiles[0]] as Tile);
            AddCurveData(startCurve, routeGeometry, uv1, type);

            for (var i = 1; i < tiles.Length - 1; i++)
            {
                var curve = ParametricCurve.FromTileToTileOverTile(Map.Instance.Tiles[tiles[i - 1]] as Tile,
                    Map.Instance.Tiles[tiles[i + 1]] as Tile, Map.Instance.Tiles[tiles[i]] as Tile);
                AddCurveData(curve, routeGeometry, uv1, type);
            }

            var endCurve = ParametricCurve.FromTileToTileCenter(Map.Instance.Tiles[tiles[^2]] as Tile,
                Map.Instance.Tiles[tiles[^1]] as Tile);
            AddCurveData(endCurve, routeGeometry, uv1, type);

            routeGeometry.StoreMeshData();
            routeGeometry.ClearOutline();
            return routeGeometry;
        }

        private static void AddCurveData(ParametricCurve curve, RouteGeometry element, Vector4 uv1, RouteGeometry.RouteType type)
        {
            var vertexOffset = element.Vertices.Count;
            var heightOffset = type == RouteGeometry.RouteType.Fastest
                ? FASTEST_ROAD_NORMAL_DELTA
                : CHEAPEST_ROAD_NORMAL_DELTA;

            for (var i = 0; i < EDGE_RESOLUTION; i++)
            {
                var t = (float)i / (EDGE_RESOLUTION - 1);

                var (p, normal) = GetPosAndNormal(curve, t);
                p += p.normalized * heightOffset;

                var leftPoint = p + normal * FULL_ROAD_RADIUS;
                var rightPoint = p - normal * FULL_ROAD_RADIUS;

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
    }
}