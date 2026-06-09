using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class PinboardUi : MonoBehaviour
{

    [SerializeField] private VisualTreeAsset truckTemplate;

    public VisualElement root;

    protected virtual void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
    }

public VisualElement CreateTruckIndicator()
{
    VisualElement wrapper = new VisualElement();
    
    wrapper.style.height = Length.Percent(5f);
    wrapper.style.aspectRatio = 0.7f;
    wrapper.AddToClassList("element");
    
    wrapper.style.position = Position.Absolute;

    VisualElement truckElement = truckTemplate.Instantiate();

    wrapper.Add(truckElement);
    root.Add(wrapper);


    return wrapper; 
}


}
