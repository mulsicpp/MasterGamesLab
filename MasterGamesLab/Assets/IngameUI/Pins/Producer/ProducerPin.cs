using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Map.Infrastructure;
using Map.Hoverables;
using Map.Fleet;

namespace UI
{
    public class ProducerPin : Pin
    {
        [Serializable]
        private struct ProducerGoodImagePair
        {
            public Good GoodType;
            public Sprite ImageAsset;
        }

        [SerializeField]
        private List<ProducerGoodImagePair> producerGoodsConfiguration = new List<ProducerGoodImagePair>();

        private Dictionary<Good, Sprite> producerGoodsImages = new Dictionary<Good, Sprite>();

        public StructureRenderer structureRenderer;
        private VisualElement goodIcon;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 1f;

        // MANAGER HOOKS: Identify this as a fixed building anchored to a specific tile

        protected override void OnEnable()
        {
            structureRenderer = GetComponentInParent<StructureRenderer>();
            base.OnEnable(); // Registers to the central layout pool
        }

        protected override void Start()
        {
            hoverable = UiElement.Q<VisualElement>("Icon");
            base.Start();
            
            foreach (var pair in producerGoodsConfiguration)
            {
                if (!producerGoodsImages.ContainsKey(pair.GoodType))
                {
                    producerGoodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }
            UiElement.SendToBack();
        }

        public void Update()
        {
            if (structureRenderer?.Structure == null) return;

            if (IsHovered && Map.Map.Instance.ShouldBeHoverablePredicate(structureRenderer.Structure.Tile))
            {
                Map.Map.Instance.CurrentlyHovered = structureRenderer.Structure.Tile;
                HoverablePicker.Instance.DenyPick = true;
            }
        }

        protected override void LateUpdate()
        {
            if (structureRenderer?.Structure is Producer producer)
            {
                var Good = producer.Good;

                if (Good != Good.None && producerGoodsImages.TryGetValue(Good, out Sprite img))
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
            else
            {
                SetShowing(false);
                return;
            }

            // Stacking lift calculations are now fully handled by the central PinboardUi pass!
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
            if (structureRenderer?.Structure is Producer producer)
            {
                var requestedGood = producer.Good;
                if (requestedGood != Good.None && producerGoodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
            }
        }
    }
}