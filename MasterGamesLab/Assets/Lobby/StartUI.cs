using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class StartUI : MonoBehaviour
{
    [SerializeField] private JoinUI joinMenu;
    [SerializeField] private LobbyUI hostMenu;
    private Button hostButton;
    private Button joinButton;

    private VisualElement root;


    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = root.Q<Button>("Host");
        joinButton = root.Q<Button>("Join");

        hostButton.clicked += OnHostPressed;
        joinButton.clicked += OnJoinPressed;

    }

    private void OnHostPressed()
    {
        //hostMenu.Show();
        gameObject.SetActive(false);
    }

    private void OnJoinPressed()
    {
        //joinMenu.Show();
        gameObject.SetActive(false);

    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
