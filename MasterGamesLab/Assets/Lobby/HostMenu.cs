using UnityEngine;
using UnityEngine.UIElements;

public class HostMenu : MonoBehaviour
{

    [SerializeField] private Lobby lobby;
    private Button hostButton;
    private Button backButton;

    private TextField nameField;

    private Label ipAddressLabel;

    private VisualElement root;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        nameField = root.Q<TextField>("Name");

        ipAddressLabel = root.Q<Label>("IpAddress");
        ipAddressLabel.text = "furz";

        backButton = root.Q<Button>("Back");
        backButton.clicked += OnBackPressed;

        hostButton = root.Q<Button>("Host");
        hostButton.clicked += OnHostPressed;

    }

    private void OnHostPressed()
    {
        Debug.Log("Name: " + nameField.text + ", IP Address: " + ipAddressLabel.text);
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
