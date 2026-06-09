using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class PinboardUi : MonoBehaviour
{

    [SerializeField] private VisualTreeAsset truckTemplate;

    protected VisualElement root;

    protected virtual void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
    }

    public VisualElement CreateTruckIndicator()
    {
        VisualElement truckElement = truckTemplate.Instantiate();

        truckElement.style.position = Position.Absolute;

        root.Add(truckElement);

        return truckElement;
    }


}
