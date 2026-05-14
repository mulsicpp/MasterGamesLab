using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using WebSocketSharp;

public class JoinUI : MonoBehaviour
{
    private TextField lobbyCodeInput;
    private MultiColumnListView lobbyList;
    [SerializeField] private StartUI startUI;
    [SerializeField] private LobbyUI lobbyUI;


    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        var backButton = root.Q<Button>("BackButton");
        var joinButton = root.Q<Button>("JoinButton");
        var refreshButton = root.Q<Button>("RefreshButton");
        lobbyCodeInput = root.Q<TextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;
        refreshButton.clicked += OnRefreshPressed;


        lobbyList = root.Q<MultiColumnListView>("LobbyList");
        SetupLobbyList();
    }

    private async void OnEnable()
    {
        refreshLobbies();
    }

    private void OnBackPressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        gameObject.SetActive(false);
        startUI.Show();
    }

    private async void OnJoinPressed()
    {
        if (lobbyCodeInput.text.IsNullOrEmpty())
            await LobbyLogic.Instance.JoinLobbyByCode(lobbyCodeInput.value);
        else
            await LobbyLogic.Instance.JoinLobbyById((lobbyList.selectedItem as Lobby).Id);
        lobbyUI.Show();
        gameObject.SetActive(false);
    }

    private void OnRefreshPressed()
    {
        refreshLobbies();
    }


    private async void refreshLobbies()
    {
        await LobbyLogic.Instance.LoadPublicLobbies();
        lobbyList.itemsSource = LobbyLogic.Instance.PublicLobbies;
        lobbyList.RefreshItems();
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
        gameObject.SetActive(true);
    }
}