using UnityEngine;
using UnityEngine.UIElements;
using Map.Fleet;
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

        public VehicleRenderer VehicleRenderer;
        private Label timeLabel;
        private VisualElement icon, good, time;

        [SerializeField]
        private Sprite truckWithGood, truckWithoutGood;

        protected override float pinHeightPercent => 4f;
        protected override float pinAspectRatio => 0.5618f;

        // MANAGER HOOKS: Hand over data context seamlessly to the manager pa
        protected override void OnEnable()
        {
            VehicleRenderer = GetComponentInParent<VehicleRenderer>();
            VehicleRenderer.VehiclePin = this;
            base.OnEnable(); // Crucial: Registers this pin into the manager pool
        }

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Pickable");
            hoverable.userData = VehicleRenderer?.Vehicle;
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
            var loadedgood = (VehicleRenderer.Vehicle as Truck).Good;
            if (loadedgood == Good.None)
            {
                good.style.backgroundImage =  null;
                icon.style.backgroundImage = new StyleBackground(truckWithoutGood);
            }
            else
            {
                good.style.backgroundImage = new StyleBackground(goodsImages[loadedgood]);
                icon.style.backgroundImage = new StyleBackground(truckWithGood);
            }


            hoverable.pickingMode = Map.Map.Instance.ShouldBeHoverablePredicate(VehicleRenderer.Vehicle) ? PickingMode.Position : PickingMode.Ignore;
            time.style.visibility = VehicleRenderer.Vehicle.IsParked ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void LateUpdate()
        {
            Truck Vehicle = VehicleRenderer.Vehicle as Truck;
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

        public override Vector3 GetTargetWorldPosition(out Vector3 upVector)
        {
            var truck = VehicleRenderer.Vehicle as Truck;
            if (truck.Freighter is Freighter freighter)
            {
                return freighter.Renderer.VehiclePin.GetTargetWorldPosition(out upVector);
            }
            Vector3 rawPosition = gameObject.transform.position;
            if (truck.IsParked)
            {
                rawPosition = truck.ParkedTile.PositionOnSphere;
            }
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
            icon.style.unityBackgroundImageTintColor = VehicleRenderer.Vehicle.Owner.Color;
        }
    }
}