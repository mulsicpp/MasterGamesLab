
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace UI
{
    public abstract class Menu : MonoBehaviour
    {
        public enum MenuId
        {
            Start,
            Join,
            Lobby,
            Loading,
            Ingame,
            GameFinished
        };

        protected VisualElement root;
        public Action OnBecameVisible;
        public Action OnBecameHidden;

        public abstract MenuId Id { get; }

        protected virtual void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
        }

        public void Show()
        {
            root.style.display = DisplayStyle.Flex;
            OnBecameVisible?.Invoke();
        }

        public void Hide()
        {
            OnBecameHidden?.Invoke();
            root.style.display = DisplayStyle.None;
        }
    }
}