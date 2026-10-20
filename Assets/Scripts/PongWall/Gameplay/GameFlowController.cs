using UnityEngine;
using Zenject;

namespace PongWall
{
    public class GameFlowController : MonoBehaviour
    {
        private PlayFieldBounds _playFieldBounds;
        private BallRoster _ballRoster;
        private Paddle _paddle;

        [Inject]
        public void Construct(
            PlayFieldBounds playFieldBounds,
            BallRoster ballRoster,
            Paddle paddle)
        {
            _playFieldBounds = playFieldBounds;
            _ballRoster = ballRoster;
            _paddle = paddle;
        }

        private void Start()
        {
            _playFieldBounds.Refresh();
            _paddle.ResetPosition(_playFieldBounds.GetPaddleSpawnCenter());
            _ballRoster.Primary.LaunchFromPaddle();
        }
    }
}
