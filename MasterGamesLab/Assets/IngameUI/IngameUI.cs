using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(ConstructionControls))]
    public class IngameUI : MonoBehaviour
    {
        public static IngameUI Instance { get; private set; }

        private ConstructionControls constructionControls;
        private VisualElement root;
        private Button buildRoadButton, buildCanalButton, buildPortButton, buyTruckButton, buyFreighterButton, confirmButton, cancelButton, hideButton;
        private Button currentActiveButton;
        public const string activeClass = "ingame-build-button--active";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            constructionControls = GetComponent<ConstructionControls>();

            constructionControls.OnConstructionTypeChanged += HandleStateUIUpdate;

            buildRoadButton = root.Q<Button>("BuildRoadButton");
            buildCanalButton = root.Q<Button>("BuildCanalButton");
            buildPortButton = root.Q<Button>("BuildPortButton");
            buyTruckButton = root.Q<Button>("BuyTruckButton");
            buyFreighterButton = root.Q<Button>("BuyFreighterButton");
            confirmButton = root.Q<Button>("ConfirmButton");
            cancelButton = root.Q<Button>("CancelButton");
            hideButton = root.Q<Button>("HideButton");

            buildRoadButton.clicked += OnRoadClicked;
            buildCanalButton.clicked += OnCanalClicked;
            buildPortButton.clicked += OnPortClicked;
            buyTruckButton.clicked += OnTruckClicked;
            buyFreighterButton.clicked += OnFreighterClicked;
            confirmButton.clicked += OnConfirmPressed;
            cancelButton.clicked += OnCancelPressed;
            hideButton.clicked += OnHidePressed;
        }

        void OnDisable()
        {
            if (constructionControls != null)
                constructionControls.OnConstructionTypeChanged -= HandleStateUIUpdate;

            if (buildRoadButton == null) return;
            buildRoadButton.clicked -= OnRoadClicked;
            buildCanalButton.clicked -= OnCanalClicked;
            buildPortButton.clicked -= OnPortClicked;
            buyTruckButton.clicked -= OnTruckClicked;
            buyFreighterButton.clicked -= OnFreighterClicked;
            confirmButton.clicked -= OnConfirmPressed;
            cancelButton.clicked -= OnCancelPressed;
            hideButton.clicked -= OnHidePressed;
        }

        private void HandleStateUIUpdate(ConstructionControls.ConstructionType state)
        {
            if (state == ConstructionControls.ConstructionType.Hidden)
            {
                SetMenuVisibility(false);
                SetActiveButton(ConstructionControls.ConstructionType.None);
                SetActionButtonsVisibility(false);
            }
            else if (state == ConstructionControls.ConstructionType.None)
            {
                SetMenuVisibility(true);
                SetActiveButton(ConstructionControls.ConstructionType.None);
            }
            else
            {
                SetMenuVisibility(true);
                SetActiveButton(state);
                SetActionButtonsVisibility(true);
            }
        }

        // Keep your existing UI button styling logic completely identical below here...
        private void OnRoadClicked() => constructionControls.Type = ConstructionControls.ConstructionType.Road;
        private void OnCanalClicked() => constructionControls.Type = ConstructionControls.ConstructionType.Canal;
        private void OnPortClicked() => constructionControls.Type = ConstructionControls.ConstructionType.Port;
        private void OnTruckClicked() => constructionControls.Type = ConstructionControls.ConstructionType.Truck;
        private void OnFreighterClicked() => constructionControls.Type = ConstructionControls.ConstructionType.Freighter;
        public void OnConfirmPressed() => constructionControls.ConfirmConstruction();
        public void OnCancelPressed() => constructionControls.CancelConstruction();
        public void OnHidePressed() => constructionControls.ToggleHide();

        public void SetActiveButton(ConstructionControls.ConstructionType type)
        {
            currentActiveButton?.RemoveFromClassList(activeClass);

            currentActiveButton = type switch
            {
                ConstructionControls.ConstructionType.Road => buildRoadButton,
                ConstructionControls.ConstructionType.Canal => buildCanalButton,
                ConstructionControls.ConstructionType.Port => buildPortButton,
                ConstructionControls.ConstructionType.Freighter => buyFreighterButton,
                ConstructionControls.ConstructionType.Truck => buyTruckButton,
                _ => null
            };

            currentActiveButton?.AddToClassList(activeClass);
        }

        public void SetMenuVisibility(bool visible)
        {
            DisplayStyle style = visible ? DisplayStyle.Flex : DisplayStyle.None;

            buildCanalButton.style.display = style;
            buildRoadButton.style.display = style;
            buildPortButton.style.display = style;
            buyFreighterButton.style.display = style;
            buyTruckButton.style.display = style;
        }

        public void SetActionButtonsVisibility(bool visible)
        {
            DisplayStyle style = visible ? DisplayStyle.Flex : DisplayStyle.None;
            confirmButton.style.display = style;
            cancelButton.style.display = style;
        }

        public void Show() => root.style.display = DisplayStyle.Flex;
        public void Hide() => root.style.display = DisplayStyle.None;
    }
}