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
        public float CurrentDistance { get; private set; } = 15f;

        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private float minZoom = 1.1f;
        [SerializeField] private float maxZoom = 5f;
        [SerializeField] private float minZoomSpeed = 0.05f;
        [SerializeField] private float maxZoomSpeed = 1;

        [SerializeField] private float minRotationSpeed = 0.03f;
        [SerializeField] private float maxRotationSpeed = 0.3f;

        [SerializeField] private float minPitch = -90f;
        [SerializeField] private float maxPitch = 90f;

        // Internal tracking variables
        private float currentYaw = 0f;
        private float currentPitch = 0f;

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
        }

        private void Start()
        {
            CurrentDistance = Mathf.Clamp(Vector3.Distance(transform.position, Target.position), minZoom, maxZoom);
            var angles = transform.eulerAngles;
            currentPitch = angles.x;
            currentYaw = angles.y;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
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

            if (Mathf.Abs(scrollDelta.y) > 0.001f)
            {
                var currentZoomSpeed =
                    ExponentialMapRange(CurrentDistance, minZoom, maxZoom, minZoomSpeed, maxZoomSpeed);
                CurrentDistance -= scrollDelta.y * currentZoomSpeed;
                CurrentDistance = Mathf.Clamp(CurrentDistance, minZoom, maxZoom);
            }
        }

        private void UpdateCameraTransform()
        {
            var rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            var position = Target.position + rotation * new Vector3(0f, 0f, -CurrentDistance);

            transform.position = position;
            transform.rotation = rotation;
        }

        private static float ExponentialMapRange(float value, float minX, float maxX, float minY, float maxY)
        {
            value = Mathf.Clamp(value, minX, maxX);
            var k = Math.Log(maxY / minY) / (maxX - minX);
            return minY * (float)Math.Exp(k * (value - minX));
        }
    }
}