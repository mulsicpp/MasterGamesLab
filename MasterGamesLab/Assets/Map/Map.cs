using System;
using System.Collections.Generic;
using InGameCamera;
using Map.GeometryGeneration;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Map
{
    public class Map : NetworkBehaviour, IMap
    {
        public const int ID_OFFSET = 1;
        private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        private static readonly int ProjectionFactor = Shader.PropertyToID("_ProjectionFactor");
        private static readonly int ProjectionCenter = Shader.PropertyToID("_ProjectionCenter");

        public static Map Instance { get; private set; } = null!;

        public IReadOnlyList<ITile> Tiles => tiles;
        public IReadOnlyList<ITile> ActiveTiles => activeTiles;
        public float Radius => radius;
        public int Resolution => resolution;

        public IReadOnlyList<Edge> Edges => edges;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        public struct TreeData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float Scale;
            public float Yaw;
            public float Random;
        }

        private List<Tile> tiles;
        private List<Tile> activeTiles;
        private List<MapChunk> chunks;
        private float oldProjectionFactor;
        private Vector3 oldProjectionCenter;
        private int currentlyHoveredTileId;

        private Edge[] edges;

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            currentlyHoveredTileId = -1;
            Debug.Log("Starting Map Generation");
            var (chunksPoints, numPoints) = HexagonalSphere.GenerateIcoSphereChunks(radius, resolution);
            tiles = new List<Tile>(numPoints);
            chunks = new List<MapChunk>(chunksPoints.Count);
            edges = Array.Empty<Edge>();

            var currentId = 0;
            foreach (var chunkPoints in chunksPoints)
            {
                var chunkGameObject = Instantiate(chunkPrefab, transform);
                var chunk = chunkGameObject.GetComponent<MapChunk>();
                var startId = currentId;

                foreach (var point in chunkPoints)
                {
                    point.InitializeTile(currentId++, radius, chunk);
                    tiles.Add(point);
                }

                chunk.Init(this, startId, currentId);
                // chunk.UpdateMesh();
                chunks.Add(chunk);
            }

            /*foreach (var tile in tiles)
            {
                tile.InitializeNeighbors();
            }*/

            ProceduralMapGenerator.GenerateMap();

            foreach (var tile in tiles)
            {
                var r = UnityEngine.Random.Range(0f, 1f);
                if (r < 0.5)
                {
                    continue;
                }
                else if (r < 0.75)
                {
                    tile.Type = Tile.TileType.Forest;
                }
                else
                {
                    tile.Type = Tile.TileType.Mountain;
                }
            }

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }

            Debug.Log($"Generated {tiles.Count} tiles");

            activeTiles = new List<Tile>();

            Shader.SetGlobalFloat(PlanetRadius, radius);
        }

        private void Update()
        {
            // Update the map chunks
            foreach (var chunk in chunks)
            {
                if (chunk.GeometryChanged)
                {
                    chunk.UpdateMesh();
                }
                else if (chunk.Dirty)
                {
                    //chunk.UpdateTileData();
                    //chunk.UpdateMesh();
                }

                chunk.RenderTrees();
            }

            // Update the currently hovered tile
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log("Click");
                MainCamera.Instance.RequestCurrentlyHoveredTile(OnReadbackComplete);
            }

            // Update the projection
            UpdateProjectionUniforms();
        }


        public ITile GetCurrentlyHoveredTile()
        {
            return currentlyHoveredTileId == -1 ? null : tiles[currentlyHoveredTileId];
        }

        public void AddActiveTile(Tile tile) => activeTiles.Add(tile);
        public void RemoveActiveTile(Tile tile) => activeTiles.Remove(tile);

        private void UpdateProjectionUniforms()
        {
            var projectionCenter = (MainCamera.Instance.CurrentPosition - transform.position).normalized;
            var currentDistance = MainCamera.Instance.CurrentDistance;
            var projectionFactor = (currentDistance - fullSphereDistance) /
                                   (fullProjectionDistance - fullSphereDistance);

            projectionFactor = Mathf.Clamp01(projectionFactor);

            if (oldProjectionCenter == projectionCenter && Mathf.Approximately(oldProjectionFactor, projectionFactor))
            {
                return;
            }

            Shader.SetGlobalVector(ProjectionCenter, projectionCenter);
            Shader.SetGlobalFloat(ProjectionFactor, projectionFactor);

            oldProjectionCenter = projectionCenter;
            oldProjectionFactor = projectionFactor;
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                // Debug.LogError("GPU Readback error.");
                currentlyHoveredTileId = -1;
                return;
            }

            var colorData = request.GetData<Color32>();
            var pixelColor = colorData[0];

            currentlyHoveredTileId = ((pixelColor.r << 16) | (pixelColor.g << 8) | pixelColor.b) - ID_OFFSET;
        }

        public void Generate(int seed)
        {
            Debug.Log("Generating world with seed " + seed + " ...");
            // TODO World generation needs to be implemented!

            foreach (var tile in tiles)
            {
                if (tile.PositionOnSphere.z < -0.97f) tile.Type = Tile.TileType.Mountain;
                else if (tile.PositionOnSphere.z < -0.9f) tile.Type = Tile.TileType.Forest;
                else if (tile.PositionOnSphere.z < -0.7f) tile.Type = Tile.TileType.Plain;
                else tile.Type = Tile.TileType.Water;
            }

            InitEdges();

            // Test edge types

            // for(int i = 0; i < edges.Length; i++)
            // {
            //     if (i < edges.Length / 8) edges[i].Type = Edge.EdgeType.Rail;
            //     else if (i < edges.Length / 6) edges[i].Type = Edge.EdgeType.Canal;
            //     else if(i < edges.Length / 4) edges[i].Type = Edge.EdgeType.Road;
            // }
        }

        private void InitEdges()
        {
            var tempEdges = new List<Edge>();
            for (var i = 0; i < tiles.Count; i++)
            {
                tiles[i].InitializeEdges(tempEdges);
            }

            Debug.Log("Initialized " + tempEdges.Count + " edges");

            edges = tempEdges.ToArray();
        }

        /*public void OnDrawGizmos()
        {
            if (edges == null) return;

            List<Vector3> nonePoints = new List<Vector3>();
            List<Vector3> roadPoints = new List<Vector3>();
            List<Vector3> canalPoints = new List<Vector3>();
            List<Vector3> railPoints = new List<Vector3>();

            for (int i = 0; i < edges.Length; i++)
            {
                List<Vector3> points = nonePoints;
                switch (edges[i].Type)
                {
                    case Edge.EdgeType.Road: points = roadPoints; break;
                    case Edge.EdgeType.Canal: points = canalPoints; break;
                    case Edge.EdgeType.Rail: points = railPoints; break;
                }

                points.Add(edges[i].StartTile.PositionOnSphere * 1.01f);
                points.Add(edges[i].EndTile.PositionOnSphere * 1.01f);
            }

            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            Gizmos.DrawLineList(nonePoints.ToArray().AsSpan());

            Gizmos.color = Color.black;
            Gizmos.DrawLineList(roadPoints.ToArray().AsSpan());

            Gizmos.color = Color.blue;
            Gizmos.DrawLineList(canalPoints.ToArray().AsSpan());

            Gizmos.color = new Color(0.1f, 0.1f, 0.1f);
            Gizmos.DrawLineList(railPoints.ToArray().AsSpan());

            Debug.Log("Drawing gizmos");
        }*/

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Instance = null!;
        }
#endif
    }
}