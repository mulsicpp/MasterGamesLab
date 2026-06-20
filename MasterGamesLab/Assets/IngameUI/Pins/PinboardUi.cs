using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PinboardUi : MonoBehaviour
    {
        public static PinboardUi Instance { get; private set; }

        public VisualElement root { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        protected virtual void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
        }

        public VisualElement CreatePinElement(VisualTreeAsset template, float heightPercent, float aspectRatio)
        {
            VisualElement wrapper = new VisualElement();

            wrapper.style.height = Length.Percent(heightPercent);
            wrapper.style.aspectRatio = aspectRatio;

            wrapper.AddToClassList("element");
            wrapper.style.position = Position.Absolute;

            wrapper.style.left = 0;
            wrapper.style.top = 0;
            wrapper.style.marginLeft = StyleKeyword.Null;
            wrapper.style.marginTop = StyleKeyword.Null;

            wrapper.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(100f));

            VisualElement visualContent = template.Instantiate();
            wrapper.Add(visualContent);

            root.Add(wrapper);
            return wrapper;
        }
    }
}