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
        
        // Expose the renderer so the manager can inspect the underlying fleet structure directly
        public VehicleRenderer VehicleRenderer { get; private set; }
        private Label timeLabel;
        private VisualElement icon, time;

        protected override float pinHeightPercent => 8f;
        protected override float pinAspectRatio => 0.5618f;

        protected override void OnEnable()
        {
            VehicleRenderer = GetComponentInParent<VehicleRenderer>();
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
            if (VehicleRenderer?.Vehicle == null) return;

            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.CurrentlyHovered = VehicleRenderer.Vehicle;
                HoverablePicker.Instance.DenyPick = true;
            }
            
            time.style.visibility = VehicleRenderer.Vehicle.IsParked ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void LateUpdate()
        {
            Freighter vehicle = VehicleRenderer.Vehicle as Freighter;
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
            upVector = Map.Map.Instance.GetProjectedVehicleTransform(VehicleRenderer.Vehicle.Transform).Up;
            return projectedPosition;
        }

        protected override void InitializeUiComponents()
        {
            time = UiElement.Q<VisualElement>("Time");
            icon = UiElement.Q<VisualElement>("Icon");
            timeLabel = UiElement.Q<Label>("TimeLabel");
            icon.style.unityBackgroundImageTintColor = VehicleRenderer.Vehicle.Owner.Color;
        }
    }
}