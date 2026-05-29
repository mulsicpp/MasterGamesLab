
using System;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
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


        [SerializeField] private InputActionAsset inputActions;
        private InputActionMap controlsActionMap;
        private InputAction leftClickAction;

        bool showpath = false;

        private const string activeClass = "ingame-build-button--active";

        public BuildMode BuildMode
        {
            get => buildMode;
            set
            {
                currentActiveButton?.RemoveFromClassList(activeClass);
                if (buildMode == value)
                {
                    BuildMode = BuildMode.None;
                    return;
                }
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

            controlsActionMap = inputActions.FindActionMap("Controls");
            leftClickAction = controlsActionMap.FindAction("LeftClick");
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

        private void buildRoad(InputAction.CallbackContext context)
        {
            if (BuildMode != BuildMode.Road)
                return;
            showpath = true;
        }

        public void Show()
        {
            root.style.display = DisplayStyle.Flex;
            leftClickAction.started += buildRoad;
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
            leftClickAction.started -= buildRoad;
        }
    }
}