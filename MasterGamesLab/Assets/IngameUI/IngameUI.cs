
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class IngameUI : MonoBehaviour
    {
        private VisualElement root;
        private Button buildRoadButton;
        private Button buildCanalButton;

        private Button buildPortButton;

        private Button buyTruckButton;

        private Button buyFreighterButton;

        private Button currentActiveButton;


        private BuildMode buildMode;

        private const string activeClass = "ingame-build-button--active";

        public BuildMode BuildMode
        {
            get => buildMode;
            set
            {
                Debug.Log(value);
                if (buildMode == value) return;
                currentActiveButton?.RemoveFromClassList(activeClass);
                switch (value)
                {
                    case BuildMode.Road:
                        currentActiveButton = buildRoadButton;
                        break;
                    case BuildMode.Canal:
                        currentActiveButton = buildCanalButton;
                        break;
                    case BuildMode.Port:
                        currentActiveButton = buildPortButton;
                        break;
                    case BuildMode.Freighter:
                        currentActiveButton = buyFreighterButton;
                        break;
                    case BuildMode.Truck:
                        currentActiveButton = buyTruckButton;
                        break;
                    default:
                        currentActiveButton = null;
                        break;
                }
                currentActiveButton?.AddToClassList(activeClass);
                Debug.Log(currentActiveButton?.ClassListContains(activeClass));
                buildMode = value;
            }
        }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            buildRoadButton = root.Q<Button>("BuildRoadButton");
            buildCanalButton = root.Q<Button>("BuildCanalButton");
            buildPortButton = root.Q<Button>("BuildPortButton");
            buyTruckButton = root.Q<Button>("BuyTruckButton");
            buyFreighterButton = root.Q<Button>("BuyFreighterButton");


            buildRoadButton.clicked += OnBuildRoadButtonPressed;
            buildCanalButton.clicked += OnBuildCanalButtonPressed;
            buildPortButton.clicked += OnBuildPortButtonPressed;
            buyTruckButton.clicked += OnBuyTruckButtonPressed;
            buyFreighterButton.clicked += OnBuyFreighterButtonPressed;
        }

        private void OnBuildRoadButtonPressed()
        {
            BuildMode = BuildMode.Road;
        }

        private void OnBuildCanalButtonPressed()
        {
            BuildMode = BuildMode.Canal;
        }

        private void OnBuildPortButtonPressed()
        {
            BuildMode = BuildMode.Port;
        }

        private void OnBuyTruckButtonPressed()
        {
            BuildMode = BuildMode.Truck;
        }

        private void OnBuyFreighterButtonPressed()
        {
            BuildMode = BuildMode.Freighter;
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