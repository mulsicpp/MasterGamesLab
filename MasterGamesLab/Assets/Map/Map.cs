using System.Collections.Generic;
using InGameCamera;
using Map.GeometryGeneration;
using UnityEngine;
using UnityEngine.Rendering;

namespace Map
{
    public class Map : MonoBehaviour, IMap
    {
        public const int ID_OFFSET = 1;
        private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        private static readonly int ProjectionFactor = Shader.PropertyToID("_ProjectionFactor");
        private static readonly int ProjectionCenter = Shader.PropertyToID("_ProjectionCenter");

        public static Map Instance { get; private set; } = null!;

        public List<Tile> ActiveTiles
        {
            get => activeTiles;
            set
            {
                foreach (var tile in activeTiles)
                {
                    tile.Active = false;
                }

                activeTiles = value;
                foreach (var tile in activeTiles)
                {
                    tile.Active = true;
                }
            }
        }

        public IReadOnlyList<Tile> Tiles => tiles;
        public float Radius => radius;
        public int Resolution => resolution;
        public float HexSize => hexSize;

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private float hexSize = 0.95f;
        [SerializeField] private int numberOfChunks = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        private List<Tile> tiles;
        private List<Tile> activeTiles;
        private List<MapChunk> chunks;
        private float oldProjectionFactor;
        private Vector3 oldProjectionCenter;
        private int currentlyHoveredTileId;

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            currentlyHoveredTileId = -1;
            Debug.Log("Starting Map Generation");
            tiles = HexagonalSphere.GenerateHexagonalSphere(radius, resolution);
            activeTiles = new List<Tile>();
            Debug.Log("Starting Chunk Generation");

            chunks = new List<MapChunk>(numberOfChunks);
            var numPerChunk = Mathf.CeilToInt((float)tiles.Count / numberOfChunks);

            for (var i = 0; i < numberOfChunks; i++)
            {
                var chunkGameObject = Instantiate(chunkPrefab, transform);
                var chunk = chunkGameObject.GetComponent<MapChunk>();
                chunk.Init(this, i * numPerChunk, Mathf.Min(i * numPerChunk + numPerChunk, tiles.Count));
                chunks.Add(chunk);
            }

            Shader.SetGlobalFloat(PlanetRadius, radius);

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }

            Debug.Log("Finished Map Generation");
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
                    chunk.UpdateTileData();
                }
            }

            // Update the currently hovered tile
            MainCamera.Instance.RequestCurrentlyHoveredTile(OnReadbackComplete);
            // Update the projection
            UpdateProjectionUniforms();
        }

        public Tile GetCurrentlyHoveredTile()
        {
            return currentlyHoveredTileId == -1 ? null : tiles[currentlyHoveredTileId];
        }

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

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Instance = null!;
        }
#endif
    }
}