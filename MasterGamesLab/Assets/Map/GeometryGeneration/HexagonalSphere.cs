using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public static class HexagonalSphere
    {
        public static (List<List<Tile>>, int) GenerateIcoSphereChunks(float radius, int subdivisionLevel)
        {
            var triangles = GenerateIcosahedronTriangles(radius);
            return GenerateChunks(triangles, subdivisionLevel);
        }

        public static List<MapGenerationTriangle> GenerateIcosahedronTriangles(float radius)
        {
            var h = radius / Mathf.Sqrt(5f);
            var ringR = 2f * radius / Mathf.Sqrt(5f);

            var vertices = new List<Vector3>(12) { new(0f, radius, 0f) }; // North Pole

            for (var i = 0; i < 5; i++) // upper ring
            {
                var angle = i * 2f * Mathf.PI / 5f - Mathf.PI / 5f;
                vertices.Add(new Vector3(ringR * Mathf.Cos(angle), h, ringR * Mathf.Sin(angle)));
            }

            for (var i = 0; i < 5; i++) // lower ring
            {
                var angle = i * 2f * Mathf.PI / 5f;
                vertices.Add(new Vector3(ringR * Mathf.Cos(angle), -h, ringR * Mathf.Sin(angle)));
            }

            vertices.Add(new Vector3(0f, -radius, 0f)); // South Pole

            var faces = new List<MapGenerationTriangle>(20);
            for (var i = 0; i < 5; i++)
            {
                var currentUpper = 1 + i;
                var nextUpper = 1 + (i + 1) % 5;

                var currentLower = 6 + i;
                var nextLower = 6 + (i + 1) % 5;

                // Top cap 
                faces.Add(new MapGenerationTriangle
                {
                    PointA = vertices[0],
                    PointB = vertices[nextUpper],
                    PointC = vertices[currentUpper],
                    IncludePointA = i == 0,
                    IncludePointB = false,
                    IncludePointC = true,
                    IncludeEdgeAb = false,
                    IncludeEdgeBc = true,
                    IncludeEdgeCa = true,
                });
                // Middle ring (upward pointing triangles)
                faces.Add(new MapGenerationTriangle
                {
                    PointA = vertices[currentUpper],
                    PointB = vertices[nextUpper],
                    PointC = vertices[currentLower],
                    IncludePointA = false,
                    IncludePointB = false,
                    IncludePointC = false,
                    IncludeEdgeAb = false,
                    IncludeEdgeBc = false,
                    IncludeEdgeCa = true,
                });
                // Middle ring (downward pointing triangles)
                faces.Add(new MapGenerationTriangle
                {
                    PointA = vertices[currentLower],
                    PointB = vertices[nextUpper],
                    PointC = vertices[nextLower],
                    IncludePointA = false,
                    IncludePointB = false,
                    IncludePointC = false,
                    IncludeEdgeAb = true,
                    IncludeEdgeBc = false,
                    IncludeEdgeCa = false,
                });
                // Bottom cap
                faces.Add(new MapGenerationTriangle
                {
                    PointA = vertices[11],
                    PointB = vertices[currentLower],
                    PointC = vertices[nextLower],
                    IncludePointA = i == 0,
                    IncludePointB = true,
                    IncludePointC = false,
                    IncludeEdgeAb = true,
                    IncludeEdgeBc = true,
                    IncludeEdgeCa = false,
                });
            }

            return faces;
        }

        private static (List<List<Tile>>, int) GenerateChunks(List<MapGenerationTriangle> triangles,
            int subdivisionLevel)
        {
            var chunks = new List<List<Tile>>(triangles.Count);
            var cachedPoints = new List<Tile>();

            foreach (var face in triangles)
            {
                var currentChunk = new List<Tile>((int)((subdivisionLevel + 2) * (subdivisionLevel + 2 + 1) * 0.5));

                var pointA = GetCachedPoint(face.PointA);
                var pointB = GetCachedPoint(face.PointB);
                var pointC = GetCachedPoint(face.PointC);

                if (face.IncludePointA)
                {
                    currentChunk.Add(pointA);
                }

                if (face.IncludePointB)
                {
                    currentChunk.Add(pointB);
                }

                if (face.IncludePointC)
                {
                    currentChunk.Add(pointC);
                }

                var lineAb = SubdivideLine(pointA, pointB, subdivisionLevel,
                    face.IncludeEdgeAb ? currentChunk : null, true);
                var lineAc = SubdivideLine(pointA, pointC, subdivisionLevel,
                    face.IncludeEdgeCa ? currentChunk : null, true);

                var currentConnection = new List<Tile> { pointA };

                for (var i = 1; i <= subdivisionLevel + 1; i++)
                {
                    var lastConnection = currentConnection;
                    currentConnection = SubdivideLine(lineAb[i], lineAc[i], i - 1,
                        i < subdivisionLevel + 1 || face.IncludeEdgeBc ? currentChunk : null,
                        i == subdivisionLevel + 1);

                    for (var j = 0; j < i; j++)
                    {
                        _ = new Triangle(lastConnection[j], currentConnection[j + 1], currentConnection[j]);
                        if (j == 0) continue;

                        _ = new Triangle(lastConnection[j - 1], lastConnection[j], currentConnection[j]);
                    }
                }

                chunks.Add(currentChunk);
            }

            return (chunks, cachedPoints.Count);

            List<Tile> SubdivideLine(Tile start, Tile end, int amount, List<Tile> points = null,
                bool checkCache = false)
            {
                // return SubdivideArc(start, end, amount, points, checkCache, Vector3.zero);
                var newPoints = new List<Tile> { GetCachedPoint(start.Position) };

                for (var i = 1; i <= amount; i++)
                {
                    var factor = (float)i / (amount + 1);
                    var x = start.Position.x * (1 - factor) + end.Position.x * factor;
                    var y = start.Position.y * (1 - factor) + end.Position.y * factor;
                    var z = start.Position.z * (1 - factor) + end.Position.z * factor;

                    var point = checkCache ? GetCachedPoint(new Vector3(x, y, z)) : new Tile(new Vector3(x, y, z));
                    newPoints.Add(point);
                    points?.Add(point);
                }

                newPoints.Add(GetCachedPoint(end.Position));
                return newPoints;
            }

            /*List<Tile> SubdivideArc(Tile start, Tile end, int amount, List<Tile> points = null,
                bool checkCache = false, Vector3 sphereCenter = default)
            {
                var newPoints = new List<Tile> { GetCachedPoint(start.Position) };

                var startRel = start.Position - sphereCenter;
                var endRel = end.Position - sphereCenter;

                for (var i = 1; i <= amount; i++)
                {
                    var factor = (float)i / (amount + 1);

                    var interpolatedRel = Vector3.Slerp(startRel, endRel, factor);
                    var interpolatedPos = interpolatedRel + sphereCenter;

                    var point = checkCache ? GetCachedPoint(interpolatedPos) : new Tile(interpolatedPos);
                    newPoints.Add(point);
                    points?.Add(point);
                }

                newPoints.Add(GetCachedPoint(end.Position));
                return newPoints;
            }*/

            Tile GetCachedPoint(Vector3 position)
            {
                foreach (var storedPoint in cachedPoints)
                {
                    if (storedPoint.ApproximatelyEqual(position))
                    {
                        return storedPoint;
                    }
                }

                var point = new Tile(position);
                cachedPoints.Add(point);
                return point;
            }
        }
    }
}