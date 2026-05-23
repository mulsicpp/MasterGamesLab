using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    private Label lobbyNameLabel;
    private Button lobbyCodeButton;
    private Label lobbyCodeLabel;
    private Label[] playerLabels = new Label[Constants.MAX_PLAYER_COUNT];
    [SerializeField] private StartUI startUI;

    VisualElement root;
    Button backButton;
    Button startButton;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        backButton = root.Q<Button>("BackButton");
        startButton = root.Q<Button>("StartButton");
        lobbyNameLabel = root.Q<Label>("LobbyNameLabel");
        lobbyCodeButton = root.Q<Button>("LobbyCodeButton");
        lobbyCodeLabel = root.Q<Label>("LobbyCodeLabel");

        for (int i = 0; i < Constants.MAX_PLAYER_COUNT; i++)
        {
            playerLabels[i] = root.Q<Label>($"Player{i}Label");
        }

        backButton.clicked += OnLeavePressed;
        startButton.clicked += OnStartPressed;
        lobbyCodeButton.clicked += OnLobbyCodePressed;

        Hide();
    }

    private void OnDisable()
    {
        backButton.clicked -= OnLeavePressed;
        startButton.clicked -= OnStartPressed;
        if (lobbyCodeButton != null) lobbyCodeButton.clicked -= OnLobbyCodePressed; // Clean up safely
    }

    private void Update()
    {
        startButton.SetEnabled(LobbyLogic.Instance.IsHost() && (!NetworkManager.Singleton?.IsListening ?? false));
    }

    private void OnLobbyCodePressed()
    {
        Debug.Log($"[Clipboard Attempt] Copying string: '{lobbyCodeLabel.text}' (Length: {lobbyCodeLabel.text?.Length})");
        GUIUtility.systemCopyBuffer = lobbyCodeLabel.text;

        // Trigger the floating text animation
        SpawnFloatingFeedbackText();
    }

    private void SpawnFloatingFeedbackText()
    {
        if (lobbyCodeButton == null) return;

        // Create the temporary popup element
        Label popup = new Label("Copied to Clipboard!");

        // Inherit your font variables and core label rules automatically from your USS
        popup.AddToClassList("label");

        // Apply absolute positioning layout properties
        popup.style.position = Position.Absolute;
        popup.style.color = Color.white;
        popup.style.fontSize = 14;

        // Align it right over the center area of the button
        popup.style.top = -20f;
        popup.style.left = 0f;

        // Add the popup into the button container scope
        lobbyCodeButton.Add(popup);

        // Slide up smoothly
        popup.experimental.animation.Start(
            new UnityEngine.UIElements.Experimental.StyleValues { top = 20f },
            new UnityEngine.UIElements.Experimental.StyleValues { top = -55f },
            1200
        );

        // Fade out
        popup.experimental.animation.Start(
            new UnityEngine.UIElements.Experimental.StyleValues { opacity = 1f },
            new UnityEngine.UIElements.Experimental.StyleValues { opacity = 0f },
            1200
        ).OnCompleted(() =>
        {
            popup.RemoveFromHierarchy();
        });
    }

    private async void OnLeavePressed()
    {
        Debug.Log("Back button clicked. Returning to Main Menu...");
        await LobbyLogic.Instance.LeaveLobby();
    }

    private async void OnStartPressed()
    {
        Debug.Log("Start Game button pressed!");
        startButton.SetEnabled(false);
        await LobbyLogic.Instance.StartHost();
        startButton.SetEnabled(LobbyLogic.Instance.IsHost() && !NetworkManager.Singleton.IsListening);
    }

    public void SetLobbyInfo(string lobbyName, string joinCode)
    {
        if (lobbyNameLabel != null) lobbyNameLabel.text = lobbyName;
        if (lobbyCodeLabel != null) lobbyCodeLabel.text = $"{joinCode}";
    }

    public void UpdateUI(Lobby lobby)
    {
        var localPlayerId = AuthenticationService.Instance.PlayerId;
        if (lobby.HostId == localPlayerId)
            startButton.SetEnabled(true);
        else
            startButton.SetEnabled(false);

        foreach (var playerLabel in playerLabels)
        {
            playerLabel.text = "";
            playerLabel.RemoveFromClassList("lobby-player-label");
            playerLabel.AddToClassList("lobby-player-label-empty");

            var hostIcon = playerLabel.Q<VisualElement>("HostIcon");
            if (hostIcon != null) hostIcon.style.display = DisplayStyle.None;
        }

        SetLobbyInfo(lobby.Name, lobby.LobbyCode);

        var sortedPlayers = lobby.Players
            .OrderByDescending(p => p.Id == localPlayerId)
            .ToList();
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var player = sortedPlayers[i];
            if (player.Id == lobby.HostId)
            {
                var hostIcon = playerLabels[i].Q<VisualElement>("HostIcon");
                if (hostIcon != null) hostIcon.style.display = DisplayStyle.Flex;
            }
            playerLabels[i].text = player.Data["Name"].Value;
            playerLabels[i].RemoveFromClassList("lobby-player-label-empty");
            playerLabels[i].AddToClassList("lobby-player-label");
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