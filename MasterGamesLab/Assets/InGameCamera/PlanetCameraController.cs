using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Required for the new Input System

namespace InGameCamera
{
    public class PlanetCameraController : MonoBehaviour
    {
        private InputActionMap sphereNavigationActionMap;
        private InputAction primaryMousePressedAction;
        private InputAction lookAction;
        private InputAction zoomAction;

        public Transform Target;
        public float CurrentDistance { get; private set; } = 3f;

        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private float minZoom = 1.1f;
        [SerializeField] private float maxZoom = 5f;
#pragma warning disable CS0414
        [SerializeField] private float minZoomSpeed = 0.05f;
#pragma warning disable CS0414
        [SerializeField] private float maxZoomSpeed = 1;

        [SerializeField] private float zoomBase = 1.1f;
        [SerializeField] private float zoomFactor = 2.5f;
        [SerializeField] private float zoomOffset = 0.5f;

        [SerializeField] private float rotationSpeedFactor = 0.95f;

        [SerializeField] private float minRotationSpeed = 0.03f;
        [SerializeField] private float maxRotationSpeed = 0.3f;

        [SerializeField] private float minPitch = -90f;
        [SerializeField] private float maxPitch = 90f;

        [SerializeField] private float minScalingFactor = 0.5f;
        [SerializeField] private float maxScalingFactor = 2f;

        private new Camera camera;

        private float zoomExp;

        // Internal tracking variables
        private float currentYaw = 0f;
        private float currentPitch = 0f;

        // public float ScalingFactor => Remap(CurrentDistance, minZoom, maxZoom, maxScalingFactor, minScalingFactor);
        public float ScalingFactor
        {
            get
            {
                return (1.0f / (CurrentDistance - 1.0f)) * 0.3f + 0.7f;
            }
        }

        private void Awake()
        {
            sphereNavigationActionMap = inputActions.FindActionMap("SphereNavigation");
            primaryMousePressedAction = sphereNavigationActionMap.FindAction("PrimaryMousePressed");
            lookAction = sphereNavigationActionMap.FindAction("Look");
            zoomAction = sphereNavigationActionMap.FindAction("Zoom");
        }

        private void OnEnable()
        {
            sphereNavigationActionMap = inputActions.FindActionMap("SphereNavigation");
            sphereNavigationActionMap.Enable();

            camera = gameObject.GetComponent<Camera>();
        }

        private void Start()
        {
            var angles = transform.eulerAngles;
            currentPitch = angles.x;
            currentYaw = angles.y;

            zoomExp = 0.0f;
        }

        private void LateUpdate()
        {
            // HandleInput();
            // UpdateCameraTransform();
            UpdateCameraTransformNew();
        }

        private void OnDisable()
        {
            sphereNavigationActionMap.Disable();
        }

        private void HandleInput()
        {
            if (primaryMousePressedAction.IsPressed())
            {
                var lookDelta = lookAction.ReadValue<Vector2>();

                var currentRotationSpeed =
                    Mathf.Lerp(minRotationSpeed, maxRotationSpeed, (CurrentDistance - minZoom) / (maxZoom - minZoom));

                currentYaw += lookDelta.x * currentRotationSpeed;
                currentPitch -= lookDelta.y * currentRotationSpeed;

                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }

            var scrollDelta = zoomAction.ReadValue<Vector2>();

            // if (Mathf.Abs(scrollDelta.y) > 0.001f)
            // {
            //     var currentZoomSpeed =
            //         ExponentialMapRange(CurrentDistance, minZoom, maxZoom, minZoomSpeed, maxZoomSpeed);
            //     CurrentDistance -= scrollDelta.y * currentZoomSpeed;
            //     CurrentDistance = Mathf.Clamp(CurrentDistance, minZoom, maxZoom);
            // }

            float minZoomExp = Mathf.Log((minZoom - zoomOffset) / zoomFactor, zoomBase);
            float maxZoomExp = Mathf.Log((maxZoom - zoomOffset) / zoomFactor, zoomBase);

            if (zoomExp - scrollDelta.y <= maxZoomExp && zoomExp - scrollDelta.y >= minZoomExp)
            {
                zoomExp -= scrollDelta.y;
                CurrentDistance = Mathf.Pow(zoomBase, zoomExp) * zoomFactor + zoomOffset;
            }
        }

        private void UpdateCameraTransform()
        {
            var rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            var position = Target.position + rotation * new Vector3(0f, 0f, -CurrentDistance);

            transform.position = position;
            transform.rotation = rotation;
        }

        private void UpdateCameraTransformNew()
        {
            if (primaryMousePressedAction.IsPressed())
            {
                var lookDelta = lookAction.ReadValue<Vector2>();

                var velocityWorld = camera.ScreenToWorldPoint(new(0, 0, CurrentDistance - 1)) - camera.ScreenToWorldPoint(new(lookDelta.x, lookDelta.y, CurrentDistance - 1));
                var axis = Vector3.Cross(transform.position, velocityWorld).normalized;

                transform.rotation = Quaternion.AngleAxis(velocityWorld.magnitude * rotationSpeedFactor * 180 / Mathf.PI, axis) * transform.rotation;
            }

            var scrollDelta = zoomAction.ReadValue<Vector2>();

            float minZoomExp = Mathf.Log((minZoom - zoomOffset) / zoomFactor, zoomBase);
            float maxZoomExp = Mathf.Log((maxZoom - zoomOffset) / zoomFactor, zoomBase);

            if (zoomExp - scrollDelta.y <= maxZoomExp && zoomExp - scrollDelta.y >= minZoomExp)
            {
                zoomExp -= scrollDelta.y;
                CurrentDistance = Mathf.Pow(zoomBase, zoomExp) * zoomFactor + zoomOffset;
            }

            var position = Target.position + transform.rotation * new Vector3(0f, 0f, -CurrentDistance);
            transform.position = position;
        }

        private static float ExponentialMapRange(float value, float minX, float maxX, float minY, float maxY)
        {
            value = Mathf.Clamp(value, minX, maxX);
            var k = Math.Log(maxY / minY) / (maxX - minX);
            return minY * (float)Math.Exp(k * (value - minX));
        }

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.red;
        // 
        //     var worldSpacePos = MainCamera.Instance.GetComponentInChildren<Camera>().ScreenToWorldPoint(new(0, 0, CurrentDistance - 1));
        //     Gizmos.DrawSphere(worldSpacePos, 0.02f);
        // }

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            // 1. Convert the value into a 0.0 to 1.0 percentage of the original range
            float percentage = (value - fromMin) / (fromMax - fromMin);

            // 2. Project that percentage onto the new target range
            return toMin + percentage * (toMax - toMin);
        }
    }
}