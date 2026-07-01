using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;
using UnityEngine.UIElements;
using static UI.IngameUI;

namespace UI
{
    public class GameFinishedUI : Menu
    {
        public ResponsiveButton returnToStartButton;
        public ResponsiveButton playAgainButton;

        private ResponsiveLabel titleLabel;
        private VisualElement playersContainer;
        private List<ResponsiveButton> headerButtons = new List<ResponsiveButton>();
        private PlayerStats[] finalPlayerStats;
        private bool? playerWonStatus = null;

        private SortColumn currentSortColumn = SortColumn.Name;

        public override MenuId Id => MenuId.GameFinished;

        protected override void OnEnable()
        {
            base.OnEnable();

            returnToStartButton = root.Q<ResponsiveButton>("ReturnToStart");
            playAgainButton = root.Q<ResponsiveButton>("PlayAgain");
            titleLabel = root.Q<ResponsiveLabel>("title");

            returnToStartButton.clicked += OnReturnToStartPressed;
            playAgainButton.clicked += OnPlayAgainPressed;

            var statsContainer = root.Q<VisualElement>("Stats");
            playersContainer = root.Q<VisualElement>("players-container");

            SetupHeaderButtons(statsContainer);
        }

        void OnDisable()
        {
            returnToStartButton.clicked -= OnReturnToStartPressed;
            playAgainButton.clicked -= OnPlayAgainPressed;
        }

        public void SetFinalStats(PlayerStats[] stats, bool hasWon)
        {
            finalPlayerStats = stats;
            playerWonStatus = hasWon;

            UpdateTitleDisplay();
            UpdateAllPlayerStats();
        }

        private void UpdateTitleDisplay()
        {
            if (playerWonStatus.Value)
            {
                titleLabel.text = "Victory";
                titleLabel.style.color = Color.green;
            }
            else
            {
                titleLabel.text = "Defeat";
                titleLabel.style.color = Color.red;
            }
        }

        private async void OnReturnToStartPressed()
        {
            await UIManager.Instance.LeaveLobbyAsync();
        }

        private void OnPlayAgainPressed()
        {
            UIManager.Instance.CurrentMenu = MenuId.Lobby;
        }

        private void SetupHeaderButtons(VisualElement container)
        {
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
        }

        private void UpdateAllPlayerStats()
        {

            var sortedStats = currentSortColumn switch
            {
                SortColumn.Name => finalPlayerStats.OrderBy(s => s.Id).ToList(),
                SortColumn.MarketCap => finalPlayerStats.OrderByDescending(s => s.MarketCap).ToList(),
                SortColumn.Cash => finalPlayerStats.OrderByDescending(s => s.Cash).ToList(),
                SortColumn.Trucks => finalPlayerStats.OrderByDescending(s => s.TruckCount).ToList(),
                SortColumn.Freighters => finalPlayerStats.OrderByDescending(s => s.FreighterCount).ToList(),
                SortColumn.Roads => finalPlayerStats.OrderByDescending(s => s.RoadCount).ToList(),
                SortColumn.Canals => finalPlayerStats.OrderByDescending(s => s.CanalCount).ToList(),
                SortColumn.Ports => finalPlayerStats.OrderByDescending(s => s.PortCount).ToList(),
                _ => finalPlayerStats.OrderBy(s => s.Id).ToList(),
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

        private void HighliteHoveredColumn(SortColumn column)
        {
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
                row.Query<Label>().ForEach(label => label.RemoveFromClassList(hoveredColumnClass));
            }
        }
    }
}