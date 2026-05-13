using UnityEngine;
using UnityEngine.UIElements;

public class HostMenu : MonoBehaviour
{
    private Label _lobbyNameLabel;
    private Label _codeLabel;
    private Label[] _playerLabels = new Label[4];

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        _lobbyNameLabel = root.Q<Label>("LobbyNameLabel");
        _codeLabel = root.Q<Label>("CodeLabel");
        
        // Find player labels using your specific naming convention (Player0Label, etc.)
        for (int i = 0; i < 4; i++)
        {
            _playerLabels[i] = root.Q<Label>($"Player{i}Label");
        }

        // 2. Setup Buttons
        var backButton = root.Q<Button>("BackButton");
        var startButton = root.Q<Button>("StartButton");

        backButton.clicked += OnBackPressed;
        startButton.clicked += OnStartPressed;
    }

    // --- Button Functions ---

    public void OnBackPressed()
    {
        Debug.Log("Leaving lobby...");
        // Add Netcode logic to disconnect/leave lobby here
    }

    public void OnStartPressed()
    {
        Debug.Log("Start Game button pressed!");
        // Add Netcode logic to change scenes for all players
    }

    // --- UI Update Functions ---

    public void SetLobbyInfo(string lobbyName, string joinCode)
    {
        if (_lobbyNameLabel != null) _lobbyNameLabel.text = lobbyName;
        if (_codeLabel != null) _codeLabel.text = $"Code: {joinCode}";
    }

    public void UpdatePlayerSlot(int index, string playerName)
    {
        if (index >= 0 && index < _playerLabels.Length)
        {
            _playerLabels[index].text = playerName;
        }
    }
}