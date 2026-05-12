using System.Collections.Generic;
using Map.GeometryGeneration;
using UnityEngine;

namespace Map
{
    public class Map : MonoBehaviour, IMap
    {
        [SerializeField] private float radius = 1;
        [SerializeField] private int resolution = 20;
        [SerializeField] private float hexSize = 0.95f;
        [SerializeField] private int numberOfChunks = 20;
        [SerializeField] private GameObject chunkPrefab;

        [SerializeField] private float fullSphereDistance = 2;
        [SerializeField] private float fullProjectionDistance = 1.5f;
        [SerializeField] private PlanetCameraController planetCamera;

        private List<Tile> tiles;
        private List<MapChunk> chunks;

        public IReadOnlyList<Tile> Tiles => tiles;

        public float Radius => radius;

        public int Resolution => resolution;

        public float HexSize => hexSize;

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

            foreach (var chunk in chunks)
            {
                chunk.UpdateMesh();
            }
            Debug.Log("Finished Map Generation");
        }

        public Tile GetCurrentlyHoveredTile()
        {
            return tiles[0];
        }

        public List<Tile> ActiveTiles { get; set; }
    }
}