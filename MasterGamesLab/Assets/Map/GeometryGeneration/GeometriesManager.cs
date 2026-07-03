using System;
using UnityEngine;

namespace Map.GeometryGeneration
{
    public class GeometriesManager : MonoBehaviour
    {
        const int PREFIX_MASK = 0xff00;

        public enum GeometryTypePrefix
        {
            Good = 0x0000,
            Vehicle = 0x0100,
            Structure = 0x0200,
            Action = 0x300,
        }

        public enum GeometryType
        {
            Tetrahedron = GeometryTypePrefix.Good,
            Cube,
            Octahedron,
            Icosahedron,
            Dodecahedron,
            Truck = GeometryTypePrefix.Vehicle,
            Freighter,
            ProducerTetrahedron = GeometryTypePrefix.Structure,
            ProducerCube,
            ProducerOctahedron,
            ProducerIcosahedron,
            ProducerDodecahedron,
            Consumer,
            Port,
            ParkingLot,
            ActionWait = GeometryTypePrefix.Action,
            ActionLoad,
            ActionUnload,
        }

        private static float ScaleValue => 0.0095f * Map.Instance.TileScale;

        public static Vector3 Scale => new Vector3(ScaleValue, ScaleValue, ScaleValue);
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
        [SerializeField] private Mesh actionWaitMesh;
        [SerializeField] private Mesh actionLoadMesh;
        [SerializeField] private Mesh actionUnloadMesh;

        [SerializeField] private GameObject geometryPrefab;
        [SerializeField] private GameObject routePrefab;

        [SerializeField] private Mesh buoyMesh;

        [SerializeField] private Material defaultFixedGeometryMaterial;
        [SerializeField] private Material defaultEdgeMaterial;
        [SerializeField] private Material previewMaterial;
        [SerializeField] private Material blueprintMaterial;
        [SerializeField] private Material buoyMaterial;

        [SerializeField] private Material actionMaterial;

        public Material overlappingMaterial;
        public Material invalidMaterial;
        
        public Constants.OutlineData blueprintOutline;
        public Constants.OutlineData invalid;
        public Constants.OutlineData blueprintOverlapping;
        public Constants.OutlineData previewOverlapping;
        
        private void Awake()
        {
            Instance = this;
        }

        public ObjectWithFixedGeometry GetGameObjectGeometry(GeometryType type, int id, Transform parent,
            Player.Player owner = null)
        {
            Mesh mesh;
            var localRotation = Quaternion.identity;
            var localScale = Vector3.one;
            var localPosition = Vector3.zero;

            var prefix = (GeometryTypePrefix)((int)type & PREFIX_MASK);

            switch (prefix)
            {
                case GeometryTypePrefix.Good:
                case GeometryTypePrefix.Vehicle:
                    break;
                case GeometryTypePrefix.Structure:
                    localScale = Vector3.one * 1.4f;
                    break;
                case GeometryTypePrefix.Action:
                    localRotation = Quaternion.Euler(0, 90, 0);
                    localScale = Vector3.one * 0.5f;
                    localPosition = type switch
                    {
                        GeometryType.ActionWait => new(0, 1, 1),
                        GeometryType.ActionLoad => new(0, 1, 3),
                        _ => new(0, 3, 0),
                    };
                    break;
            }

            mesh = type switch
            {
                GeometryType.Tetrahedron => tetrahedronMesh,
                GeometryType.Cube => cubeMesh,
                GeometryType.Octahedron => octahedronMesh,
                GeometryType.Icosahedron => icosahedronMesh,
                GeometryType.Dodecahedron => dodecahedronMesh,
                GeometryType.Truck => truckMesh,
                GeometryType.Freighter => freighterMesh,
                GeometryType.ProducerTetrahedron => producerTetrahedronMesh,
                GeometryType.ProducerCube => producerCubeMesh,
                GeometryType.ProducerOctahedron => producerOctahedronMesh,
                GeometryType.ProducerIcosahedron => producerIcosahedronMesh,
                GeometryType.ProducerDodecahedron => producerDodecahedronMesh,
                GeometryType.Consumer => consumerMesh,
                GeometryType.Port => portMesh,
                GeometryType.ParkingLot => parkingLotMesh,
                GeometryType.ActionWait => actionWaitMesh,
                GeometryType.ActionLoad => actionLoadMesh,
                GeometryType.ActionUnload => actionUnloadMesh,
                _ => null,
            };

            parent.localScale = Scale;
            var gO = Instantiate(geometryPrefab, parent);
            gO.transform.localPosition = localPosition;
            gO.transform.localRotation = localRotation;
            gO.transform.localScale = localScale;
            var fixedGeometry = gO.GetComponent<ObjectWithFixedGeometry>();
            fixedGeometry.Init(mesh, id, owner?.Color ?? Color.black);

            if (prefix == GeometryTypePrefix.Action)
            {
                fixedGeometry.SetMaterial(actionMaterial);
            }

            return fixedGeometry;
        }

        public GameObject GetRouteGameObject() => Instantiate(routePrefab, transform);

        public Mesh GetBuoyMesh() => buoyMesh;

        public Material GetEdgeMaterial() => defaultEdgeMaterial;

        public Material GetFixedGeometryMaterial() => defaultFixedGeometryMaterial;

        public Material GetPreviewMaterial() => previewMaterial;

        public Material GetBlueprintMaterial() => blueprintMaterial;

        public Material GetBuoyMaterial() => buoyMaterial;
    }
}