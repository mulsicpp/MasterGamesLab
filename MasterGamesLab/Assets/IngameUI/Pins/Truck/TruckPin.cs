using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Hoverables;
using System;
using Map.Infrastructure;
using System.Collections.Generic;

namespace UI
{
    public class TruckPin : Pin
    {
        [Serializable]
        public struct GoodImagePair
        {
            public Good GoodType;
            public Sprite ImageAsset;
        }

        [SerializeField] private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();
        public Dictionary<Good, Sprite> goodsImages = new Dictionary<Good, Sprite>();

        public VehicleRenderer vehicleRenderer;
        private Label timeLabel;
        private VisualElement icon, good, time;

        protected override float pinHeightPercent => 8f;
        protected override float pinAspectRatio => 0.5618f;

        // MANAGER HOOKS: Hand over data context seamlessly to the manager pa
        protected override void OnEnable()
        {
            vehicleRenderer = GetComponentInParent<VehicleRenderer>();
            base.OnEnable(); // Crucial: Registers this pin into the manager pool
        }

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Pickable");
            base.Start();

            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
            }
            hoverable.BringToFront();
        }

        private void Update()
        {
            if (vehicleRenderer?.Vehicle == null) return;

            var loadedgood = (vehicleRenderer.Vehicle as Truck).Good;
            good.style.backgroundImage = loadedgood != Good.None ? new StyleBackground(goodsImages[loadedgood]) : null;

            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
                HoverablePicker.Instance.DenyPick = true;
            }

            time.style.visibility = vehicleRenderer.Vehicle.IsParked ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void LateUpdate()
        {
            Truck Vehicle = vehicleRenderer.Vehicle as Truck;
            if (Vehicle == null || !Vehicle.Exists)
            {
                SetShowing(false);
                return;
            }

            SetShowing(true);
            timeLabel.text = Vehicle.RemainingDriveTime is float t ? ((int)Mathf.Ceil(t)).ToString() + "s" : "";

            // All sorting/offset math was completely stripped out of here!
            base.LateUpdate();
        }

        protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            Vector3 rawPosition = gameObject.transform.position;
            Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
            upVector = (Map.Map.Instance.GetProjectedPosition(rawPosition * 1.01f) - projectedPosition).normalized;
            return projectedPosition;
        }

        protected override void InitializeUiComponents()
        {
            icon = UiElement.Q<VisualElement>("Icon");
            timeLabel = UiElement.Q<Label>("TimeLabel");
            good = UiElement.Q<VisualElement>("Recource");
            time = UiElement.Q<VisualElement>("Time");
            icon.style.unityBackgroundImageTintColor = vehicleRenderer.Vehicle.Owner.Color;
        }
    }
}