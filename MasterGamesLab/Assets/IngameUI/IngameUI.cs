using System.Collections;
using Map.Blueprint;
using Player;
using UnityEngine;
using UnityEngine.UIElements;

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
        ShrinkWrapContainer blueprintCountContainer;

        private Label totalCostLabel;

        public override MenuId Id => MenuId.Ingame;

        private VisualElement playersContainer;
        private Coroutine uiUpdateCoroutine;
        private VisualElement tabMenu;


        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void Update()
        {
            Map.Map.Instance.GetPlayerStats();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            constructionControls = GetComponent<ConstructionControls>();
            constructionControls.OnConstructionTypeChanged += HandleStateUIUpdate;

            Map.Map.Instance.Blueprint.OnChanged += HandleBlueprintUpdate;

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
            blueprintCountContainer = root.Q<ShrinkWrapContainer>("BlueprintCountContainer");
            totalCostLabel = root.Q<Label>("TotalCost");
            playersContainer = root.Q<VisualElement>("players-container");
            tabMenu = root.Q<VisualElement>("TabMenu");

            UpdateAllPlayerStats();
            uiUpdateCoroutine = StartCoroutine(PeriodicUiUpdateLoop());

            blueprintCountContainer.style.display = DisplayStyle.None;

            buildRoadButton.clicked += OnRoadClicked;
            buildCanalButton.clicked += OnCanalClicked;
            buildPortButton.clicked += OnPortClicked;
            buyTruckButton.clicked += OnTruckClicked;
            buyFreighterButton.clicked += OnFreighterClicked;
            confirmButton.clicked += OnConfirmPressed;
            cancelButton.clicked += OnCancelPressed;
            hideButton.clicked += OnHidePressed;

            Player.Player.OnPlayerChanged += ChangePlayerInfo;
        }

        void OnDisable()
        {
            Map.Map.Instance.Blueprint.OnChanged -= HandleBlueprintUpdate;
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

            if (uiUpdateCoroutine != null)
            {
                StopCoroutine(uiUpdateCoroutine);
            }

            Player.Player.OnPlayerChanged -= ChangePlayerInfo;
        }

        private IEnumerator PeriodicUiUpdateLoop()
        {
            WaitForSeconds delay = new WaitForSeconds(5.0f);

            while (true)
            {
                UpdateAllPlayerStats();
                yield return delay;
            }
        }

        private void UpdateAllPlayerStats()
        {
            PlayerStats[] allStats = Map.Map.Instance.GetPlayerStats();
            if (allStats.Length == 0 || playersContainer == null) return;

            for (int i = 0; i < playersContainer.childCount; i++)
            {
                if (i >= allStats.Length) break;

                VisualElement rowInstance = playersContainer[i];
                PlayerStats stats = allStats[i];

                UpdatePlayerRowData(rowInstance, stats);
            }
        }

        private void UpdatePlayerRowData(VisualElement row, PlayerStats stats)
        {
            row.Q<ResponsiveLabel>("Name").text = stats.Name;
            row.Q<ResponsiveLabel>("MarketCap").text = stats.MarketCap.ToString();
            row.Q<ResponsiveLabel>("Cash").text = stats.Cash.ToString();
            row.Q<ResponsiveLabel>("Trucks").text = stats.TruckCount.ToString();
            row.Q<ResponsiveLabel>("Freighters").text = stats.FreighterCount.ToString();
            row.Q<ResponsiveLabel>("Roads").text = stats.RoadCount.ToString();
            row.Q<ResponsiveLabel>("Canals").text = stats.CanalCount.ToString();
            row.Q<ResponsiveLabel>("Ports").text = stats.PortCount.ToString();
        }

        public void ShowTabMenu(bool visible)
        {
            Visibility style = visible ? Visibility.Visible : Visibility.Hidden;
            tabMenu.style.visibility = style;
        }



        public void ChangePlayerInfo(Player.Player player)
        {
            if (player.IsSelf)
            {
                moneyLabel.text = "MONEY: " + player.Cash;
            }
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

        private void setTotalCost(int cost)
        {
            totalCostLabel.text = "Total Cost: " + cost;
        }


        private void HandleBlueprintUpdate(Blueprint blueprint)
        {
            if (blueprint.IsEmpty)
            {
                blueprintCountContainer.style.display = DisplayStyle.None;
                return;
            }
            blueprintCountContainer.style.display = DisplayStyle.Flex;
            var details = blueprint.GetDetails();
            var objectInfos = details.ObjectInfos;

            foreach (ConstructibleType type in System.Enum.GetValues(typeof(ConstructibleType)))
            {
                int count = objectInfos.TryGetValue(type, out var info) ? info.Count : 0;

                UpdateBlueprintCount(type, count);
            }
            setTotalCost(details.TotalCost);

            blueprintCountContainer.schedule.Execute(() =>
            {
                blueprintCountContainer.RecalculateHeight();
            }).ExecuteLater(1);
        }

        public void UpdateBlueprintCount(ConstructibleType type, int count)
        {
            string elementName = type switch
            {
                ConstructibleType.Road => "RoadCount",
                ConstructibleType.Canal => "CanalCount",
                ConstructibleType.Port => "PortCount",
                ConstructibleType.Truck => "TruckCount",
                ConstructibleType.Freighter => "FreighterCount",
                _ => null
            };

            if (string.IsNullOrEmpty(elementName)) return;

            var countContainer = blueprintCountContainer.Q<VisualElement>(elementName);

            Label countLabel = countContainer.Q<Label>();

            if (count > 0)
            {
                countLabel.text = count.ToString();
                countContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                countContainer.style.display = DisplayStyle.None;
            }
        }

    }
}