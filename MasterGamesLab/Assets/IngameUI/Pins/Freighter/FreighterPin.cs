using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Hoverables;
using Map.Infrastructure;
using System;
using System.Collections.Generic;

namespace UI
{
    public class FreighterPin : Pin
    {
        [Serializable]
        public struct GoodImagePair
        {
            public Good GoodType;
            public Sprite ImageAsset;
        }

        [SerializeField] private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();
        public Dictionary<Good, Sprite> goodsImages = new Dictionary<Good, Sprite>();

        private VehicleRenderer vehicleRenderer;
        private Label timeLabel;
        private VisualElement icon, time;

        protected override float pinHeightPercent => 8f;
        protected override float pinAspectRatio => 0.6666666f;

        // MANAGER HOOKS: Let PinboardUi read the entity configuration natively
        public override object CurrentTile => vehicleRenderer?.Vehicle?.IsParked == true ? vehicleRenderer.Vehicle.ParkedTile : null;
        public override bool IsStructure => false;

        // Expose linked truck's UI pin directly to the central layout calculator
        public Pin LinkedTruckPin => (vehicleRenderer?.Vehicle as Freighter)?.Truck?.Renderer is TruckRenderer tr ? tr.Pin : null;

        protected override void OnEnable()
        {
            vehicleRenderer = GetComponentInParent<VehicleRenderer>();
            base.OnEnable();
        }

        protected override void Start()
        {
            var hoverable = UiElement.Q<VisualElement>("Pickable");
            base.Start();

            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                {
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }
            hoverable.BringToFront();
        }

        private void Update()
        {
            if (vehicleRenderer?.Vehicle == null) return;

            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
                HoverablePicker.Instance.DenyPick = true;
            }

            time.style.visibility = vehicleRenderer.Vehicle.IsParked ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void LateUpdate()
        {
            Freighter vehicle = vehicleRenderer.Vehicle as Freighter;
            if (vehicle == null || !vehicle.Exists || vehicle.Transform == null)
            {
                SetShowing(false);
                return;
            }

            SetShowing(true);
            timeLabel.text = vehicle.RemainingDriveTime is float t ? ((int)Mathf.Ceil(t)).ToString() + "s" : "";

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
            time = UiElement.Q<VisualElement>("Time");
            icon = UiElement.Q<VisualElement>("Icon");
            timeLabel = UiElement.Q<Label>("TimeLabel");
            icon.style.unityBackgroundImageTintColor = vehicleRenderer.Vehicle.Owner.Color;
        }
    }
}