using System.Collections.Generic;
using GeometryGeneration;
using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode]
public class MapEditmodeTest : MonoBehaviour
{
    [SerializeField] private int radius = 10;
    [SerializeField] private int resolution = 5;
    [SerializeField] private float hexSize = 1;
    [SerializeField] private float projectionFactor = 1;

    [SerializeField] private int maxSpawn = 10;

    public static int MaxSpawn = 10;

    private HexagonalSphere hexagonalSphere;
    private MeshFilter meshFilter;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        DrawSphere();
    }

    private void Update()
    {
        Debug.Log("Update!!");
        DrawSphere();
    }

    private void DrawSphere()
    {
        Debug.Log("Sphere drawing");
        MaxSpawn = maxSpawn;
        hexagonalSphere = new HexagonalSphere(radius, resolution, hexSize);
        hexagonalSphere.GenerateMesh(meshFilter, projectionFactor);
    }

    private void SpawnDebugSpheres(List<Point> positions)
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        foreach (var position in positions)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = position.Position;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.transform.parent = transform;
        }
    }
}