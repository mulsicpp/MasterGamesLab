using System;
using System.Collections.Generic;
using System.Linq;
using Map.Fleet;
using Map.GeometryGeneration;
using Map.GeometryGeneration.Edges;
using Map.Infrastructure;
using UnityEngine;

namespace Map
{
    public class Tile : ITile
    {
        private const float FLOAT_COMPARISON_DELTA = 1e-5f;

        public enum TileType
        {
            Water,
            Plain,
            Forest,
            Mountain
        }

        public struct NeighborTile
        {
            public Tile Tile;
            public Vector3 LeftVertex;
            public Vector3 RightVertex;
        }

        // Point data
        public Vector3 Position;
        private List<Triangle> neighborTriangles;

        // Tile data
        public TileId Id { get; private set; }
        public MapChunk Chunk;
        public IReadOnlyList<ITile> Neighbors => neighbors;
        public Vector3 PositionOnSphere { get; private set; }

        public int ContinentId { get; set; } = -1;

        public IReadOnlyList<Edge> Edges => edges;

        private Structure structure { get; set; }

        public Structure Structure
        {
            get => structure;
            set
            {
                structure = value;
                GeometryChanged = true;
            }
        }

        private Structure blueprintStructure { get; set; }

        public Structure BlueprintStructure
        {
            get => blueprintStructure;
            set
            {
                blueprintStructure = value;
                GeometryChanged = true;
            }
        }

        public TileType Type
        {
            get => tileType;
            set
            {
                tileType = value;
                Chunk.GeometryChanged = true;
            }
        }

        public bool Active
        {
            get => active;
            set
            {
                active = value;
                if (active)
                {
                    Map.Instance.AddActiveTile(this);
                }
                else
                {
                    Map.Instance.RemoveActiveTile(this);
                }

                Chunk.Dirty = true;
            }
        }

        public readonly float RandomValue;
        public readonly List<NeighborTile> NeighborTiles;

        public bool GeometryChanged
        {
            get => geometryChanged;
            set
            {
                geometryChanged = value;
                EdgesCenter = Vector3.zero;
                EdgesCenterBlueprint = Vector3.zero;
            }
        }

        private bool geometryChanged;
        public Vector3 EdgesCenter;
        public Vector3 EdgesCenterBlueprint;

        // public bool EdgeDirty;
        public bool StructureDirty;

        private readonly List<Tile> neighbors;
        private TileType tileType;
        private bool active;
        private readonly List<Edge> edges;
        private MapChunk.TileGeometryInformation tileGeometryInformation;

        public Tile(Vector3 position)
        {
            Position = position;
            neighborTriangles = new List<Triangle>();

            // Initialize tile data for later
            Id = TileId.NONE;
            Chunk = null;
            neighbors = new List<Tile>(6);
            NeighborTiles = new List<NeighborTile>(6);
            RandomValue = UnityEngine.Random.Range(0f, 1f);

            edges = new List<Edge>();

            Structure = null;
        }

        // Point Functions
        public void AddNeighborTriangle(Triangle triangle)
        {
            neighborTriangles.Add(triangle);
        }

        public bool ApproximatelyEqual(Vector3 other)
        {
            return
                Mathf.Abs(other.x - Position.x) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.y - Position.y) <= FLOAT_COMPARISON_DELTA &&
                Mathf.Abs(other.z - Position.z) <= FLOAT_COMPARISON_DELTA;
        }

        public static Vector3 ProjectToSphere(Vector3 position, float radius)
        {
            return position.normalized * radius;
        }

