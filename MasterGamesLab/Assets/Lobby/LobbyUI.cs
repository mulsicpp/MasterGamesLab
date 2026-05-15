using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

public class LobbyUI : MonoBehaviour
{
    private Label lobbyNameLabel;
    private Label codeLabel;
    private Label[] playerLabels = new Label[Constants.MAX_PLAYER_COUNT];
    [SerializeField] private StartUI startUI;

    VisualElement root;
    Button leaveButton;
    Button startButton;


    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        leaveButton = root.Q<Button>("LeaveButton");
        startButton = root.Q<Button>("StartButton");
        lobbyNameLabel = root.Q<Label>("LobbyNameLabel");
        codeLabel = root.Q<Label>("CodeLabel");

        for (int i = 0; i < Constants.MAX_PLAYER_COUNT; i++)
        {
            playerLabels[i] = root.Q<Label>($"Player{i}Label");
        }



        leaveButton.clicked += OnLeavePressed;
        startButton.clicked += OnStartPressed;

        Hide();
    }

    private void OnDisable()
    {
        leaveButton.clicked -= OnLeavePressed;
        startButton.clicked -= OnStartPressed;
    }


    private void OnLeavePressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        LobbyLogic.Instance.ShowStartUI();
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

    public void UpdateUI(Lobby lobby)
    {
        foreach (var playerLabel in playerLabels)
        {
            playerLabel.text = "";
        }

        SetLobbyInfo(lobby.Name, lobby.LobbyCode);

        for (int i = 0; i < lobby.Players.Count; i++)
        {
            playerLabels[i].text = lobby.Players[i].Data["Name"].Value;
        }
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