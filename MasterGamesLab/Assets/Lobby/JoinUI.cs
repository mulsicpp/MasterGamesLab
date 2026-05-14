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

    VisualElement root;
    Button backButton;
    Button joinButton;
    Button refreshButton;


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

        refreshLobbies();
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
        gameObject.SetActive(false);
        startUI.Show();
    }

    private async void OnJoinPressed()
    {
        if (lobbyCodeInput.text.IsNullOrEmpty())
            await LobbyLogic.Instance.JoinLobbyById((lobbyList.selectedItem as Lobby).Id);
        else
            await LobbyLogic.Instance.JoinLobbyByCode(lobbyCodeInput.value);
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