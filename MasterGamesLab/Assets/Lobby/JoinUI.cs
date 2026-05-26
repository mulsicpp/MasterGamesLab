using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using WebSocketSharp;
using Unity.Services.Lobbies;

public class JoinUI : MonoBehaviour
{
    public ResponsiveTextField lobbyCodeInput;
    public ListView lobbyList;
    [SerializeField] private VisualTreeAsset lobbyRowTemplate;


    [SerializeField] private StartUI startUI;
    [SerializeField] private LobbyUI lobbyUI;

    VisualElement root;
    public Button backButton;
    public Button joinButton;
    public LoadingButton refreshButton;


    private void OnEnable()
    {



        root = GetComponent<UIDocument>().rootVisualElement;

        backButton = root.Q<Button>("BackButton");
        joinButton = root.Q<Button>("JoinButton");
        refreshButton = root.Q<LoadingButton>("RefreshButton");

        lobbyCodeInput = root.Q<ResponsiveTextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;
        refreshButton.clicked += OnRefreshPressed;


        lobbyList = root.Q<ListView>("LobbyList");

        // for (int i = 0; i < 10; i++)
        // {

        //     CreateLobbyOptions options = new CreateLobbyOptions
        //     {
        //         IsPrivate = false,
        //         // Data = new Dictionary<string, DataObject> {
        //         // {
        //         //     "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
        //         // }
        //         // },
        //         Player = new Player
        //         {
        //             Data = new Dictionary<string, PlayerDataObject> {
        //         {
        //             "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "PlayerName")
        //         }
        //     }
        //         }
        //     };

        //     LobbyService.Instance.CreateLobbyAsync("PlayerName" + "'s Lobby", 4, options);
        // }

        SetupLobbyList();

        Hide();
    }

    private void OnDisable()
    {
        backButton.clicked -= OnBackPressed;
        joinButton.clicked -= OnJoinPressed;
        refreshButton.clicked -= OnRefreshPressed;
    }

    private void OnBackPressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        LobbyLogic.Instance.ShowStartUI();
    }

    private async void OnJoinPressed()
    {
        if (!lobbyCodeInput.Value.IsNullOrEmpty())
            await LobbyLogic.Instance.JoinLobbyByCode(lobbyCodeInput.Value);
    }

    private async void OnRefreshPressed()
    {
        Debug.Log(LobbyLogic.Instance.PublicLobbies.Count);
        refreshButton.SetEnabled(false);
        refreshButton.SetLoading(true);
        await LobbyLogic.Instance.LoadPublicLobbies();
        refreshButton.SetLoading(false);
        refreshButton.SetEnabled(true);
    }

    private void SetupLobbyList()
    {
        lobbyList.itemsSource = LobbyLogic.Instance.PublicLobbies;

        lobbyList.makeItem = () => lobbyRowTemplate.Instantiate();

        lobbyList.bindItem = (VisualElement element, int index) =>
        {
            Lobby lobbyData = LobbyLogic.Instance.PublicLobbies[index];

            var nameLabel = element.Q<ResponsiveLabel>("LobbyName");
            var countLabel = element.Q<ResponsiveLabel>("PlayerCount");
            var rowJoinButton = element.Q<ResponsiveButton>("Join");

            if (nameLabel != null) nameLabel.text = lobbyData.Name;
            if (countLabel != null) countLabel.text = $"{lobbyData.Players.Count}/{lobbyData.MaxPlayers}";

            lobbyList.RegisterCallback<GeometryChangedEvent>(OnListGeometryChanged);

            if (rowJoinButton != null)
            {
                rowJoinButton.clickable = null;
                rowJoinButton.clicked += async () =>
                {
                    await LobbyLogic.Instance.JoinLobbyById(lobbyData.Id);
                };
            }
        };
    }

    public void Show()
    {
        root.style.display = DisplayStyle.Flex;
        lobbyCodeInput.schedule.Execute(() => lobbyCodeInput.Focus());
    }

    public void Hide()
    {
        root.style.display = DisplayStyle.None;
    }


    private void OnListGeometryChanged(GeometryChangedEvent evt)
    {
        float visibleHeight = evt.newRect.height;

        if (visibleHeight > 0)
        {
            float targetRowHeight = visibleHeight / 3f;

            // 1. Assign the new target height tracking rule
            lobbyList.fixedItemHeight = Mathf.RoundToInt(targetRowHeight);

            // 2. FORCE the virtualized visual elements to rebuild their layout bounds
            lobbyList.Rebuild();
        }
    }
}