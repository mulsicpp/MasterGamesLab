using System;
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
        [SerializeField] private Camera tileIdCamera;
        [SerializeField] private Light[] lights;
        [SerializeField] private Vector3[] lightOffsetAngles;

        private RenderTexture tileIdTexture1X1;
        private PlanetCameraController planetCameraController;

        public bool PlanetControllerEnabled
        {
            get => planetCameraController.enabled;
            set => planetCameraController.enabled = value;
        }

        private Vector3 lastCamPos;
        private Quaternion lastCamRot;
        private float lastFOV;

        private void OnEnable()
        {
            Instance = this;
            planetCameraController = mainCamera.GetComponent<PlanetCameraController>();
        }

        private void Start()
        {
            planetCameraController.Target = Map.Map.Instance.transform;
            tileIdCamera.enabled = false;
        }

        private void OnDestroy()
        {
            if (tileIdTexture1X1 != null)
            {
                tileIdTexture1X1.Release();
                Destroy(tileIdTexture1X1);
            }
        }

        private void LateUpdate()
        {
            for (var i = 0; i < lights.Length; i++)
            {
                lights[i].transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(lightOffsetAngles[i]);
            }
        }

        public void RequestCurrentlyHoveredTile(Action<AsyncGPUReadbackRequest> onReadbackComplete)
        {
            var mousePos = Mouse.current.position.ReadValue();
            var mX = (int)mousePos.x;
            var mY = (int)mousePos.y;

            if (mX < 0 || mX >= Screen.width || mY < 0 || mY >= Screen.height)
            {
                return;
            }

            /*var mouseDelta = Mouse.current.delta.ReadValue();
            var cameraMoved = CheckIfCameraMoved();
            var mouseMoved = mouseDelta.sqrMagnitude > 0.01f;
            if (!cameraMoved && !mouseMoved)
            {
                return;
            }*/

            EnsureRenderTextureMatchesScreen();

            // tileIdCamera.fieldOfView = mainCamera.fieldOfView;
            tileIdCamera.transform.position = mainCamera.transform.position;
            tileIdCamera.transform.rotation = mainCamera.transform.rotation;

            // This warps the camera's vision to zoom infinitely into the mouse pixel
            var pickMat = Matrix4x4.identity;
            pickMat.m00 = Screen.width;
            pickMat.m11 = Screen.height;
            pickMat.m03 = Screen.width - 2.0f * mousePos.x;
            pickMat.m13 = Screen.height - 2.0f * mousePos.y;

            tileIdCamera.projectionMatrix = pickMat * mainCamera.projectionMatrix;

            tileIdCamera.Render();
            AsyncGPUReadback.Request(
                tileIdTexture1X1,
                0,
                0, 1,
                0, 1,
                0, 1,
                onReadbackComplete
            );
        }


        private bool CheckIfCameraMoved()
        {
            if (mainCamera.transform.position != lastCamPos ||
                mainCamera.transform.rotation != lastCamRot ||
                !Mathf.Approximately(mainCamera.fieldOfView, lastFOV))
            {
                lastCamPos = mainCamera.transform.position;
                lastCamRot = mainCamera.transform.rotation;
                lastFOV = mainCamera.fieldOfView;
                return true;
            }

            return false;
        }

        private void EnsureRenderTextureMatchesScreen()
        {
            var screenW = Screen.width;
            var screenH = Screen.height;

            if (tileIdTexture1X1 != null && tileIdTexture1X1.width == screenW && tileIdTexture1X1.height == screenH)
                return;

            if (tileIdTexture1X1 != null)
            {
                tileIdTexture1X1.Release();
                Destroy(tileIdTexture1X1);
            }

            tileIdTexture1X1 = new RenderTexture(1, 1, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point
            };
            tileIdTexture1X1.Create();

            tileIdCamera.targetTexture = tileIdTexture1X1;
        }

        /*private void OnGUI()
        {
            if (tileIdTexture1X1 == null)
            {
                return;
            }

            var width = Screen.width / 3;
            var height = Screen.height / 3;
            var rect = new Rect(10, 10, width, height);

            GUI.DrawTexture(rect, tileIdTexture1X1, ScaleMode.ScaleToFit, false);
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