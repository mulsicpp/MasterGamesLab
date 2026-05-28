
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class IngameUI : MonoBehaviour
    {
        private VisualElement root;

        private BuildMode buildMode;

        public BuildMode BuildMode {
            get => buildMode;
            set
            {
                if (buildMode == value) return;
                buildMode = value;
            }
        }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        public void Show()
        {
            root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
        }
    }
}