using System.Collections.Generic;
using UnityEngine;

namespace Map.GeometryGeneration.Edges
{
    public static class EdgeGeometryFactory
    {
        private const float EDGE_WIDTH = 0.01f;
        private const float EDGE_HEIGHT = 0.005f;

        public static Edge.PartialEdgeGeometry GenerateEdgeGeometry(Tile tile, Edge edge)
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
    }
}