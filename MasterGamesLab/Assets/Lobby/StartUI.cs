using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

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

            LobbyLogic.Instance.PlayerName = playerName.Value;
            try
            {
                await LobbyLogic.Instance.CreateLobbyAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create lobby: {e}");
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

            LobbyLogic.Instance.PlayerName = playerName.Value;
            await LobbyLogic.Instance.GoToJoinMenu();
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

        public void BecameVisible()
        {
            playerName.schedule.Execute(() => playerName.Focus());
        }

        public void BecameHidden()
        {
            // Clean up the error highlight state if the window gets hidden/closed
            playerName.RemoveFromClassList(HighlightClass);
        }
    }
}