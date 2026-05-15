using System;
using System.Collections.Generic;
using GeometryGeneration.Projections;
using Inputs;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map
{
    public class Map : MonoBehaviour, IMap
    {
        private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
        private static readonly int ProjectionFactor = Shader.PropertyToID("_ProjectionFactor");
        private static readonly int ProjectionCenter = Shader.PropertyToID("_ProjectionCenter");

        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private float hexSize = 0.95f;
        [SerializeField] private int numberOfChunks = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;

        public static Map Instance { get; private set; } = null!;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Instance = null!;
        }
#endif

        public IReadOnlyList<Tile> Tiles => tiles;

        public float Radius => radius;

        public int Resolution => resolution;

        public float HexSize => hexSize;

        private List<Tile> tiles;

        private List<MapChunk> chunks;

        private float oldProjectionFactor;
        private Vector3 oldProjectionCenter;

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log("Starting Map Generation");
            tiles = HexagonalSphere.GenerateHexagonalSphere(radius, resolution);
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
            var projectionCenter = (PlanetCameraController.Instance.transform.position - transform.position).normalized;
            var currentDistance = PlanetCameraController.Instance.CurrentDistance;
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

        public Tile GetCurrentlyHoveredTile()
        {
            return tiles[0];
        }

        public List<Tile> ActiveTiles { get; set; }
    }
}