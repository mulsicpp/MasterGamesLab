using System;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class GeometriesManager : MonoBehaviour
    {
        public enum GeometryType
        {
            Truck,
            Freighter,
            Producer,
            Consumer,
            Port,
        }

        private const float SCALE = 0.015f;
        public static GeometriesManager Instance { get; private set; }

        public Mesh truckMesh;
        public Mesh freighterMesh;
        public Mesh producerMesh;
        public Mesh consumerMesh;
        public Mesh portMesh;

        public GameObject geometryPrefab;

        private void Awake()
        {
            Instance = this;
        }

        public GameObject GetGameObject(GeometryType type, int id)
        {
            Mesh mesh;
            var defaultLayerName = "Default";
            string outlineLayerName;
            string outlineTransparentLayerName;

            switch (type)
            {
                case GeometryType.Truck:
                    mesh = truckMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    break;
                case GeometryType.Freighter:
                    mesh = freighterMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    break;
                case GeometryType.Producer:
                    mesh = producerMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    break;
                case GeometryType.Consumer:
                    mesh = consumerMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    break;
                case GeometryType.Port:
                    mesh = portMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var gO = Instantiate(geometryPrefab, transform);
            gO.transform.localScale = new Vector3(SCALE, SCALE, SCALE);
            var fixedGeometry = gO.GetComponent<ObjectWithFixedGeometry>();
            fixedGeometry.Init(mesh, defaultLayerName, outlineLayerName, outlineTransparentLayerName, id);
            return gO;
        }
    }
}