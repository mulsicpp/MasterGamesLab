using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

public class StartUI : MonoBehaviour
{
    [SerializeField] private JoinUI joinUI;
    [SerializeField] private LobbyUI lobbyUI;
    public ResponsiveTextField playerName;
    public ResponsiveButton hostButton;
    public ResponsiveButton joinButton;

    private VisualElement root;


    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = root.Q<ResponsiveButton>("Host");
        joinButton = root.Q<ResponsiveButton>("Join");
        playerName = root.Q<ResponsiveTextField>("Name");


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
        if (playerName.Value.IsNullOrEmpty())
            return;

        hostButton.SetEnabled(false);
        joinButton.SetEnabled(false);

        LobbyLogic.Instance.PlayerName = playerName.Value;
        try
        {
            await LobbyLogic.Instance.CreateLobby();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
        hostButton.SetEnabled(true);
        joinButton.SetEnabled(true);
    }

    private async void OnJoinPressed()
    {
        if (playerName.Value.IsNullOrEmpty())
            return;
        LobbyLogic.Instance.PlayerName = playerName.Value;

        await LobbyLogic.Instance.GoToJoinMenu();
        // if (playerName.text.IsNullOrEmpty())
        //     return;
        // LobbyLogic.Instance.PlayerName = playerName.text;
        // joinUI.Show();
        // Hide();
    }

    public void Show()
    {
        root.style.display = DisplayStyle.Flex;
        playerName.schedule.Execute(() => playerName.Focus());
    }

    public void Hide()
    {
        root.style.display = DisplayStyle.None;

    }
}
