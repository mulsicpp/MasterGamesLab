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

        public StructureRenderer structureRenderer;
        private Label payout;
        private VisualElement goodIcon;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 1f;

        [SerializeField]
        private Color MaxPayoutColor;

        // MANAGER HOOKS: Wire this structure cleanly into the central PinboardUi loop

        protected override void OnEnable()
        {
            structureRenderer = GetComponentInParent<StructureRenderer>();
            structureRenderer.Pin = this;
            base.OnEnable(); // Crucial: Registers this pin into the manager pool
        }

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Pickable");
            hoverable.userData = structureRenderer.Structure.Tile;
            base.Start();

            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                {
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }
            UiElement.SendToBack();
        }

        public void Update()
        {
            if (structureRenderer?.Structure == null) return;

            hoverable.pickingMode = Map.Map.Instance.ShouldBeHoverablePredicate(structureRenderer.Structure.Tile) ? PickingMode.Position : PickingMode.Ignore;
        }

        protected override void LateUpdate()
        {
            if (payout != null && structureRenderer?.Structure is Consumer consumer)
            {
                var requestedGood = consumer.Request.Good;

                if (requestedGood != Good.None && goodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    SetShowing(true);
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                    payout.text = consumer.Request.Payout.ToString();
                    float payoutRange = consumer.Request.MaxPayout - consumer.Request.InitialPayout;

                    float t = payoutRange > 0f ? Math.Clamp((consumer.Request.Payout - consumer.Request.InitialPayout) / payoutRange, 0f, 1f) : 0f;

                    payout.style.color = Color.Lerp(Color.white, MaxPayoutColor, t);
                }
                else
                {
                    goodIcon.style.backgroundImage = null;
                    SetShowing(false);
                    return;
                }
            }
            else
            {
                SetShowing(false);
                return;
            }

            // High-performance structural vertical lifts are now managed globally!
            base.LateUpdate();
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