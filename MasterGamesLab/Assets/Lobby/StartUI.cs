using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

public class StartUI : MonoBehaviour
{
    [SerializeField] private JoinUI joinUI;
    [SerializeField] private LobbyUI lobbyUI;
    private TextField playerName;
    private Button hostButton;
    private Button joinButton;

    private VisualElement root;


    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = root.Q<Button>("Host");
        joinButton = root.Q<Button>("Join");
        playerName = root.Q<TextField>("Name");

        hostButton.clicked += OnHostPressedAsync;
        joinButton.clicked += OnJoinPressed;
    }

    void OnDisable()
    {
        hostButton.clicked -= OnHostPressedAsync;
        joinButton.clicked -= OnJoinPressed;

    }

    private async void OnHostPressedAsync()
    {
        if (playerName.text.IsNullOrEmpty())
            return;

        hostButton.SetEnabled(false);
        joinButton.SetEnabled(false);

        LobbyLogic.Instance.PlayerName = playerName.text;
        hostButton.SetEnabled(false);
        try
        {
            await LobbyLogic.Instance.CreateLobby();
            lobbyUI.Show();
            gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
            hostButton.SetEnabled(true);
        }
        hostButton.SetEnabled(true);
        joinButton.SetEnabled(true);
    }

    private void OnJoinPressed()
    {
        if (playerName.text.IsNullOrEmpty())
            return;
        LobbyLogic.Instance.PlayerName = playerName.text;
        joinUI.Show();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