        // Tile Functions
        public void InitializeTile(TileId id, float sphereRadius, MapChunk chunk)
        {
            Id = id;
            Chunk = chunk;
            PositionOnSphere = ProjectToSphere(Position, sphereRadius);
            tileGeometryInformation = new MapChunk.TileGeometryInformation
            {
                NumVertices = -1,
            };

            if (neighborTriangles.Count == 0)
            {
                throw new Exception($"Tile {Id} at {Position} has no neighbours");
            }

            // Sort the neighbors so that they are in the correct order for the tile faces
            SortList(neighborTriangles, t => t.Center);

            neighbors.Clear();
            NeighborTiles.Clear();

            for (var i = 0; i < neighborTriangles.Count; i++)
            {
                var t1 = neighborTriangles[i];
                var t2 = neighborTriangles[(i + 1) % neighborTriangles.Count];

                Tile commonTile = null;
                foreach (var p1 in t1.Points)
                {
                    if (p1 == this) continue;
                    foreach (var p2 in t2.Points)
                    {
                        if (p1 == p2)
                        {
                            commonTile = p1;
                            break;
                        }
                    }

                    if (commonTile != null) break;
                }

                if (commonTile != null)
                {
                    neighbors.Add(commonTile);
                    NeighborTiles.Add(new NeighborTile
                    {
                        Tile = commonTile,
                        LeftVertex = ProjectToSphere(t1.Center, sphereRadius),
                        RightVertex = ProjectToSphere(t2.Center, sphereRadius)
                    });
                }
            }

            neighborTriangles = null;
        }

        public void ClearEdges()
        {
            edges.Clear();
        }

        public void InitializeEdges(List<Edge> edgeList)
        {
            foreach (var n in neighbors)
            {
                if (n.Id < Id) continue;
                if (n.Type == TileType.Water && Type == TileType.Water) continue;
                if (n.Type == TileType.Mountain || Type == TileType.Mountain) continue;

                var neighborTile = NeighborTiles.First(nt => nt.Tile == n);

                var edge = new Edge(new EdgeId(edgeList.Count), this, n, Edge.EdgeType.None, PlayerId.NONE,
                    neighborTile.LeftVertex, neighborTile.RightVertex);

                edges.Add(edge);
                n.edges.Add(edge);
                edgeList.Add(edge);
            }
        }


        public void SortEdges() => SortList(edges, e => (e.VertexA + e.VertexB) / 2f);

        public int CountEdgesWith(Predicate<Edge> condition)
        {
            var count = 0;
            foreach (var edge in edges)
            {
                if (condition(edge))
                {
                    count++;
                }
            }

            return count;
        }

        public Edge FindEdgeTo(ITile other)
            => edges.FirstOrDefault(edge =>
                (edge.StartTile == this && edge.EndTile == other) || (edge.StartTile == other && edge.EndTile == this));

        public bool CanSpawnStructure(Structure.StructureType type)
        {
            if (Structure != null) return false;

            switch (type)
            {
                case Structure.StructureType.Producer:
                case Structure.StructureType.Consumer:
                case Structure.StructureType.Garage: return Type is TileType.Plain or TileType.Forest;
                case Structure.StructureType.Port:
                    bool buildable = Type is TileType.Plain or TileType.Forest;
                    bool bordersWater = neighbors.Where(t => t.Type is TileType.Water).Count() > 0;
                    return buildable & bordersWater;
                default: return false;
            }

            ;
        }

        public bool CanSpawnVehicle(Vehicle.VehicleType type)
        {
            return type switch
            {
                Vehicle.VehicleType.Truck => Type is TileType.Plain or TileType.Forest,
                Vehicle.VehicleType.Freighter => Type == TileType.Water,
                _ => false
            };
        }

        public bool CanBuild(out float costFactor)
        {
            costFactor = 0f;
            switch (Type)
            {
                case TileType.Plain:
                    costFactor = Constants.PLAIN_BUILD_COST_FACTOR;
                    return true;
                case TileType.Forest:
                    costFactor = Constants.FOREST_BUILD_COST_FACTOR;
                    return true;
                default: return false;
            }
        }

        public void BuildFaces(MapChunk.ChunkGeometry cg)
        {
            tileGeometryInformation = TileGeometryFactory.BuildFaces(this, cg);
        }

