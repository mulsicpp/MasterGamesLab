using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Map.Infrastructure;

namespace UI
{
    public class ConsumerPin : Pin
    {
       

        private StructureRenderer structureRenderer;
        private Label payout;
        private VisualElement goodIcon;

        protected override float pinHeightPercent => 6f;
        protected override float pinAspectRatio => 0.55f;

        public void OnEnable()
        {
            structureRenderer = GetComponentInParent<StructureRenderer>();
        }

        protected override void Start()
        {

            base.Start();
        }

        protected override void LateUpdate()
        {
            if (payout != null && structureRenderer?.Structure is Consumer consumer)
            {
                payout.text = consumer.Request.Payout.ToString();
                var requestedGood = consumer.Request.Good;

                if (requestedGood != Good.None && IngameUI.Instance.goodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    setActive(true);
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
                else
                {
                    goodIcon.style.backgroundImage = null;
                    setActive(false);
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
                if (requestedGood != Good.None && IngameUI.Instance.goodsImages.TryGetValue(requestedGood, out Sprite img))
                {
                    goodIcon.style.backgroundImage = new StyleBackground(img);
                }
            }
        }
    }
}