using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

public class GameFinishedUI : MonoBehaviour
{
    public ResponsiveButton returnToStartButton;
    public ResponsiveButton playAgainButton;

    private VisualElement root;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        returnToStartButton = root.Q<ResponsiveButton>("ReturnToStart");
        playAgainButton = root.Q<ResponsiveButton>("PlayAgain");

        returnToStartButton.clicked += OnReturnToStartPressed;
        playAgainButton.clicked += OnPlayAgainPressed;
    }

    void OnDisable()
    {
        returnToStartButton.clicked -= OnReturnToStartPressed;
        playAgainButton.clicked -= OnPlayAgainPressed;
    }

    private async void OnReturnToStartPressed()
    {
        await LobbyLogic.Instance.LeaveLobbyAsync();
    }

    private void OnPlayAgainPressed()
    {
        if (LobbyLogic.Instance.Lobby != null)
            LobbyLogic.Instance.ShowLobbyUI();
        else
            LobbyLogic.Instance.ShowStartUI();
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