        public void FillTileData(List<Vector4> tileDataList, List<Map.TreeData> treeDataList)
        {
            var tileData = GetTileData();
            for (var i = 0; i < tileGeometryInformation.NumVertices; i++)
            {
                tileDataList.Add(tileData);
            }

            for (var i = tileGeometryInformation.StartTreeIdx; i < tileGeometryInformation.EndTreeIdx; i++)
            {
                var tree = treeDataList[i];
                tree.Active = active ? 1 : 0;
                treeDataList[i] = tree;
            }
        }

        public void BuildGeometryData()
        {
            GeometryChanged = false;

            var infoNormal = new EdgeGeometryFactory.TileInformation();
            var infoBlueprint = new EdgeGeometryFactory.TileInformation();
            foreach (var edge in edges)
            {
                switch (edge.Type)
                {
                    case Edge.EdgeType.None:
                        break;
                    case Edge.EdgeType.Road:
                        infoNormal.AmountOfRoads++;
                        break;
                    case Edge.EdgeType.Canal:
                        infoNormal.AmountOfCanals++;
                        break;
                    case Edge.EdgeType.Rail:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (edge.BlueprintType)
                {
                    case Edge.EdgeType.None:
                        break;
                    case Edge.EdgeType.Road:
                        infoBlueprint.AmountOfRoads++;
                        break;
                    case Edge.EdgeType.Canal:
                        infoBlueprint.AmountOfCanals++;
                        break;
                    case Edge.EdgeType.Rail:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var edge in edges)
            {
                if (edge.Type == Edge.EdgeType.None)
                {
                    edge.SetGeometryFrom(Edge.PartialEdgeGeometry.Empty, this);
                }
                else
                {
                    var eg = EdgeGeometryFactory.GenerateEdgeGeometry(this, edge, infoNormal, false);
                    edge.SetGeometryFrom(eg, this);
                }

                if (edge.BlueprintType == Edge.EdgeType.None)
                {
                    edge.SetBluePrintGeometryFrom(Edge.PartialEdgeGeometry.Empty, this);
                }
                else
                {
                    var eg = EdgeGeometryFactory.GenerateEdgeGeometry(this, edge, infoBlueprint, true);
                    edge.SetBluePrintGeometryFrom(eg, this);
                }
                /*else
                {
                    edge.BlueprintType = Edge.EdgeType.Road;
                    var eg = EdgeGeometryFactory.GenerateEdgeGeometry(this, edge);
                    edge.SetBluePrintGeometryFrom(eg, this);
                }*/
            }

            if (Type == TileType.Forest && (infoNormal.AmountOfCanals > 0 || infoNormal.AmountOfRoads > 0))
            {
                Chunk.GeometryChanged = true;
            }
        }

        public Vector4 GetTileData()
        {
            //return new Vector4(Id + Map.ID_OFFSET, randomValue, active ? 1 : 0, 0);
            return new Vector4(Id + Map.ID_OFFSET, (float)Type, active ? 1 : 0, RandomValue);
        }

        private void SortList<T>(List<T> listToSort, Func<T, Vector3> toVector3)
        {
            var normal = Position.normalized;
            var tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.magnitude < 0.001f)
            {
                tangent = Vector3.Cross(normal, Vector3.right);
            }

            tangent.Normalize();
            var bitangent = Vector3.Cross(normal, tangent);

            listToSort.Sort((a, b) => Comparison(toVector3(a), toVector3(b)));
            return;

            int Comparison(Vector3 a, Vector3 b)
            {
                var vA = a - Position;
                var angleA = Mathf.Atan2(Vector3.Dot(vA, bitangent), Vector3.Dot(vA, tangent));
                var vB = b - Position;
                var angleB = Mathf.Atan2(Vector3.Dot(vB, bitangent), Vector3.Dot(vB, tangent));
                return angleA.CompareTo(angleB);
            }
        }
    }
}