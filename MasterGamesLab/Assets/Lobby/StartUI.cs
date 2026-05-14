using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class StartUI : MonoBehaviour
{
    [SerializeField] private JoinUI joinUI;
    [SerializeField] private LobbyUI lobbyUI;
    private Button hostButton;
    private Button joinButton;

    private VisualElement root;


    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = root.Q<Button>("Host");
        joinButton = root.Q<Button>("Join");

        hostButton.clicked += OnHostPressedAsync;
        joinButton.clicked += OnJoinPressed;

    }

    private async void OnHostPressedAsync()
    {
        Debug.Log("Creating lobby");

        hostButton.SetEnabled(false);

        try
        {
            await LobbyLogic.Instance.CreateLobby();

            Debug.Log("Lobby created");

            lobbyUI.Show();
            gameObject.SetActive(false);
            Debug.Log("Menu switched");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
            hostButton.SetEnabled(true);
        }
    }

    private void OnJoinPressed()
    {
        joinUI.Show();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
