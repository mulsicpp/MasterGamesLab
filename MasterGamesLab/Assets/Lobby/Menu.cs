
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

        public bool IsVisible { get; private set; }

        public abstract MenuId Id { get; }

        protected virtual void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            IsVisible = false;
            Show();
        }

        public void Show()
        {
            if (IsVisible) return;
            root.style.display = DisplayStyle.Flex;
            IsVisible = true;
            OnBecameVisible?.Invoke();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            root.style.display = DisplayStyle.None;
            IsVisible = false;
            OnBecameHidden?.Invoke();
        }
    }
}