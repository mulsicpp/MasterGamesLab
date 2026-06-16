using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PinboardUi : MonoBehaviour
    {
        public VisualTreeAsset truckTemplate;

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

        public VisualElement CreatePinElement(VisualTreeAsset template)
        {
            VisualElement wrapper = new VisualElement();
            wrapper.style.height = Length.Percent(5f);
            wrapper.style.aspectRatio = 0.7f;
            wrapper.AddToClassList("element");
            wrapper.style.position = Position.Absolute;

            VisualElement visualContent = template.Instantiate();
            wrapper.Add(visualContent);

            root.Add(wrapper);
            return wrapper;
        }
    }
}