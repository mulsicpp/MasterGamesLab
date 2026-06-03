
using GLTFast.Schema;
using UnityEngine;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;
using UnityEngine.UIElements;
using System;

public class Menu : MonoBehaviour
{
    protected VisualElement root;
    public Action OnBecomesVisible;
    public Action OnBecomesHidden;

    public virtual void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
    }

    public void Show()
    {
        root.style.display = DisplayStyle.Flex;
        OnBecomesVisible?.Invoke();
    }

    public void Hide()
    {
        root.style.display = DisplayStyle.None;
        OnBecomesHidden?.Invoke();
    }
}