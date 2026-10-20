using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace PongWall
{
    public class BallRoster
    {
        public const int MaxExtraBalls = 5;

        private readonly GameSession _gameSession;
        private readonly GameFxSignals _fxSignals;
        private readonly Ball _primaryBall;
        private readonly Transform _ballParent;
        private readonly Transform _shadowParent;
        private readonly Ball.Factory _ballFactory;
        private readonly BallShadowFollower.Factory _shadowFactory;
        private readonly List<ExtraBall> _extraBalls = new();
        private readonly ReactiveProperty<int> _extraCount = new(0);

        public Ball Primary => _primaryBall;
        public IReadOnlyReactiveProperty<int> ExtraCount => _extraCount;
        public bool CanSpawnExtra => _extraBalls.Count < MaxExtraBalls;

        [Inject]
        public BallRoster(
            GameSession gameSession,
            GameFxSignals fxSignals,
            PlayFieldBounds playFieldBounds,
            Ball primaryBall,
            BallShadowFollower primaryShadow,
            Ball.Factory ballFactory,
            BallShadowFollower.Factory shadowFactory)
        {
            _gameSession = gameSession;
            _fxSignals = fxSignals;
            _primaryBall = primaryBall;
            _ballParent = playFieldBounds.BallsRoot;
            _shadowParent = primaryShadow.transform.parent;
            _ballFactory = ballFactory;
            _shadowFactory = shadowFactory;
            primaryBall.transform.SetParent(_ballParent, true);
            primaryShadow.Bind(primaryBall);
        }

        public void SpawnExtra()
        {
            if (!_gameSession.IsPlaying || !CanSpawnExtra)
                return;

            var ball = _ballFactory.Create();
            ball.transform.SetParent(_ballParent, false);

            var shadow = _shadowFactory.Create();
            shadow.transform.SetParent(_shadowParent, false);
            shadow.Bind(ball);

            ball.LaunchFromPaddle();
            _extraBalls.Add(new ExtraBall(ball, shadow));
            _extraCount.Value = _extraBalls.Count;
            _fxSignals?.ExtraBallSpawned.OnNext(new ExtraBallFx(
                ball.transform.position,
                _extraBalls.Count + 1));
        }

        public void RemoveLastExtra()
        {
            if (_extraBalls.Count == 0)
                return;

            var lastIndex = _extraBalls.Count - 1;
            DestroyExtra(_extraBalls[lastIndex]);
            _extraBalls.RemoveAt(lastIndex);
            _extraCount.Value = _extraBalls.Count;
        }

        public void DestroyAllExtras()
        {
            for (var index = 0; index < _extraBalls.Count; index++)
                DestroyExtra(_extraBalls[index]);

            _extraBalls.Clear();
            _extraCount.Value = 0;
        }

        public void CopyActiveBalls(List<Ball> destination)
        {
            destination.Clear();
            if (_primaryBall.IsInFlight)
                destination.Add(_primaryBall);

            for (var index = 0; index < _extraBalls.Count; index++)
            {
                var extra = _extraBalls[index].Ball;
                if (extra.IsInFlight)
                    destination.Add(extra);
            }
        }

        public bool TryGetTiltTarget(float paddleY, out Ball target)
        {
            target = null;
            var nearestY = float.MaxValue;

            ConsiderForTilt(_primaryBall, paddleY, ref nearestY, ref target);

            for (var index = 0; index < _extraBalls.Count; index++)
                ConsiderForTilt(_extraBalls[index].Ball, paddleY, ref nearestY, ref target);

            return target != null;
        }

        private static void ConsiderForTilt(Ball ball, float paddleY, ref float nearestY, ref Ball target)
        {
            if (!ball.IsInFlight || ball.IsDying)
                return;

            if (ball.FlightPhase != BallFlightPhase.FallingToPaddle)
                return;

            var ballY = ball.Position.y;
            if (ballY <= paddleY)
                return;

            if (ballY >= nearestY)
                return;

            nearestY = ballY;
            target = ball;
        }

        private static void DestroyExtra(ExtraBall extraBall)
        {
            extraBall.Ball.Stop();
            Object.Destroy(extraBall.Ball.gameObject);
            Object.Destroy(extraBall.Shadow.gameObject);
        }

        private readonly struct ExtraBall
        {
            public ExtraBall(Ball ball, BallShadowFollower shadow)
            {
                Ball = ball;
                Shadow = shadow;
            }

            public Ball Ball { get; }
            public BallShadowFollower Shadow { get; }
        }
    }
}
