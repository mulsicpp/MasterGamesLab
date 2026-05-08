using UnityEngine;
using UnityEngine.UIElements;

public class JoinMenu : MonoBehaviour
{

    [SerializeField] private Lobby lobby;
    private Button joinButton;
    private Button backButton;

    private TextField nameField;
    private TextField ipAdressField;


    private VisualElement root;


    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        nameField = root.Q<TextField>("Name");
        ipAdressField = root.Q<TextField>("IpAddress");

        backButton = root.Q<Button>("Back");
        backButton.clicked += OnBackPressed;

        joinButton = root.Q<Button>("Join");
        joinButton.clicked += OnJoinPressed;

    }


    private void OnJoinPressed()
    {
        Debug.Log("Name: " + nameField.text + ", IP Address: " + ipAdressField.text);
    }
    private void OnBackPressed()
    {
        lobby.Show();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
