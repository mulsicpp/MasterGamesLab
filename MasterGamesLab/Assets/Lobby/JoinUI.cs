using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using WebSocketSharp;

public class JoinUI : MonoBehaviour
{
    public TextField lobbyCodeInput;
    public MultiColumnListView lobbyList;
    [SerializeField] private StartUI startUI;
    [SerializeField] private LobbyUI lobbyUI;

    VisualElement root;
    public Button backButton;
    public Button joinButton;
    public Button refreshButton;


    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        backButton = root.Q<Button>("BackButton");
        joinButton = root.Q<Button>("JoinButton");
        refreshButton = root.Q<Button>("RefreshButton");

        lobbyCodeInput = root.Q<TextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;
        refreshButton.clicked += OnRefreshPressed;


        lobbyList = root.Q<MultiColumnListView>("LobbyList");
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
        if (lobbyCodeInput.text.IsNullOrEmpty())
            await LobbyLogic.Instance.JoinLobbyById((lobbyList.selectedItem as Lobby).Id);
        else
            await LobbyLogic.Instance.JoinLobbyByCode(lobbyCodeInput.value);
    }

    private async void OnRefreshPressed()
    {
        refreshButton.SetEnabled(false);
        await LobbyLogic.Instance.LoadPublicLobbies();
        refreshButton.SetEnabled(true);
    }

    private void SetupLobbyList()
    {
        lobbyList.itemsSource = LobbyLogic.Instance.PublicLobbies;

        // Binding the "Name" column
        lobbyList.columns["name"].makeCell = () => new Label();
        lobbyList.columns["name"].bindCell = (VisualElement e, int i) =>
            (e as Label).text = LobbyLogic.Instance.PublicLobbies[i].Name;

        // Binding the "Players" column
        lobbyList.columns["players"].makeCell = () => new Label();
        lobbyList.columns["players"].bindCell = (VisualElement e, int i) =>
            (e as Label).text = LobbyLogic.Instance.PublicLobbies[i].Players.Count + "/" + LobbyLogic.Instance.PublicLobbies[i].MaxPlayers;
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