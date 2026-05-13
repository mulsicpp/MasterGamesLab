using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class JoinMenu : MonoBehaviour
{
    private TextField _lobbyCodeInput;
    private MultiColumnListView _lobbyList;
    

    private List<LobbyData> _availableLobbies = new List<LobbyData>();

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        var backButton = root.Q<Button>("BackButton");
        var joinButton = root.Q<Button>("JoinButton");
        _lobbyCodeInput = root.Q<TextField>("LobbyCode");

        backButton.clicked += OnBackPressed;
        joinButton.clicked += OnJoinPressed;


        _lobbyList = root.Q<MultiColumnListView>("LobbyList");
        SetupLobbyList();
        AddLobbyElement("test1", 2);
        AddLobbyElement("test2", 3);
        AddLobbyElement("test3", 4);
    }

    public void OnBackPressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");

    }

    public void OnJoinPressed()
    {
        string code = _lobbyCodeInput.value;
        Debug.Log($"Attempting to join with code: {code}");
    }

    // This is the function you requested to add new items dynamically
    public void AddLobbyElement(string lobbyName, int playerCount)
    {
        _availableLobbies.Add(new LobbyData { Name = lobbyName, Players = playerCount });
        
        // Tell the UI Toolkit to refresh and show the new data
        _lobbyList.RefreshItems();
    }

    private void SetupLobbyList()
    {
        _lobbyList.itemsSource = _availableLobbies;

        // Binding the "Name" column
        _lobbyList.columns["name"].makeCell = () => new Label();
        _lobbyList.columns["name"].bindCell = (VisualElement e, int i) => 
            (e as Label).text = _availableLobbies[i].Name;

        // Binding the "Players" column
        _lobbyList.columns["players"].makeCell = () => new Label();
        _lobbyList.columns["players"].bindCell = (VisualElement e, int i) => 
            (e as Label).text = $"{_availableLobbies[i].Players}/" + Constants.MAX_PLAYER_COUNT;
    }
}

public class LobbyData
{
    public string Name;
    public int Players;
}