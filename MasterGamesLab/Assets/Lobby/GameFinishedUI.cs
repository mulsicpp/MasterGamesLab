using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

namespace UI
{
    public class GameFinishedUI : Menu
    {
        public ResponsiveButton returnToStartButton;
        public ResponsiveButton playAgainButton;

        public override MenuId Id => MenuId.GameFinished;

        protected override void OnEnable()
        {
            base.OnEnable();

            returnToStartButton = root.Q<ResponsiveButton>("ReturnToStart");
            playAgainButton = root.Q<ResponsiveButton>("PlayAgain");

            returnToStartButton.clicked += OnReturnToStartPressed;
            playAgainButton.clicked += OnPlayAgainPressed;
        }

        void OnDisable()
        {
            returnToStartButton.clicked -= OnReturnToStartPressed;
            playAgainButton.clicked -= OnPlayAgainPressed;
        }

        private async void OnReturnToStartPressed()
        {
            await UIManager.Instance.LeaveLobbyAsync();
        }

        private void OnPlayAgainPressed()
        {
            UIManager.Instance.CurrentMenu = MenuId.Lobby;
        }
    }
}
