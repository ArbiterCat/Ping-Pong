using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace PongWall
{
    public class BallCollisionResolver : ILateTickable
    {
        private const float PositionEpsilon = 0.0001f;

        private readonly GameSession _gameSession;
        private readonly GameConfig _gameConfig;
        private readonly BallRoster _ballRoster;
        private readonly List<Ball> _activeBalls = new();
        private readonly List<Vector2> _deltaVelocity = new();
        private readonly List<Vector2> _separation = new();

        [Inject]
        public BallCollisionResolver(
            GameSession gameSession,
            GameConfig gameConfig,
            BallRoster ballRoster)
        {
            _gameSession = gameSession;
            _gameConfig = gameConfig;
            _ballRoster = ballRoster;
        }

        public void LateTick()
        {
            if (!_gameSession.IsPlaying)
                return;

            _ballRoster.CopyActiveBalls(_activeBalls);
            if (_activeBalls.Count < 2)
                return;

            EnsureBuffers(_activeBalls.Count);

            for (var firstIndex = 0; firstIndex < _activeBalls.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < _activeBalls.Count; secondIndex++)
                    ResolvePair(firstIndex, secondIndex);
            }

            for (var index = 0; index < _activeBalls.Count; index++)
            {
                var velocityDelta = _deltaVelocity[index];
                var separation = _separation[index];
                if (velocityDelta.sqrMagnitude <= PositionEpsilon && separation.sqrMagnitude <= PositionEpsilon)
                    continue;

                _activeBalls[index].ApplyPeerImpulse(velocityDelta, separation);
            }
        }

        private void ResolvePair(int firstIndex, int secondIndex)
        {
            var firstBall = _activeBalls[firstIndex];
            var secondBall = _activeBalls[secondIndex];

            if (!firstBall.CanCollideWithPeers || !secondBall.CanCollideWithPeers)
                return;

            if (Mathf.Abs(firstBall.VisualScale - secondBall.VisualScale) > _gameConfig.BallCollisionScaleTolerance)
                return;

            var offset = firstBall.Position - secondBall.Position;
            var distance = offset.magnitude;
            var combinedRadius = (firstBall.Radius + secondBall.Radius)
                * _gameConfig.BallCollisionRadiusMultiplier;
            if (distance >= combinedRadius)
                return;

            var normal = distance > PositionEpsilon
                ? offset / distance
                : Vector2.right;

            var relativeVelocity = firstBall.Velocity - secondBall.Velocity;
            if (Vector2.Dot(relativeVelocity, normal) >= 0f)
                return;

            var penetration = combinedRadius - distance;
            var sameDirection = firstBall.FlightPhase == secondBall.FlightPhase
                && (firstBall.FlightPhase == BallFlightPhase.RisingToWall
                    || firstBall.FlightPhase == BallFlightPhase.FallingToPaddle);

            if (sameDirection)
                ApplySameDirection(firstIndex, secondIndex, firstBall, secondBall, normal, penetration);
            else
                ApplyOppositeNudge(firstIndex, secondIndex, firstBall, secondBall, normal, penetration);
        }

        private void ApplySameDirection(
            int firstIndex,
            int secondIndex,
            Ball firstBall,
            Ball secondBall,
            Vector2 normal,
            float penetration)
        {
            var verticalSign = Mathf.Sign(firstBall.Velocity.y);
            if (Mathf.Approximately(verticalSign, 0f))
                verticalSign = Mathf.Sign(secondBall.Velocity.y);
            if (Mathf.Approximately(verticalSign, 0f))
                verticalSign = 1f;

            var forward = new Vector2(0f, verticalSign);
            var firstAlong = Vector2.Dot(firstBall.Position, forward);
            var secondAlong = Vector2.Dot(secondBall.Position, forward);
            var firstIsRear = firstAlong < secondAlong;

            var rearBall = firstIsRear ? firstBall : secondBall;
            var frontBall = firstIsRear ? secondBall : firstBall;
            var rearIndex = firstIsRear ? firstIndex : secondIndex;
            var frontIndex = firstIsRear ? secondIndex : firstIndex;

            var closingSpeed = Vector2.Dot(rearBall.Velocity - frontBall.Velocity, forward);
            if (closingSpeed < 0f)
                closingSpeed = 0f;

            var push = closingSpeed * _gameConfig.SameDirectionPushFactor;
            var alongImpulse = forward * push;
            _deltaVelocity[frontIndex] += alongImpulse;
            _deltaVelocity[rearIndex] -= alongImpulse;

            var sideSign = Mathf.Sign(firstBall.Position.x - secondBall.Position.x);
            if (Mathf.Approximately(sideSign, 0f))
                sideSign = 1f;

            var sideImpulse = new Vector2(_gameConfig.SideDeflection * sideSign, 0f);
            _deltaVelocity[firstIndex] += sideImpulse;
            _deltaVelocity[secondIndex] -= sideImpulse;

            var halfSeparation = normal * (penetration * 0.5f);
            _separation[firstIndex] += halfSeparation;
            _separation[secondIndex] -= halfSeparation;
        }

        private void ApplyOppositeNudge(
            int firstIndex,
            int secondIndex,
            Ball firstBall,
            Ball secondBall,
            Vector2 normal,
            float penetration)
        {
            var sideSign = Mathf.Sign(firstBall.Position.x - secondBall.Position.x);
            if (Mathf.Approximately(sideSign, 0f))
                sideSign = Mathf.Sign(normal.x);
            if (Mathf.Approximately(sideSign, 0f))
                sideSign = 1f;

            var nudge = new Vector2(_gameConfig.OppositeNudge * sideSign, 0f);
            _deltaVelocity[firstIndex] += nudge;
            _deltaVelocity[secondIndex] -= nudge;

            var halfSeparation = new Vector2(sideSign * penetration * 0.5f, 0f);
            _separation[firstIndex] += halfSeparation;
            _separation[secondIndex] -= halfSeparation;
        }

        private void EnsureBuffers(int count)
        {
            while (_deltaVelocity.Count < count)
                _deltaVelocity.Add(Vector2.zero);

            while (_separation.Count < count)
                _separation.Add(Vector2.zero);

            for (var index = 0; index < count; index++)
            {
                _deltaVelocity[index] = Vector2.zero;
                _separation[index] = Vector2.zero;
            }
        }
    }
}
