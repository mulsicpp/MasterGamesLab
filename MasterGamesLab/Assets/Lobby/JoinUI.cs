using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class JoinUI : MonoBehaviour
{
    private TextField lobbyCodeInput;
    private MultiColumnListView lobbyList;
    [SerializeField] private StartUI startUI;


    private async void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        var backButton = root.Q<Button>("BackButton");
        var joinButton = root.Q<Button>("JoinButton");
        lobbyCodeInput = root.Q<TextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;


        lobbyList = root.Q<MultiColumnListView>("LobbyList");
        SetupLobbyList();

        await LobbyLogic.Instance.LoadPublicLobbies();
        Debug.Log("Loaded lobbies: " + LobbyLogic.Instance.PublicLobbies.Count);
        lobbyList.itemsSource = LobbyLogic.Instance.PublicLobbies;
        lobbyList.RefreshItems();
        Debug.Log("Refreshed");
    }

    public void OnBackPressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        gameObject.SetActive(false);
        startUI.Show();
    }

    public void OnJoinPressed()
    {
        string code = lobbyCodeInput.value;
        Debug.Log($"Attempting to join with code: {code}");
    }


    // public void AddLobbyElement(string lobbyName, int playerCount)
    // {
    //     availableLobbies.Add(new Lobby { Name = lobbyName, Players = playerCount });

    //     lobbyList.RefreshItems();
    // }

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