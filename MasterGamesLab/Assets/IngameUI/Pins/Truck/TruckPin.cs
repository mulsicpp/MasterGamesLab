using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
using Map.Hoverables;
using System;
using Map.Infrastructure;
using System.Collections.Generic;
using GLTFast.Schema;
using System.Linq;

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

        [SerializeField]
        private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();

        public Dictionary<Good, Sprite> goodsImages = new Dictionary<Good, Sprite>();

        private VehicleRenderer vehicleRenderer;

        private Label timeLabel;
        private VisualElement icon, good, time;

        protected override float pinHeightPercent => 8f;
        protected override float pinAspectRatio => 0.6666666f;

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Pickable");
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

        public void OnEnable()
        {
            vehicleRenderer = GetComponentInParent<VehicleRenderer>();
        }

        private void Update()
        {
            var loadedgood = (vehicleRenderer.Vehicle as Truck).Good;
            good.style.backgroundImage = loadedgood != Map.Infrastructure.Good.None ? new StyleBackground(goodsImages[loadedgood]) : null;
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

        private Vector2 _currentLayoutOffset = Vector2.zero;

        protected override void LateUpdate()
        {
            Truck Vehicle = vehicleRenderer.Vehicle as Truck;
            if (!Vehicle.Exists || Vehicle.Transform == null)
            {
                SetShowing(false);
                return;
            }

            timeLabel.text = Vehicle.RemainingDriveTime is float t
                ? ((int)Mathf.Ceil(t)).ToString() + "s"
                : "";

            if (Vehicle.IsParked)
            {
                var parkedTile = Vehicle.ParkedTile;

                var trucks = Map.Map.Instance.Fleet.Trucks
                    .Where(t => t.ParkedTile == parkedTile)
                    .ToList();

                int totalCount = trucks.Count;
                int myIndex = trucks.IndexOf(Vehicle);

                if (totalCount > 1 && myIndex >= 0)
                {
                    float shiftMultiplier = myIndex - ((totalCount - 1) / 2f);

                    float pinWidth = UiElement.layout.width;

                    _currentLayoutOffset = new Vector2(shiftMultiplier * pinWidth, 0f);
                }
                else
                {
                    _currentLayoutOffset = Vector2.zero;
                }
            }
            else
            {
                _currentLayoutOffset = Vector2.zero;
            }

            base.LateUpdate();
        }

        protected override Vector2 GetCustomOffset()
        {
            return _currentLayoutOffset;
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
            icon = UiElement.Q<VisualElement>("Icon");
            timeLabel = UiElement.Q<Label>("TimeLabel");
            good = UiElement.Q<VisualElement>("Recource");
            time = UiElement.Q<VisualElement>("Time");
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