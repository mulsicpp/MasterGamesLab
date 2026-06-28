using System;
using Map.Hoverables;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace InGameCamera
{
    public class MainCamera : MonoBehaviour
    {
        public static MainCamera Instance { get; private set; } = null!;

        public Vector3 CurrentPosition => planetCameraController.transform.position;
        public float CurrentDistance => planetCameraController.CurrentDistance;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light[] lights;
        [SerializeField] private Vector3[] lightOffsetAngles;

        private PlanetCameraController planetCameraController;

        public bool PlanetControllerEnabled
        {
            get => planetCameraController.enabled;
            set => planetCameraController.enabled = value;
        }

        private Vector3 lastCamPos;
        private Quaternion lastCamRot;
        private float lastFOV;

        private void Awake()
        {
            Instance = this;
            planetCameraController = mainCamera.GetComponent<PlanetCameraController>();
        }

        private void Start()
        {
            planetCameraController.Target = Map.Map.Instance.transform;
        }

        private void OnDestroy()
        {
        }

        private void LateUpdate()
        {
            for (var i = 0; i < lights.Length; i++)
            {
                lights[i].transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(lightOffsetAngles[i]);
            }
        }
    }
}