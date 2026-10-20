using DG.Tweening;
using UnityEngine;
using Zenject;

namespace PongWall
{
    public class GameRestarter
    {
        private readonly GameSession _gameSession;
        private readonly BallRoster _ballRoster;
        private readonly Paddle _paddle;
        private readonly PlayFieldBounds _playFieldBounds;

        [Inject]
        public GameRestarter(
            GameSession gameSession,
            BallRoster ballRoster,
            Paddle paddle,
            PlayFieldBounds playFieldBounds)
        {
            _gameSession = gameSession;
            _ballRoster = ballRoster;
            _paddle = paddle;
            _playFieldBounds = playFieldBounds;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            var paddleRectTransform = _paddle.transform as RectTransform;
            paddleRectTransform.DOKill();
            _ballRoster.DestroyAllExtras();
            _playFieldBounds.Refresh();
            _paddle.ResetPosition(_playFieldBounds.GetPaddleSpawnCenter());
            _ballRoster.Primary.LaunchFromPaddle();
            _gameSession.ResetSession();
        }
    }
}
