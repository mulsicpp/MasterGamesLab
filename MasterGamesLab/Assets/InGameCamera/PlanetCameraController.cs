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
        private InputAction turnNorthAction;
        private InputAction lookAction;
        private InputAction zoomAction;

        public Transform Target;
        public float CurrentDistance { get; private set; } = 3f;

        public Transform FocusedObject;

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

        [SerializeField] private float rotationBaseSpeed = 90f;
        [SerializeField] private float rotationApproachFactor = 5f;

        [SerializeField] private float zoomBaseSpeed = 1.0f;
        [SerializeField] private float zoomApproachFactor = 8f;

        [SerializeField] private Vector3 north = new(0, 1, 0);

        private bool turnNorth = false;

        private new Camera camera;

        private float zoomExp;
        private float currentZoomExp;

        // Internal tracking variables
        private float currentYaw = 0f;
        private float currentPitch = 0f;
        public bool supressZoom = false;

        // public float ScalingFactor => Remap(CurrentDistance, minZoom, maxZoom, maxScalingFactor, minScalingFactor);
        public float ScalingFactor
        {
            get
            {
                return (1.0f / (CurrentDistance - 1.0f)) * 0.8f + 0.7f;
            }
        }

        public Vector3 TangentNorth
        {
            get
            {
                var northNorm = north.normalized;
                var dot = Vector3.Dot(transform.forward, northNorm);

                var ret = (northNorm - dot * transform.forward).normalized;
                return ret == Vector3.zero ? transform.up : ret;
            }
        }

        public Vector2 LocalNorth
        {
            get
            {
                return camera.worldToCameraMatrix.MultiplyVector(TangentNorth);
            }
        }

        private void Awake()
        {
            sphereNavigationActionMap = inputActions.FindActionMap("SphereNavigation");
            primaryMousePressedAction = sphereNavigationActionMap.FindAction("PrimaryMousePressed");
            turnNorthAction = sphereNavigationActionMap.FindAction("TurnNorth");
            lookAction = sphereNavigationActionMap.FindAction("Look");
            zoomAction = sphereNavigationActionMap.FindAction("Zoom");

            camera = gameObject.GetComponent<Camera>();

            zoomExp = 0.0f;
            currentZoomExp = zoomExp;
        }

        private void OnEnable()
        {
            // sphereNavigationActionMap.Enable();
        }

        private void LateUpdate()
        {
            // HandleInput();
            // UpdateCameraTransform();
            UpdateCameraTransformNew();
        }

        private void OnDisable()
        {
            // sphereNavigationActionMap.Disable();
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
                FocusedObject = null;
                var lookDelta = lookAction.ReadValue<Vector2>();

                var velocityWorld = camera.ScreenToWorldPoint(new(0, 0, CurrentDistance - 1)) - camera.ScreenToWorldPoint(new(lookDelta.x, lookDelta.y, CurrentDistance - 1));
                var axis = Vector3.Cross(transform.position, velocityWorld).normalized;

                transform.rotation = Quaternion.AngleAxis(velocityWorld.magnitude * rotationSpeedFactor * 180 / Mathf.PI, axis) * transform.rotation;
            }
            else if (FocusedObject != null)
            {
                var currentVec = (transform.position - Target.position).normalized;
                var targetVec = (FocusedObject.position - Target.position).normalized;

                AddRotationStepFromTo(currentVec, targetVec);
            }
            if (!supressZoom)
            {
                var scrollDelta = zoomAction.ReadValue<Vector2>();

                float minZoomExp = Mathf.Log((minZoom - zoomOffset) / zoomFactor, zoomBase);
                float maxZoomExp = Mathf.Log((maxZoom - zoomOffset) / zoomFactor, zoomBase);

                if (zoomExp - scrollDelta.y <= maxZoomExp && zoomExp - scrollDelta.y >= minZoomExp)
                {
                    zoomExp -= scrollDelta.y;
                }

                var totalZoomDistanceAbs = Mathf.Abs(zoomExp - currentZoomExp);
                if (totalZoomDistanceAbs > 0.001f)
                {
                    var zoomDistanceThisFrame = Mathf.Min((totalZoomDistanceAbs * zoomApproachFactor + zoomBaseSpeed) * Time.deltaTime, totalZoomDistanceAbs);

                    currentZoomExp = Mathf.Lerp(currentZoomExp, zoomExp, zoomDistanceThisFrame / totalZoomDistanceAbs);
                }
                CurrentDistance = Mathf.Pow(zoomBase, currentZoomExp) * zoomFactor + zoomOffset;
            }
            var position = Target.position + transform.rotation * new Vector3(0f, 0f, -CurrentDistance);
            transform.position = position;

            if (turnNorthAction.IsPressed())
            {
                TurnNorth();
            }

            if (turnNorth)
            {
                var currentVec = transform.up;
                var targetVec = TangentNorth;

                if (!AddRotationStepFromTo(currentVec, targetVec))
                {
                    turnNorth = false;
                }
            }
        }

        private bool AddRotationStepFromTo(Vector3 currentVec, Vector3 targetVec)
        {
            var totalAngle = Vector3.Angle(currentVec, targetVec);
            if (totalAngle > 0.001f)
            {
                var angleThisFrame = Mathf.Min((totalAngle * rotationApproachFactor + rotationBaseSpeed) * Time.deltaTime, totalAngle);

                var targetVecThisFrame = Vector3.Slerp(currentVec, targetVec, angleThisFrame / totalAngle);
                transform.rotation = Quaternion.FromToRotation(currentVec, targetVecThisFrame) * transform.rotation;
                return true;
            }
            return false;
        }

        private static float ExponentialMapRange(float value, float minX, float maxX, float minY, float maxY)
        {
            value = Mathf.Clamp(value, minX, maxX);
            var k = Math.Log(maxY / minY) / (maxX - minX);
            return minY * (float)Math.Exp(k * (value - minX));
        }

        private void OnDrawGizmos()
        {
            if (camera == null) return;
            Gizmos.color = Color.red;

            Vector3 localNorth = LocalNorth * 50f;
            Vector3 screenCenter = new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2, CurrentDistance - 1.05f);

            var startPos = camera.ScreenToWorldPoint(screenCenter);
            var endPos = camera.ScreenToWorldPoint(screenCenter + localNorth);
            Gizmos.DrawLine(startPos, endPos);
        }

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            // 1. Convert the value into a 0.0 to 1.0 percentage of the original range
            float percentage = (value - fromMin) / (fromMax - fromMin);

            // 2. Project that percentage onto the new target range
            return toMin + percentage * (toMax - toMin);
        }

        public void TurnNorth()
        {
            turnNorth = true;
        }

        public void SnapNorth()
        {
            transform.rotation = Quaternion.FromToRotation(transform.up, TangentNorth) * transform.rotation;
        }

        public void SnapToFocusedObject()
        {
            if (FocusedObject != null)
            {
                transform.rotation = Quaternion.FromToRotation(-transform.forward, (FocusedObject.transform.position - Target.transform.position).normalized) * transform.rotation;

                transform.position = Target.position + transform.rotation * new Vector3(0f, 0f, -CurrentDistance);

                Debug.Log("Snapping to focused object: position is now: " + transform.position + " target is: " + Target.transform.position);
            }
        }

        public void SetForGameStart()
        {
            SnapToFocusedObject();
            SnapNorth();

            zoomExp = 0.0f;
            currentZoomExp = zoomExp;

            CurrentDistance = Mathf.Pow(zoomBase, currentZoomExp) * zoomFactor + zoomOffset;
            transform.position = Target.transform.position - transform.forward * CurrentDistance;

            Map.Map.Instance.UpdateProjectionUniforms(false);
        }


        public void CenterOnPosition(Vector3 worldPosition, float? desiredDistance = null)
        {
            if (Target == null) return;

            if (desiredDistance.HasValue)
            {
                CurrentDistance = Mathf.Clamp(desiredDistance.Value, minZoom, maxZoom);
                zoomExp = Mathf.Log((CurrentDistance - zoomOffset) / zoomFactor, zoomBase);
            }

            Vector3 directionToTarget = (worldPosition - Target.position).normalized;

            Vector3 lookDirection = -directionToTarget;

            Vector3 approximateUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(lookDirection, approximateUp)) > 0.99f)
            {
                approximateUp = Vector3.forward;
            }

            transform.rotation = Quaternion.LookRotation(lookDirection, approximateUp);

            var position = Target.position + transform.rotation * new Vector3(0f, 0f, -CurrentDistance);
            transform.position = position;
        }
    }
}