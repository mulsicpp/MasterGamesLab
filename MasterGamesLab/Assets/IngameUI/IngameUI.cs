using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InGameCamera;
using Map.Blueprint;
using Map.Fleet;
using Map.Hoverables;
using Map.Infrastructure;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(ConstructionControls))]
    [RequireComponent(typeof(VehicleControls))]
    public class IngameUI : Menu, IClickEventHandler
    {
        [Serializable]
        public struct GoodImagePair
        {
            public Good GoodType;
            public Sprite ImageAsset;
        }

        [SerializeField]
        private List<GoodImagePair> goodsConfiguration = new List<GoodImagePair>();

        public Dictionary<Good, Sprite> goodsImages = new Dictionary<Good, Sprite>();

        public static IngameUI Instance { get; private set; }
        public override MenuId Id => MenuId.Ingame;
        public bool IsHovered = false;

        // --- Configuration Constants ---
        public const string activeClass = "ingame-build-button--active";
        public const string activeColumnClass = "tab-menu-active-column";
        public const string hoveredColumnClass = "tab-menu-hovered-row";

        public const HoverablePicker.HoverableLayer DEFAULT_HOVERABLE_LAYERS = HoverablePicker.HoverableLayer.All;

        // --- Sorting Enums & Variables ---
        private enum SortColumn { Name, MarketCap, Cash, Trucks, Freighters, Roads, Canals, Ports }
        private SortColumn currentSortColumn = SortColumn.Name;

        // --- Dependencies & Coroutines ---
        public VehicleControls VehicleControls { get; private set; }
        public ConstructionControls ConstructionControls { get; private set; }
        private Coroutine uiUpdateCoroutine;

        // --- UI Toolkit Elements ---
        private VisualElement tabMenu;
        private VisualElement playersContainer;
        private ShrinkWrapContainer container;
        private ShrinkWrapContainer blueprintCountContainer;
        private GroupBox div;

        // --- Labels & Buttons ---
        private Label moneyLabel;
        private Label totalCostLabel;
        private Button buildRoadButton, buildCanalButton, buildPortButton;
        private Button buyTruckButton, buyFreighterButton;
        private Button confirmButton, cancelButton;
        public Button hideButton;
        private Button currentActiveButton;
        private GroupBox buildCount;

        [SerializeField] public Sprite hide, hidden;


        protected PlanetCameraController mainCamera;


        private IControls[] controls;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

        }
        private void Start()
        {
            foreach (var pair in goodsConfiguration)
            {
                if (!goodsImages.ContainsKey(pair.GoodType))
                {
                    goodsImages.Add(pair.GoodType, pair.ImageAsset);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            OnBecameVisible += BecameVisible;
            OnBecameHidden += BecameHidden;

            // 1. Resolve Dependencies
            VehicleControls = GetComponent<VehicleControls>();

            ConstructionControls = GetComponent<ConstructionControls>();
            ConstructionControls.OnConstructionTypeChanged += HandleStateUIUpdate;

            controls = new IControls[] { VehicleControls, ConstructionControls };

            Player.Player.OnPlayerChanged += ChangePlayerInfo;

            // 2. Query Visual Elements
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
            buildCount = root.Q<GroupBox>("BuildCount");

            // 3. Setup Sorting Header Events
            var headerRow = root.Q<VisualElement>("header-row");
            var headers = headerRow.Query<ResponsiveButton>().ToList();
            //headers[0].clicked += () => SetSortTarget(SortColumn.Name);
            headers[1].clicked += () => SetSortTarget(SortColumn.MarketCap, headers, 1);
            headers[2].clicked += () => SetSortTarget(SortColumn.Cash, headers, 2);
            headers[3].clicked += () => SetSortTarget(SortColumn.Trucks, headers, 3);
            headers[4].clicked += () => SetSortTarget(SortColumn.Freighters, headers, 4);
            headers[5].clicked += () => SetSortTarget(SortColumn.Roads, headers, 5);
            headers[6].clicked += () => SetSortTarget(SortColumn.Canals, headers, 6);
            headers[7].clicked += () => SetSortTarget(SortColumn.Ports, headers, 7);

            headers[1].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.MarketCap));
            headers[2].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Cash));
            headers[3].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Trucks));
            headers[4].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Freighters));
            headers[5].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Roads));
            headers[6].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Canals));
            headers[7].RegisterCallback<PointerEnterEvent>(e => HighliteHoveredColumn(SortColumn.Ports));

            for (int i = 1; i < headers.Count; i++)
            {
                headers[i].RegisterCallback<PointerLeaveEvent>(e => ClearHoveredColumns());
            }

            // 4. Setup Interaction Button Events
            buildRoadButton.clicked += OnRoadClicked;
            buildCanalButton.clicked += OnCanalClicked;
            buildPortButton.clicked += OnPortClicked;
            buyTruckButton.clicked += OnTruckClicked;
            buyFreighterButton.clicked += OnFreighterClicked;
            confirmButton.clicked += OnConfirmPressed;
            cancelButton.clicked += OnCancelPressed;
            hideButton.clicked += OnHidePressed;

            // 5. Initialize States & Loops
            blueprintCountContainer.style.display = DisplayStyle.None;
            UpdateAllPlayerStats();
            uiUpdateCoroutine = StartCoroutine(PeriodicUiUpdateLoop());

            container.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            container.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
            blueprintCountContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            blueprintCountContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
            tabMenu.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            tabMenu.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);


            mainCamera = MainCamera.Instance.GetComponentInChildren<PlanetCameraController>();
        }

        void OnDisable()
        {
            if (ConstructionControls != null)
                ConstructionControls.OnConstructionTypeChanged -= HandleStateUIUpdate;

            Player.Player.OnPlayerChanged -= ChangePlayerInfo;

            if (uiUpdateCoroutine != null)
            {
                StopCoroutine(uiUpdateCoroutine);
            }

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

        private void BecameVisible()
        {
            foreach (var c in controls)
            {
                c.DisableControls();
            }

            Map.Map.Instance.Blueprint.OnChanged += HandleBlueprintUpdate;

            // Map.Map.Instance.enabled = true;
            // TODO enable ingame actions
        }

        private void BecameHidden()
        {
            if (Map.Map.Instance.Blueprint != null)
                Map.Map.Instance.Blueprint.OnChanged -= HandleBlueprintUpdate;
            // Map.Map.Instance.enabled = false;
            // TODO disable ingame actions
        }

        private void Update()
        {
            if (IsHovered)
            {
                Map.Map.Instance.CurrentlyHovered = null;
                HoverablePicker.Instance.DenyPick = true;
            }

            Map.Map.Instance.HoverLayers = controls.FirstOrDefault(c => c.ControlsAreActive)?.SelectHoverableLayers() ?? DEFAULT_HOVERABLE_LAYERS;

            Map.Map.Instance.HoverOutliner.HoverState = HoverState.Valid;
            foreach (var c in controls)
                c.UpdateControls();

            if (IngameInputs.selectClickAction.WasPerformedThisFrame())
                HandleClick(ClickEventType.Select);
            if (IngameInputs.cancelClickAction.WasPressedThisFrame())
                HandleClick(ClickEventType.CancelPressed);
            if (IngameInputs.cancelClickAction.WasReleasedThisFrame())
                HandleClick(ClickEventType.CancelReleased);
        }

        public bool HandleClick(ClickEventType type)
        {
            foreach (var handler in controls)
            {
                if (handler.HandleClick(type)) return true;
            }
            return false;
        }

        #endregion

        #region Tab Menu Sorting Logic

        private void HighliteHoveredColumn(SortColumn column)
        {
            // First, strip old hover classes so columns don't stack highlights
            ClearHoveredColumns();

            string elementName = column switch
            {
                SortColumn.MarketCap => "MarketCap",
                SortColumn.Cash => "Cash",
                SortColumn.Trucks => "Trucks",
                SortColumn.Freighters => "Freighters",
                SortColumn.Roads => "Roads",
                SortColumn.Canals => "Canals",
                SortColumn.Ports => "Ports",
                _ => null
            };

            if (elementName == null) return;

            for (int i = 0; i < playersContainer.childCount; i++)
            {
                VisualElement row = playersContainer[i];
                row.Q<Label>(elementName)?.AddToClassList(hoveredColumnClass);
            }
        }

        private void ClearHoveredColumns()
        {
            for (int i = 0; i < playersContainer.childCount; i++)
            {
                VisualElement row = playersContainer[i];

                // Find all labels within this row and strip the hover class
                row.Query<Label>().ForEach(label =>
                {
                    label.RemoveFromClassList(hoveredColumnClass);
                });
            }
        }

        private void SetSortTarget(SortColumn column, List<ResponsiveButton> buttons, int clicked)
        {
            foreach (var button in buttons)
            {
                button.Q<VisualElement>("Icon")?.RemoveFromClassList(activeColumnClass);
            }
            if (column == currentSortColumn)
                currentSortColumn = SortColumn.Name;
            else
            {
                currentSortColumn = column;
                buttons[clicked].Q<VisualElement>("Icon").AddToClassList(activeColumnClass);
            }
            UpdateAllPlayerStats();
        }

        private IEnumerator PeriodicUiUpdateLoop()
        {
            WaitForSeconds delay = new WaitForSeconds(1.0f);
            while (true)
            {
                UpdateAllPlayerStats();
                yield return delay;
            }
        }

        private void UpdateAllPlayerStats()
        {
            PlayerStats[] rawStats = Map.Map.Instance.GetPlayerStats();
            if (rawStats == null || rawStats.Length == 0 || playersContainer == null) return;

            var sortedStats = currentSortColumn switch
            {
                SortColumn.Name => Enumerable.ToList(Enumerable.OrderBy(rawStats, s => s.Id)),
                SortColumn.MarketCap => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.MarketCap)),
                SortColumn.Cash => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.Cash)),
                SortColumn.Trucks => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.TruckCount)),
                SortColumn.Freighters => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.FreighterCount)),
                SortColumn.Roads => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.RoadCount)),
                SortColumn.Canals => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.CanalCount)),
                SortColumn.Ports => Enumerable.ToList(Enumerable.OrderByDescending(rawStats, s => s.PortCount)),
                _ => Enumerable.ToList(Enumerable.OrderBy(rawStats, s => s.Id))
            };

            for (int i = 0; i < playersContainer.childCount; i++)
            {
                if (i >= sortedStats.Count) break;

                VisualElement rowInstance = playersContainer[i];
                PlayerStats stats = sortedStats[i];

                UpdatePlayerRowData(rowInstance, stats);
            }
        }

        private void UpdatePlayerRowData(VisualElement row, PlayerStats stats)
        {
            var nameLabel = row.Q<ResponsiveLabel>("Name");
            var marketCapLabel = row.Q<ResponsiveLabel>("MarketCap");
            var cashLabel = row.Q<ResponsiveLabel>("Cash");
            var trucksLabel = row.Q<ResponsiveLabel>("Trucks");
            var freightersLabel = row.Q<ResponsiveLabel>("Freighters");
            var roadsLabel = row.Q<ResponsiveLabel>("Roads");
            var canalsLabel = row.Q<ResponsiveLabel>("Canals");
            var portsLabel = row.Q<ResponsiveLabel>("Ports");

            nameLabel.text = stats.Name;
            nameLabel.style.color = stats.Color;
            marketCapLabel.text = stats.MarketCap.ToString();
            cashLabel.text = stats.Cash.ToString();
            trucksLabel.text = stats.TruckCount.ToString();
            freightersLabel.text = stats.FreighterCount.ToString();
            roadsLabel.text = stats.RoadCount.ToString();
            canalsLabel.text = stats.CanalCount.ToString();
            portsLabel.text = stats.PortCount.ToString();

            marketCapLabel.RemoveFromClassList(activeColumnClass);
            cashLabel.RemoveFromClassList(activeColumnClass);
            trucksLabel.RemoveFromClassList(activeColumnClass);
            freightersLabel.RemoveFromClassList(activeColumnClass);
            roadsLabel.RemoveFromClassList(activeColumnClass);
            canalsLabel.RemoveFromClassList(activeColumnClass);
            portsLabel.RemoveFromClassList(activeColumnClass);

            ResponsiveLabel targetSortedLabel = currentSortColumn switch
            {
                SortColumn.MarketCap => marketCapLabel,
                SortColumn.Cash => cashLabel,
                SortColumn.Trucks => trucksLabel,
                SortColumn.Freighters => freightersLabel,
                SortColumn.Roads => roadsLabel,
                SortColumn.Canals => canalsLabel,
                SortColumn.Ports => portsLabel,
                _ => null
            };

            targetSortedLabel?.AddToClassList(activeColumnClass);
        }

        #endregion

        #region Blueprint UI Controls

        private void HandleBlueprintUpdate(Blueprint blueprint)
        {

            if (blueprint.IsEmpty)
            {
                confirmButton.style.display = DisplayStyle.None;
                cancelButton.style.display = DisplayStyle.None;
                blueprintCountContainer.style.display = DisplayStyle.None;
                return;
            }

            blueprintCountContainer.style.display = DisplayStyle.Flex;
            var details = blueprint.GetDetails();
            var objectInfos = details.ObjectInfos;
            cancelButton.style.display = DisplayStyle.Flex;

            confirmButton.style.display = Player.Player.Self.Cash >= details.TotalCost ? DisplayStyle.Flex : DisplayStyle.None;



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

        private void setTotalCost(int cost)
        {
            totalCostLabel.text = "Total Cost: " + cost;
        }

        #endregion

        #region Construction Infrastructure Interaction Callbacks

        private void OnRoadClicked() => ConstructionControls.Type = ConstructionControls.ConstructionType.Road;
        private void OnCanalClicked() => ConstructionControls.Type = ConstructionControls.ConstructionType.Canal;
        private void OnPortClicked() => ConstructionControls.Type = ConstructionControls.ConstructionType.Port;
        private void OnTruckClicked() => ConstructionControls.Type = ConstructionControls.ConstructionType.Truck;
        private void OnFreighterClicked() => ConstructionControls.Type = ConstructionControls.ConstructionType.Freighter;
        public void OnConfirmPressed() => ConstructionControls.ConfirmConstruction();
        public void OnCancelPressed() => ConstructionControls.CancelConstruction();
        public void OnHidePressed() => ConstructionControls.ToggleHide();

        public void SelectNextVehicle()
        {
            var current = VehicleControls.SelectedVehicle;
            Vehicle nextVehicle = null;

            Func<Vehicle, bool> condition = v => v.Exists && v.Owner.IsSelf && (v as Truck)?.Freighter == null;

            if (current != null)
            {
                nextVehicle = Map.Map.Instance.Fleet.Vehicles.FirstOrDefault(v => condition(v) && v.IndexInVehicles > current.IndexInVehicles);
            }

            if (nextVehicle == null)
                nextVehicle = Map.Map.Instance.Fleet.Vehicles.FirstOrDefault(condition);

            mainCamera.CenterOnPosition(nextVehicle.Transform.Position);
            VehicleControls.SelectedVehicle = nextVehicle;
        }

        public void SelectVehicleBySlot(Vehicle.VehicleType type, int slotIndex)
        {
            Vehicle v;
            if (type == Vehicle.VehicleType.Truck)
            {
                v = Map.Map.Instance.Fleet.Trucks[Player.Player.SelfId * Constants.MAX_TRUCKS_PER_PLAYER + slotIndex];
                if ((v as Truck).Freighter != null)
                    v = (v as Truck).Freighter;
            }
            else
                v = Map.Map.Instance.Fleet.Freighters[Player.Player.SelfId * Constants.MAX_FREIGHTERS_PER_PLAYER + slotIndex];


            if (v.Exists)
            {
                VehicleControls.SelectedVehicle = v;

                if (v.Transform != null)
                {
                    mainCamera.CenterOnPosition(v.Transform.Position);
                }
            }
        }

        #endregion

        #region General View Visibility & Layout Formatting

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

            buildCount.style.visibility = style;

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

        private void OnMouseEnterElement(MouseEnterEvent evt)
        {
            IsHovered = true;
            Map.Map.Instance.CurrentlyHovered = null;
            HoverablePicker.Instance.DenyPick = true;
        }

        private void OnMouseLeaveElement(MouseLeaveEvent evt)
        {
            IsHovered = false;
        }

        #endregion
    }
}