using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUI : MonoBehaviour
{
    private Label lobbyNameLabel;
    private Label codeLabel;
    private Label[] playerLabels = new Label[Constants.MAX_PLAYER_COUNT];
    [SerializeField] private StartUI startUI;



    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        lobbyNameLabel = root.Q<Label>("LobbyNameLabel");
        codeLabel = root.Q<Label>("CodeLabel");

        for (int i = 0; i < Constants.MAX_PLAYER_COUNT; i++)
        {
            playerLabels[i] = root.Q<Label>($"Player{i}Label");
        }

        var backButton = root.Q<Button>("BackButton");
        var startButton = root.Q<Button>("StartButton");

        backButton.clicked += OnBackPressed;
        startButton.clicked += OnStartPressed;
    }


    private void OnBackPressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        gameObject.SetActive(false);
        startUI.Show();
    }

    private void OnStartPressed()
    {
        Debug.Log("Start Game button pressed!");
    }


    public void SetLobbyInfo(string lobbyName, string joinCode)
    {
        if (lobbyNameLabel != null) lobbyNameLabel.text = lobbyName;
        if (codeLabel != null) codeLabel.text = $"Code: {joinCode}";
    }

    public void UpdatePlayerSlot(int index, string playerName)
    {
        if (index >= 0 && index < playerLabels.Length)
        {
            playerLabels[index].text = playerName;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}