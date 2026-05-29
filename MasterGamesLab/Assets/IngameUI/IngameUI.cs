using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    public class IngameUI : MonoBehaviour
    {
        public static IngameUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private VisualElement root;
        private Button buildRoadButton;
        private Button buildCanalButton;
        private Button buildPortButton;
        private Button buyTruckButton;
        private Button buyFreighterButton;
        private Button confirmButton;
        private Button cancelButton;
        private Button hideButton;

        private Button currentActiveButton;
        private BuildMode buildMode = BuildMode.None;

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

            buildRoadButton.clicked += () => BuildMode = BuildMode.Road;
            buildCanalButton.clicked += () => BuildMode = BuildMode.Canal;
            buildPortButton.clicked += () => BuildMode = BuildMode.Port;
            buyTruckButton.clicked += () => BuildMode = BuildMode.Truck;
            buyFreighterButton.clicked += () => BuildMode = BuildMode.Freighter;

            confirmButton.clicked += OnConfirmPressed;
            cancelButton.clicked += OnCancelPressed;
            hideButton.clicked += OnHidePressed;
        }

        void OnDisable()
        {
            if (buildRoadButton == null) return;

            buildRoadButton.clicked -= () => BuildMode = BuildMode.Road;
            buildCanalButton.clicked -= () => BuildMode = BuildMode.Canal;
            buildPortButton.clicked -= () => BuildMode = BuildMode.Port;
            buyTruckButton.clicked -= () => BuildMode = BuildMode.Truck;
            buyFreighterButton.clicked -= () => BuildMode = BuildMode.Freighter;

            confirmButton.clicked -= OnConfirmPressed;
            cancelButton.clicked -= OnCancelPressed;
            hideButton.clicked -= OnHidePressed;
        }

        public void OnLeftClickPressed()
        {
            if(BuildMode == BuildMode.Road || BuildMode == BuildMode.Canal)
            {

            }
        }

        public void OnConfirmPressed()
        {
            BuildMode = BuildMode.None;
            SetActionButtonsVisibility(false);
        }

        public void OnCancelPressed()
        {
            BuildMode = BuildMode.None;
            SetActionButtonsVisibility(false);
        }

        public void OnHidePressed()
        {
            BuildMode = (BuildMode == BuildMode.Hidden) ? BuildMode.None : BuildMode.Hidden;
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

        private void SetActionButtonsVisibility(bool visible)
        {
            DisplayStyle style = visible ? DisplayStyle.Flex : DisplayStyle.None;
            confirmButton.style.display = style;
            cancelButton.style.display = style;
        }

        public void Show() => root.style.display = DisplayStyle.Flex;
        public void Hide() => root.style.display = DisplayStyle.None;
    }
}