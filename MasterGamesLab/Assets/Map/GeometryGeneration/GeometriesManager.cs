using Player;
using System;
using Map.GeometryGeneration.Edges;
using UnityEditorInternal;
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

        private const float SCALE_VALUE = 0.015f;

        public static Vector3 Scale => new Vector3(SCALE_VALUE, SCALE_VALUE, SCALE_VALUE);
        public static GeometriesManager Instance { get; private set; }

        [SerializeField] private Mesh truckMesh;
        [SerializeField] private Mesh freighterMesh;
        [SerializeField] private Mesh producerMesh;
        [SerializeField] private Mesh consumerMesh;
        [SerializeField] private Mesh portMesh;
        [SerializeField] private GameObject geometryPrefab;
        [SerializeField] private GameObject fullRoadPrefab;

        private void Awake()
        {
            Instance = this;
        }

        public ObjectWithFixedGeometry GetGameObjectGeometry(GeometryType type, int id, Transform parent,
            Player.Player owner = null)
        {
            Mesh mesh;
            var defaultLayerName = "Default";
            string outlineLayerName;
            string outlineTransparentLayerName;
            Quaternion localRotation;
            var localPosition = Vector3.zero;

            switch (type)
            {
                case GeometryType.Truck:
                    mesh = truckMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    break;
                case GeometryType.Freighter:
                    mesh = freighterMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                    break;
                case GeometryType.Producer:
                    mesh = producerMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Consumer:
                    mesh = consumerMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Port:
                    mesh = portMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            parent.localScale = Scale;
            var gO = Instantiate(geometryPrefab, parent);
            gO.transform.localPosition = localPosition;
            gO.transform.localRotation = localRotation;
            var fixedGeometry = gO.GetComponent<ObjectWithFixedGeometry>();
            fixedGeometry.Init(mesh, defaultLayerName, outlineLayerName, outlineTransparentLayerName, id);
            return fixedGeometry;
        }

        public GameObject GetFullRoadGameObject() => Instantiate(fullRoadPrefab, transform);
    }
}