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

        [SerializeField]
        private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();

        public Dictionary<Good, Sprite> goodsImages = new Dictionary<Good, Sprite>();
        private VehicleRenderer vehicleRenderer;

        private Label timeLabel;
        private VisualElement icon, time;

        protected override float pinHeightPercent => 8f;
        protected override float pinAspectRatio => 0.4f;

        public void OnEnable()
        {
            vehicleRenderer = GetComponentInParent<VehicleRenderer>();
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
        }

        private void Update()
        {
            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles))
            {
                Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
                HoverablePicker.Instance.DenyPick = true;
            }
            if (vehicleRenderer.Vehicle.IsParked)
            {
                time.style.visibility = Visibility.Hidden;
            }
            else
            {
                time.style.visibility = Visibility.Visible;
            }
        }

        protected override void LateUpdate()
        {
            if (!vehicleRenderer.Vehicle.Exists || vehicleRenderer.Vehicle.Transform == null)
            {
                SetShowing(false);
                return;
            }
            timeLabel.text = vehicleRenderer.Vehicle.RemainingDriveTime is float t ? ((int)Mathf.Ceil(t)).ToString() + "s" : "";
            base.LateUpdate();
        }

        protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            Vector3 rawPosition = gameObject.transform.position;
            Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
            upVector = Map.Map.Instance.GetProjectedVehicleTransform(vehicleRenderer.Vehicle.Transform).Up;
            return projectedPosition;
        }

        protected override void InitializeUiComponents()
        {
            time = UiElement.Q<VisualElement>("Time");
            icon = UiElement.Q<VisualElement>("Icon");
            timeLabel = UiElement.Q<Label>("TimeLabel");
            icon.style.unityBackgroundImageTintColor = vehicleRenderer.Vehicle.Owner.Color;
        }

        // override protected void OnMouseEnterElement(MouseEnterEvent evt)
        // {
        //     if (!Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Vehicles)) return;
        //     Map.Map.Instance.isOverUI = true;
        //     Map.Map.Instance.CurrentlyHovered = vehicleRenderer.Vehicle;
        // }
    }
}