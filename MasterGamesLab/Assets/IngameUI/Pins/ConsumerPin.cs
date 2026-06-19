using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Map.Infrastructure;

namespace UI
{
    public class ConsumerPin : Pin
    {
        [Serializable]
        public struct GoodImagePair
        {
            public Good GoodType;
            public VectorImage ImageAsset;
        }

        [SerializeField] 
        private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();

        private Dictionary<Good, VectorImage> goodsImages = new Dictionary<Good, VectorImage>();

        private StructureRenderer structureRenderer;
        private Label payout;
        private VisualElement goodIcon;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 0.5f;

        public void OnEnable()
        {
            structureRenderer = GetComponentInParent<StructureRenderer>();
        }

        protected override void Start()
        {
            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                {
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }

            base.Start();
        }

        protected override void LateUpdate()
        {
            if (payout != null && structureRenderer?.Structure is Consumer consumer)
            {
                payout.text = consumer.Request.Payout.ToString();
                var requestedGood = consumer.Request.Good;

                if (requestedGood != Good.None && goodsImages.TryGetValue(requestedGood, out VectorImage img))
                {
                    setActive(true);
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
                else
                {
                    setActive(false);
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
            payout = UiElement.Q<Label>("TimeLabel");

            if (structureRenderer?.Structure is Consumer consumer)
            {
                var requestedGood = consumer.Request.Good;
                if (requestedGood != Good.None && goodsImages.TryGetValue(requestedGood, out VectorImage img))
                {
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
            }
        }
    }
}