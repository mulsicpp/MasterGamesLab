using UnityEngine;
using UnityEngine.UIElements;
using static ConstructionControls;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(ConstructionControls))]
    public class IngameUI : Menu
    {
        public static IngameUI Instance { get; private set; }

        private ConstructionControls constructionControls;

        private Button buildRoadButton, buildCanalButton, buildPortButton, buyTruckButton, buyFreighterButton, confirmButton, cancelButton, hideButton;
        private Button currentActiveButton;
        private Label moneyLabel;
        private ShrinkWrapContainer container;
        private GroupBox div;
        public const string activeClass = "ingame-build-button--active";
        VisualElement blueprintCountContainer;

        public override MenuId Id => MenuId.Ingame;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

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
            moneyLabel = root.Q<Label>("MONEY");
            container = root.Q<ShrinkWrapContainer>("Container");
            div = root.Q<GroupBox>("Devider");
            blueprintCountContainer = root.Q<VisualElement>("BlueprintCountContainer");




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



        public void setMoney(ulong money)
        {
            moneyLabel.text = "MONEY: " + money;
        }

        private void HandleStateUIUpdate(ConstructionControls.ConstructionType state)
        {
            if (state == ConstructionControls.ConstructionType.Hidden)
            {
                SetMenuVisibility(false);
                SetActiveButton(ConstructionControls.ConstructionType.None);
            }
            else
            {
                SetMenuVisibility(true);
                SetActiveButton(state);
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
            Visibility style = visible ? Visibility.Visible : Visibility.Hidden;

            var buildButtonsGroup = root.Q<GroupBox>("BuildButtons");
            buildButtonsGroup.style.visibility = style;

            div.style.visibility = style;

            var confirmSlot = confirmButton.parent;
            confirmSlot.style.visibility = style;

            var cancelSlot = cancelButton.parent;
            cancelSlot.style.visibility = style;

            if (visible)
                container.RemoveFromClassList("container-hidden");
            else
                container.AddToClassList("container-hidden");
        }


        public void UpdateBlueprintCount(ConstructionType type, int count)
        {
            string elementName = type switch
            {
                ConstructionType.Road => "RoadCount",
                ConstructionType.Canal => "CanalCount",
                ConstructionType.Port => "PortCount",
                ConstructionType.Truck => "TruckCount",
                ConstructionType.Freighter => "FreighterCount",
                _ => null
            };

            if (string.IsNullOrEmpty(elementName)) return;

            var countContainer = blueprintCountContainer.Q<VisualElement>(elementName);

            if (countContainer != null)
            {
                // 3. Find the Label element nested inside your BlueprintCount template instance
                Label countLabel = countContainer.Q<Label>();
                if (countLabel != null)
                {
                    // Update the text safely
                    countLabel.text = count.ToString();
                }
                else
                {
                    Debug.LogWarning($"Label not found inside template instance for: {elementName}");
                }
            }
        }

    }
}