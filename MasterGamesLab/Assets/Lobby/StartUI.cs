using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

namespace UI
{
    public class StartUI : Menu
    {
        public ResponsiveTextField playerName;
        public ResponsiveButton hostButton;
        public ResponsiveButton joinButton;

        // Cache the class name string to avoid typos
        private const string HighlightClass = "highlighted-text-field";

        public override MenuId Id => MenuId.Start;

        protected override void OnEnable()
        {
            base.OnEnable();

            hostButton = root.Q<ResponsiveButton>("Host");
            joinButton = root.Q<ResponsiveButton>("Join");
            playerName = root.Q<ResponsiveTextField>("Name");

            OnBecameVisible += BecameVisible;
            OnBecameHidden += BecameHidden;

            hostButton.clicked += OnHostPressedAsync;
            joinButton.clicked += OnJoinPressed;

            // Register a callback to clear the glow highlight as soon as the user starts typing
            playerName.RegisterValueChangedCallback(OnNameValueChanged);
        }

        void OnDisable()
        {
            hostButton.clicked -= OnHostPressedAsync;
            joinButton.clicked -= OnJoinPressed;

        }

        private async void OnHostPressedAsync()
        {
            if (playerName.Value.IsNullOrEmpty())
            {
                TriggerValidationHighlight();
                return;
            }

            hostButton.SetEnabled(false);
            joinButton.SetEnabled(false);

            UIManager.Instance.PlayerName = playerName.Value;
            try
            {
                await UIManager.Instance.CreateLobbyAsync();
            }
            catch (System.Exception e)
            {
            }
            hostButton.SetEnabled(true);
            joinButton.SetEnabled(true);
        }

        private async void OnJoinPressed()
        {
            if (playerName.Value.IsNullOrEmpty())
            {
                TriggerValidationHighlight();
                return;
            }

            UIManager.Instance.PlayerName = playerName.Value;
            await UIManager.Instance.GoToJoinMenu();
        }

        private void TriggerValidationHighlight()
        {
            playerName.AddToClassList(HighlightClass);

            playerName.Focus();
        }

        private void OnNameValueChanged(ChangeEvent<string> evt)
        {
            // Clear the glow warning layout when text is present
            if (!evt.newValue.IsNullOrEmpty())
            {
                playerName.RemoveFromClassList(HighlightClass);
            }
        }

        private void BecameVisible()
        {
            playerName.Value = UIManager.Instance.PlayerName;
            playerName.schedule.Execute(() => playerName.Focus());
        }

        private void BecameHidden()
        {
            // Clean up the error highlight state if the window gets hidden/closed
            playerName.RemoveFromClassList(HighlightClass);
        }
    }
}