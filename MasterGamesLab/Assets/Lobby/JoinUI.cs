using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class JoinUI : MonoBehaviour
{
    private TextField lobbyCodeInput;
    private MultiColumnListView lobbyList;
    [SerializeField] private StartUI startUI;


    private List<LobbyData> availableLobbies = new List<LobbyData>();

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
        AddLobbyElement("test1", 2);
        AddLobbyElement("test2", 3);
        AddLobbyElement("test3", 4);
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

    // This is the function you requested to add new items dynamically
    public void AddLobbyElement(string lobbyName, int playerCount)
    {
        availableLobbies.Add(new LobbyData { Name = lobbyName, Players = playerCount });

        // Tell the UI Toolkit to refresh and show the new data
        lobbyList.RefreshItems();
    }

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
            (e as Label).text = $"{availableLobbies[i].Players}/" + Constants.MAX_PLAYER_COUNT;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}

public class LobbyData
{
    public string Name;
    public int Players;
}