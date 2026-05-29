
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
        private Button confirmButton;
        private Button cancelButton;
        private Button hideButton;


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
                    buildMode = BuildMode.None;
                    currentActiveButton = null;
                    return;
                }

                if (value == BuildMode.Hidden)
                {
                    SetMenuVisibility(false);
                    currentActiveButton = null;
                }
                else
                {
                    if (buildMode == BuildMode.Hidden) SetMenuVisibility(true);

                    currentActiveButton = value switch
                    {
                        BuildMode.Road => buildRoadButton,
                        BuildMode.Canal => buildCanalButton,
                        BuildMode.Port => buildPortButton,
                        BuildMode.Freighter => buyFreighterButton,
                        BuildMode.Truck => buyTruckButton,
                        _ => null
                    };

                    currentActiveButton?.AddToClassList(activeClass);
                }

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
            confirmButton = root.Q<Button>("ConfirmButton");
            cancelButton = root.Q<Button>("CancelButton");
            hideButton = root.Q<Button>("HideButton");


            buildRoadButton.clicked += OnBuildRoadButtonPressed;
            buildCanalButton.clicked += OnBuildCanalButtonPressed;
            buildPortButton.clicked += OnBuildPortButtonPressed;
            buyTruckButton.clicked += OnBuyTruckButtonPressed;
            buyFreighterButton.clicked += OnBuyFreighterButtonPressed;
            confirmButton.clicked += OnConfirmPressed;
            cancelButton.clicked += OnCancelPressed;
            hideButton.clicked += OnHidePressed;



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
        private void OnConfirmPressed()
        {
            BuildMode = BuildMode.None;
            confirmButton.style.display = DisplayStyle.None;
            cancelButton.style.display = DisplayStyle.None;
        }
        private void OnCancelPressed()
        {
            BuildMode = BuildMode.None;

            confirmButton.style.display = DisplayStyle.None;
            cancelButton.style.display = DisplayStyle.None;

        }
        private void OnHidePressed()
        {
            BuildMode = BuildMode == BuildMode.Hidden ? BuildMode.None : BuildMode.Hidden;
        }


        private void SetMenuVisibility(bool visible)
        {
            DisplayStyle style = visible ? DisplayStyle.Flex : DisplayStyle.None;

            confirmButton.style.display = style;
            cancelButton.style.display = style;
            buildCanalButton.style.display = style;
            buildRoadButton.style.display = style;
            buildPortButton.style.display = style;
            buyFreighterButton.style.display = style;
            buyTruckButton.style.display = style;
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