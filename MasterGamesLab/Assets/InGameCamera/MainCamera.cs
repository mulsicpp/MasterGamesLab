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

        private RenderTexture tileIdTexture;
        private PlanetCameraController planetCameraController;

        private void OnEnable()
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
            if (tileIdTexture != null)
            {
                tileIdTexture.Release();
                Destroy(tileIdTexture);
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

            EnsureRenderTextureMatchesScreen();

            // Sync the ID Camera to perfectly match the Main Camera
            tileIdCamera.fieldOfView = mainCamera.fieldOfView;
            tileIdCamera.transform.position = mainCamera.transform.position;
            tileIdCamera.transform.rotation = mainCamera.transform.rotation;

            // Render exactly ONE frame into our texture
            tileIdCamera.Render();

            // Request the pixel readback from the GPU
            AsyncGPUReadback.Request(
                tileIdTexture,
                0,
                mX, 1, // X offset and width
                mY, 1, // Y offset and height
                0, 1, // Z offset and depth
                onReadbackComplete
            );
        }

        private void EnsureRenderTextureMatchesScreen()
        {
            var screenW = Screen.width;
            var screenH = Screen.height;

            if (tileIdTexture != null && tileIdTexture.width == screenW && tileIdTexture.height == screenH)
                return;

            if (tileIdTexture != null)
            {
                tileIdTexture.Release();
                Destroy(tileIdTexture);
            }

            tileIdTexture = new RenderTexture(screenW, screenH, 16, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point // We want raw exact pixel colors for our IDs, no blurring
            };

            tileIdTexture.Create();
            tileIdCamera.targetTexture = tileIdTexture;
        }

        /*private void OnGUI()
        {
            if (tileIdTexture == null)
            {
                return;
            }

            var width = Screen.width / 3;
            var height = Screen.height / 3;
            var rect = new Rect(10, 10, width, height);

            GUI.DrawTexture(rect, tileIdTexture, ScaleMode.ScaleToFit, false);
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