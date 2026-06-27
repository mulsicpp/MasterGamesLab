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
            ProducerTetrahedron,
            ProducerCube,
            ProducerOctahedron,
            ProducerIcosahedron,
            ProducerDodecahedron,
            Consumer,
            Port,
            ParkingLot,
            Tetrahedron,
            Cube,
            Octahedron,
            Icosahedron,
            Dodecahedron,
        }

        private const float SCALE_VALUE = 0.008f;

        public static Vector3 Scale => new Vector3(SCALE_VALUE, SCALE_VALUE, SCALE_VALUE);
        public static GeometriesManager Instance { get; private set; }

        [SerializeField] private Mesh truckMesh;
        [SerializeField] private Mesh freighterMesh;

        [SerializeField] private Mesh producerTetrahedronMesh;
        [SerializeField] private Mesh producerCubeMesh;
        [SerializeField] private Mesh producerOctahedronMesh;
        [SerializeField] private Mesh producerIcosahedronMesh;
        [SerializeField] private Mesh producerDodecahedronMesh;

        [SerializeField] private Mesh consumerMesh;
        [SerializeField] private Mesh portMesh;
        [SerializeField] private Mesh parkingLotMesh;
        [SerializeField] private Mesh tetrahedronMesh;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh octahedronMesh;
        [SerializeField] private Mesh icosahedronMesh;
        [SerializeField] private Mesh dodecahedronMesh;
        
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
            Vector3 localScale;
            var localPosition = Vector3.zero;

            if (type is GeometryType.Truck or GeometryType.Freighter)
            {
                localScale = Vector3.one;
            }
            else
            {
                localScale = Vector3.one * 1.4f;
            }

            switch (type)
            {
                case GeometryType.Truck:
                    mesh = truckMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    break;
                case GeometryType.Freighter:
                    mesh = freighterMesh;
                    defaultLayerName = "Vehicles";
                    outlineLayerName = "Vehicles Outline";
                    outlineTransparentLayerName = "Vehicles Outline Transparent";
                    localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    break;
                case GeometryType.ProducerTetrahedron:
                    mesh = producerTetrahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.ProducerCube:
                    mesh = producerCubeMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.ProducerOctahedron:
                    mesh = producerOctahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.ProducerIcosahedron:
                    mesh = producerIcosahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.ProducerDodecahedron:
                    mesh = producerDodecahedronMesh;
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
                case GeometryType.ParkingLot:
                    mesh = parkingLotMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Tetrahedron:
                    mesh = tetrahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Cube:
                    mesh = cubeMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Octahedron:
                    mesh = octahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Icosahedron:
                    mesh = icosahedronMesh;
                    outlineLayerName = "Outline";
                    outlineTransparentLayerName = "Outline Transparent";
                    localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case GeometryType.Dodecahedron:
                    mesh = dodecahedronMesh;
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
            gO.transform.localScale = localScale;
            var fixedGeometry = gO.GetComponent<ObjectWithFixedGeometry>();
            fixedGeometry.Init(mesh, defaultLayerName, outlineLayerName, outlineTransparentLayerName, id,
                owner?.Color ?? Color.black);
            Debug.Log($"Debug: {owner}, color: {owner?.Color ?? Color.black}, truck?: {type == GeometryType.Truck}");
            return fixedGeometry;
        }

        public GameObject GetFullRoadGameObject() => Instantiate(fullRoadPrefab, transform);
    }
}