using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Map.Infrastructure;
using Map.Hoverables;

namespace UI
{
    public class ConsumerPin : Pin
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


        private StructureRenderer structureRenderer;
        private Label payout;
        private VisualElement goodIcon;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 1f;

        public void OnEnable()
        {
            structureRenderer = GetComponentInParent<StructureRenderer>();
        }

        protected override void Start()
        {
            base.Start();
            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                {
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }
        }

        public void Update()
        {
            if (IsHovered && Map.Map.Instance.HoverLayers.HasFlag(HoverablePicker.HoverableLayer.Tiles))
            {
                Map.Map.Instance.CurrentlyHovered = structureRenderer.Structure.Tile;
                HoverablePicker.Instance.DenyPick = true;
            }
        }

        protected override void LateUpdate()
        {
            if (payout != null && structureRenderer?.Structure is Consumer consumer)
            {
                payout.text = consumer.Request.Payout.ToString();
                var requestedGood = consumer.Request.Good;

                if (requestedGood != Good.None && goodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    SetShowing(true);
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
                else
                {
                    goodIcon.style.backgroundImage = null;
                    SetShowing(false);
                    return;
                }
            }

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
            goodIcon = UiElement.Q<VisualElement>("Icon");
            payout = UiElement.Q<Label>("Payout");

            if (structureRenderer?.Structure is Consumer consumer)
            {
                var requestedGood = consumer.Request.Good;
                if (requestedGood != Good.None && goodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
            }
        }
    }
}