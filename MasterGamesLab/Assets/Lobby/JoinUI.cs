using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class JoinUI : MonoBehaviour
{
    private TextField lobbyCodeInput;
    private MultiColumnListView lobbyList;
    [SerializeField] private StartUI startUI;


    private List<Lobby> availableLobbies = new List<Lobby>();

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        var backButton = root.Q<Button>("BackButton");
        var joinButton = root.Q<Button>("JoinButton");
        lobbyCodeInput = root.Q<TextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;


        lobbyList = root.Q<MultiColumnListView>("LobbyList");
        SetupLobbyList();
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
        lobbyList.itemsSource = availableLobbies;

        // Binding the "Name" column
        lobbyList.columns["name"].makeCell = () => new Label();
        lobbyList.columns["name"].bindCell = (VisualElement e, int i) =>
            (e as Label).text = availableLobbies[i].Name;

        // Binding the "Players" column
        lobbyList.columns["players"].makeCell = () => new Label();
        lobbyList.columns["players"].bindCell = (VisualElement e, int i) =>
            (e as Label).text = availableLobbies[i].Players + "/" + availableLobbies[i].MaxPlayers;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}