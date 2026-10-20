using UnityEngine;
using Zenject;

namespace PongWall
{
    public enum BallFlightPhase
    {
        RisingToWall,
        FallingToPaddle,
        DyingBounces
    }

    [RequireComponent(typeof(RectTransform))]
    public class Ball : MonoBehaviour
    {
        private const float VelocityEpsilon = 0.001f;
        private const float VerticalOffsetWeight = 0.25f;
        private const float DepthScaleFactor = 0.15f;
        private const float WallReachMargin = 20f;
        private const float DyingWallBounceDamping = 0.45f;

        private GameConfig _gameConfig;
        private GameSession _gameSession;
        private PlayFieldBounds _playFieldBounds;
        private Paddle _paddle;
        private GameFxSignals _fxSignals;
        private RectTransform _rectTransform;

        private Vector2 _velocity;
        private Vector2 _previousAnchoredPosition;
        private float _depthVelocity;
        private float _depth;
        private bool _isActive;
        private int _ignoreDeathChecksUntilFrame;

        private BallFlightPhase _flightPhase;
        private float _flightStartY;
        private float _flightEndY;
        private float _wallContactY;
        private bool _wallContactResolved;
        private int _bounceCount;
        private float _dyingInitialSpeed;
        private float _dyingWaveDistance;
        private bool _isDyingSettling;
        private float _settleTimer;
        private float _returnStartScale = 1f;

        public BallFlightPhase FlightPhase => _flightPhase;
        public bool IsInFlight => _isActive;
        public bool IsDying => _flightPhase == BallFlightPhase.DyingBounces;
        public float VisualScale => _rectTransform.localScale.x;
        public Vector2 Position => _rectTransform.anchoredPosition;
        public Vector2 Velocity => _velocity;
        public float Radius => GetHalfSize().y;
        public bool CanCollideWithPeers =>
            _isActive && !IsDying && Time.frameCount > _ignoreDeathChecksUntilFrame;

        public class Factory : PlaceholderFactory<Ball>
        {
        }

        [Inject]
        public void Construct(
            GameConfig gameConfig,
            GameSession gameSession,
            PlayFieldBounds playFieldBounds,
            Paddle paddle,
            GameFxSignals fxSignals)
        {
            _gameConfig = gameConfig;
            _gameSession = gameSession;
            _playFieldBounds = playFieldBounds;
            _paddle = paddle;
            _fxSignals = fxSignals;
            _rectTransform = transform as RectTransform;
            ApplyConfiguredSize();
            _previousAnchoredPosition = _rectTransform.anchoredPosition;
        }

        private void ApplyConfiguredSize()
        {
            var ballSize = _gameConfig.BallSize;
            _rectTransform.sizeDelta = new Vector2(ballSize, ballSize);
            _rectTransform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!_isActive || !_gameSession.IsPlaying)
                return;

            if (_flightPhase == BallFlightPhase.DyingBounces)
            {
                UpdateDyingBounces(Time.deltaTime);
                return;
            }

            Move(Time.deltaTime);
            CheckPeakBeforeWall();
            CheckStoppedInFlight();
            CheckWallReflection();
            CheckHorizontalBounds();
            CheckDeathLine();
            CheckPaddleHit();
            CheckRunOutOfEnergy();
            ApplyFlightVisuals();
        }

        public void LaunchFromPaddle()
        {
            var paddlePosition = _paddle.Position;
            var ballHalfSize = GetHalfSize();

            _rectTransform.anchoredPosition = new Vector2(
                paddlePosition.x,
                paddlePosition.y + ballHalfSize.y + _gameConfig.BallLaunchOffset);
            _previousAnchoredPosition = _rectTransform.anchoredPosition;

            var randomHorizontal = Random.Range(
                -_gameConfig.LaunchAngleRandomRange,
                _gameConfig.LaunchAngleRandomRange);

            var launchDirection = new Vector2(randomHorizontal, _gameConfig.BallSpeed).normalized;
            _velocity = launchDirection * _gameConfig.BallSpeed;
            _depth = 0f;
            _depthVelocity = 0f;
            _bounceCount = 0;
            _isActive = true;
            _ignoreDeathChecksUntilFrame = Time.frameCount + 2;
            BeginRisingToWall(_rectTransform.anchoredPosition);
            ApplyFlightVisuals();
        }

        public void Stop()
        {
            _isActive = false;
            _velocity = Vector2.zero;
            _depthVelocity = 0f;
            _wallContactResolved = false;
            _bounceCount = 0;
        }

        public void ApplyPeerImpulse(Vector2 velocityDelta, Vector2 separation)
        {
            var preservedVerticalSign = Mathf.Sign(_velocity.y);
            _velocity = ClampHorizontalSpeed(_velocity + velocityDelta);
            if (Mathf.Abs(_velocity.y) > VelocityEpsilon
                && Mathf.Sign(_velocity.y) != preservedVerticalSign
                && preservedVerticalSign != 0f)
                _velocity.y = Mathf.Abs(_velocity.y) * preservedVerticalSign;

            _rectTransform.anchoredPosition += separation;
            _previousAnchoredPosition = _rectTransform.anchoredPosition;
        }

        private void ForceEndGame()
        {
            NotifyBallLost();
            Stop();
            _gameSession.EndGame(immediate: true);
        }

        private void NotifyBallLost()
        {
            if (!_isActive)
                return;

            _fxSignals?.BallLost.OnNext(new BallLostFx(_rectTransform.position));
        }

        private float GetRadius()
        {
            return GetHalfSize().y;
        }

        private Vector2 GetHalfSize()
        {
            return UiBoundsUtility.GetLocalHalfSize(_rectTransform, _playFieldBounds.GameplayPanel);
        }

        private float GetAssistStrength(float speed, float baseAssist)
        {
            var referenceSpeed = _gameConfig.BallSpeed * 0.45f;
            return baseAssist * Mathf.Clamp01(speed / referenceSpeed);
        }

        private void Move(float deltaTime)
        {
            _previousAnchoredPosition = _rectTransform.anchoredPosition;
            var position = _previousAnchoredPosition + _velocity * deltaTime;
            _rectTransform.anchoredPosition = position;
            _depth += _depthVelocity * deltaTime;
        }

        private void BeginRisingToWall(Vector2 ballPosition)
        {
            _flightPhase = BallFlightPhase.RisingToWall;
            _flightStartY = ballPosition.y;
            _flightEndY = _playFieldBounds.WallBottomY;
            _returnStartScale = 1f;
            _wallContactResolved = false;
            _velocity = ApplyWallAssist(_velocity, ballPosition);
        }

        private void BeginFallingToPaddle(Vector2 contactPoint, float speed)
        {
            _returnStartScale = CalculateFlightScale(contactPoint.y);
            _flightPhase = BallFlightPhase.FallingToPaddle;
            _flightStartY = contactPoint.y;
            _flightEndY = _playFieldBounds.GetPaddleSpawnCenter().y;
            _wallContactResolved = false;

            var incomingDirection = _velocity.sqrMagnitude > VelocityEpsilon
                ? _velocity.normalized
                : Vector2.down;
            var reflectedDirection = new Vector2(incomingDirection.x, -Mathf.Abs(incomingDirection.y));
            if (reflectedDirection.sqrMagnitude <= VelocityEpsilon)
                reflectedDirection = Vector2.down;
            else
                reflectedDirection.Normalize();

            var returnSpeed = speed * _gameConfig.PostWallReturnSpeedRetain;
            _velocity = reflectedDirection * returnSpeed;
        }

        private void BeginDyingBounces()
        {
            var previousPhase = _flightPhase;
            _flightPhase = BallFlightPhase.DyingBounces;
            _bounceCount = 0;
            _isDyingSettling = false;
            _settleTimer = 0f;
            _dyingWaveDistance = 0f;

            if (_velocity.sqrMagnitude <= VelocityEpsilon)
            {
                var direction = GetHopDirectionFromPhase(previousPhase);
                _velocity = direction * _gameConfig.MinBounceVelocity;
            }

            _dyingInitialSpeed = _velocity.magnitude;
        }

        private Vector2 GetHopDirectionFromPhase(BallFlightPhase phase)
        {
            if (_velocity.sqrMagnitude > VelocityEpsilon)
                return _velocity.normalized;

            if (phase == BallFlightPhase.RisingToWall)
                return Vector2.up;

            if (phase == BallFlightPhase.FallingToPaddle)
            {
                var toPaddle = _playFieldBounds.GetPaddleSpawnCenter() - _rectTransform.anchoredPosition;
                if (toPaddle.sqrMagnitude > VelocityEpsilon)
                    return toPaddle.normalized;
            }

            return new Vector2(Random.value > 0.5f ? 1f : -1f, 0.35f).normalized;
        }

        private void UpdateDyingBounces(float deltaTime)
        {
            if (_isDyingSettling)
            {
                if (CheckDeathLine())
                    return;

                _settleTimer += deltaTime;
                _rectTransform.localScale = Vector3.one;

                if (_settleTimer >= _gameConfig.DyingSettleDuration)
                {
                    NotifyBallLost();
                    Stop();
                    _gameSession.EndGame();
                }

                return;
            }

            var direction = _velocity.normalized;
            var speed = Mathf.Max(0f, _velocity.magnitude - _gameConfig.DyingSpeedDecay * deltaTime);
            _velocity = direction * speed;

            var position = _rectTransform.anchoredPosition + _velocity * deltaTime;
            var radius = GetRadius();
            var speedRatio = _dyingInitialSpeed > VelocityEpsilon
                ? speed / _dyingInitialSpeed
                : 0f;

            if (_playFieldBounds.IsOutOfHorizontalBounds(position.x, radius))
            {
                ForceEndGame();
                return;
            }

            if (_velocity.y > VelocityEpsilon)
            {
                var contactY = Mathf.Lerp(_playFieldBounds.WallBottomY, _playFieldBounds.WallTopY, speedRatio);

                if (position.y + radius >= contactY)
                {
                    position.y = contactY - radius;
                    _velocity.y = -Mathf.Abs(_velocity.y) * DyingWallBounceDamping;
                }
            }

            _rectTransform.anchoredPosition = position;

            if (CheckDeathLine())
                return;

            _dyingWaveDistance += speed * deltaTime;
            var wavePhase = (_dyingWaveDistance / _gameConfig.DyingBounceWaveLength) % 1f;
            var arc = Mathf.Sin(wavePhase * Mathf.PI);
            var scale = 1f - (1f - _gameConfig.TableBounceMinScale) * arc * speedRatio;
            _rectTransform.localScale = Vector3.one * scale;

            if (speed > _gameConfig.DyingStopSpeed)
                return;

            _isDyingSettling = true;
            _velocity = Vector2.zero;
            _rectTransform.localScale = Vector3.one;
        }

        private Vector2 ApplyWallAssist(Vector2 velocity, Vector2 ballPosition)
        {
            var speed = velocity.magnitude;
            if (speed <= VelocityEpsilon)
                return velocity;

            var wallTarget = new Vector2(_playFieldBounds.WallCenterX, _playFieldBounds.WallBottomY);
            var toWall = (wallTarget - ballPosition).normalized;
            var hitDirection = velocity / speed;
            var assist = GetAssistStrength(speed, _gameConfig.PostHitWallAssist);
            var blendedDirection = Vector2.Lerp(hitDirection, toWall, assist).normalized;
            var assistedVelocity = blendedDirection * speed;
            return ClampHorizontalSpeed(assistedVelocity);
        }

        private Vector2 ClampHorizontalSpeed(Vector2 velocity)
        {
            var speed = velocity.magnitude;
            if (speed <= VelocityEpsilon)
                return velocity;

            var maxHorizontal = speed * _gameConfig.MaxHorizontalSpeedRatio;
            velocity.x = Mathf.Clamp(velocity.x, -maxHorizontal, maxHorizontal);
            return velocity.normalized * speed;
        }

        private void ApplyFlightVisuals()
        {
            var position = _rectTransform.anchoredPosition;
            var flightScale = CalculateFlightScale(position.y);

            var depthRange = _playFieldBounds.WallDepthMax - _playFieldBounds.WallDepthMin;
            var normalizedDepth = depthRange > 0f
                ? Mathf.InverseLerp(_playFieldBounds.WallDepthMin, _playFieldBounds.WallDepthMax, _depth)
                : 0.5f;

            var depthOffset = (normalizedDepth - 0.5f) * depthRange * 0.01f;
            var localPosition = _rectTransform.localPosition;
            localPosition.z = -depthOffset;
            _rectTransform.localPosition = localPosition;

            var depthScale = 1f + normalizedDepth * DepthScaleFactor;
            _rectTransform.localScale = Vector3.one * (flightScale * depthScale);
        }

        private float CalculateFlightScale(float currentY)
        {
            if (_flightPhase == BallFlightPhase.RisingToWall)
            {
                var visualEndY = _playFieldBounds.WallTopY;
                if (Mathf.Approximately(_flightStartY, visualEndY))
                    return 1f;

                var progress = Mathf.Clamp01(Mathf.InverseLerp(_flightStartY, visualEndY, currentY));
                var bounce = Mathf.Sin(progress * Mathf.PI);
                return 1f - (1f - _gameConfig.TableBounceMinScale) * bounce;
            }

            if (Mathf.Approximately(_flightStartY, _flightEndY))
                return _returnStartScale;

            var returnProgress = Mathf.Clamp01(Mathf.InverseLerp(_flightStartY, _flightEndY, currentY));
            returnProgress = Mathf.SmoothStep(0f, 1f, returnProgress);
            return Mathf.Lerp(_returnStartScale, 1f, returnProgress);
        }

        private bool ShouldIgnoreDeathChecks()
        {
            return Time.frameCount <= _ignoreDeathChecksUntilFrame;
        }

        private void CheckPeakBeforeWall()
        {
            if (_flightPhase != BallFlightPhase.RisingToWall)
                return;

            if (_velocity.y > 0f)
                return;

            var radius = GetRadius();
            if (_rectTransform.anchoredPosition.y + radius >= _playFieldBounds.WallBottomY - WallReachMargin)
                return;

            BeginDyingBounces();
        }

        private void CheckStoppedInFlight()
        {
            if (ShouldIgnoreDeathChecks())
                return;

            if (_velocity.sqrMagnitude >= _gameConfig.MinSpeedToContinue * _gameConfig.MinSpeedToContinue)
                return;

            BeginDyingBounces();
        }

        private void CheckWallReflection()
        {
            if (_flightPhase != BallFlightPhase.RisingToWall)
                return;

            var position = _rectTransform.anchoredPosition;
            var radius = GetRadius();
            var wallBottomY = _playFieldBounds.WallBottomY;

            if (position.y + radius < wallBottomY)
                return;

            if (!_wallContactResolved)
            {
                var speed = _velocity.magnitude;
                var speedFactor = Mathf.InverseLerp(
                    _gameConfig.MinSpeedToContinue,
                    _gameConfig.MaxBallSpeed,
                    speed);
                var contactT = Mathf.Clamp01(
                    speedFactor + Random.Range(-_gameConfig.WallContactSpeedSpread, _gameConfig.WallContactSpeedSpread));
                _wallContactY = Mathf.Lerp(_playFieldBounds.WallBottomY, _playFieldBounds.WallTopY, contactT);
                _wallContactResolved = true;
                _flightEndY = _wallContactY;
            }

            if (position.y + radius < _wallContactY)
                return;

            position.y = _wallContactY - radius;
            position.x = Mathf.Clamp(
                position.x,
                _playFieldBounds.WallLeftX + radius,
                _playFieldBounds.WallRightX - radius);
            _rectTransform.anchoredPosition = position;

            var impactSpeed = _velocity.magnitude;
            var impactAngle = impactSpeed > VelocityEpsilon
                ? Mathf.Abs(_velocity.y) / impactSpeed
                : 1f;

            var depthImpulse = impactSpeed * _gameConfig.WallDepthTransferFactor * impactAngle;
            _depthVelocity += depthImpulse * Mathf.Sign(_velocity.x == 0f ? 1f : _velocity.x);
            _depth = Mathf.Clamp(_depth, _playFieldBounds.WallDepthMin, _playFieldBounds.WallDepthMax);

            if (_depth <= _playFieldBounds.WallDepthMin || _depth >= _playFieldBounds.WallDepthMax)
                _depthVelocity = -_depthVelocity * _gameConfig.WallDepthDamping;

            if (impactSpeed < _gameConfig.MinSpeedToContinue * 1.2f)
            {
                _velocity = new Vector2(_velocity.x * 0.5f, -Mathf.Abs(_velocity.y) * 0.4f);
                BeginDyingBounces();
                return;
            }

            _fxSignals?.WallHit.OnNext(new WallHitFx(_rectTransform.position, impactSpeed));
            BeginFallingToPaddle(position, impactSpeed);
        }

        private void CheckHorizontalBounds()
        {
            if (ShouldIgnoreDeathChecks())
                return;

            if (!_playFieldBounds.IsOutOfHorizontalBounds(_rectTransform.anchoredPosition.x, GetRadius()))
                return;

            ForceEndGame();
        }

        private void CheckPaddleHit()
        {
            if (_flightPhase != BallFlightPhase.FallingToPaddle)
                return;

            if (_velocity.y >= 0f)
                return;

            var currentPosition = _rectTransform.anchoredPosition;
            var paddlePosition = _paddle.Position;
            var halfWidth = _paddle.HalfWidth;
            var halfHeight = _paddle.Height * 0.5f;
            var radius = GetRadius();

            if (TryGetSweptTopFaceContact(
                    _previousAnchoredPosition,
                    currentPosition,
                    paddlePosition,
                    halfWidth,
                    halfHeight,
                    radius,
                    out var sweptContactPosition))
            {
                _rectTransform.anchoredPosition = sweptContactPosition;
                ApplyPaddleHit(sweptContactPosition, paddlePosition, halfWidth, halfHeight, radius);
                return;
            }

            if (!IsValidTopFaceContactAt(currentPosition, paddlePosition, halfWidth, halfHeight, radius))
                return;

            ApplyPaddleHit(currentPosition, paddlePosition, halfWidth, halfHeight, radius);
        }

        private bool TryGetSweptTopFaceContact(
            Vector2 previousPosition,
            Vector2 currentPosition,
            Vector2 paddlePosition,
            float halfWidth,
            float halfHeight,
            float radius,
            out Vector2 contactPosition)
        {
            contactPosition = default;
            var paddleTopY = paddlePosition.y + halfHeight;
            var previousBottomY = previousPosition.y - radius;
            var currentBottomY = currentPosition.y - radius;

            if (previousBottomY < paddleTopY)
                return false;

            if (currentBottomY > paddleTopY)
                return false;

            Vector2 candidatePosition;
            if (Mathf.Approximately(previousBottomY, currentBottomY))
            {
                candidatePosition = currentPosition;
            }
            else
            {
                var crossingT = (previousBottomY - paddleTopY) / (previousBottomY - currentBottomY);
                crossingT = Mathf.Clamp01(crossingT);
                var contactX = Mathf.Lerp(previousPosition.x, currentPosition.x, crossingT);
                candidatePosition = new Vector2(contactX, paddleTopY + radius);
            }

            if (!IsValidTopFaceContactAt(candidatePosition, paddlePosition, halfWidth, halfHeight, radius))
                return false;

            contactPosition = candidatePosition;
            return true;
        }

        private bool IsValidTopFaceContactAt(
            Vector2 ballPosition,
            Vector2 paddlePosition,
            float halfWidth,
            float halfHeight,
            float radius)
        {
            var paddleTopY = paddlePosition.y + halfHeight;

            if (ballPosition.y < paddleTopY)
                return false;

            var overlapsHorizontally = ballPosition.x + radius >= paddlePosition.x - halfWidth
                && ballPosition.x - radius <= paddlePosition.x + halfWidth;

            if (!overlapsHorizontally)
                return false;

            var ballBottomY = ballPosition.y - radius;
            var penetrationFromTop = paddleTopY - ballBottomY;

            if (penetrationFromTop < 0f || penetrationFromTop > radius * 1.5f)
                return false;

            var horizontalOffset = Mathf.Abs(ballPosition.x - paddlePosition.x);
            var isSideOnlyContact = horizontalOffset + radius > halfWidth && ballPosition.y < paddleTopY + radius;

            return !isSideOnlyContact;
        }

        private void CheckRunOutOfEnergy()
        {
            if (ShouldIgnoreDeathChecks())
                return;

            if (_flightPhase != BallFlightPhase.FallingToPaddle)
                return;

            if (_velocity.magnitude >= _gameConfig.MinSpeedToContinue)
                return;

            BeginDyingBounces();
        }

        private bool CheckDeathLine()
        {
            if (ShouldIgnoreDeathChecks())
                return false;

            if (!_playFieldBounds.IsBelowDeathLine(_rectTransform.anchoredPosition.y, GetRadius()))
                return false;

            NotifyBallLost();
            Stop();
            _gameSession.EndGame();
            return true;
        }

        private void ApplyPaddleHit(
            Vector2 ballPosition,
            Vector2 paddlePosition,
            float halfWidth,
            float halfHeight,
            float radius)
        {
            var incomingSpeed = _velocity.magnitude;
            var paddleVelocity = _paddle.Velocity;
            var paddleSpeed = paddleVelocity.magnitude;

            var offsetX = Mathf.Clamp((ballPosition.x - paddlePosition.x) / halfWidth, -1f, 1f);
            var offsetY = Mathf.Clamp((ballPosition.y - paddlePosition.y) / halfHeight, -1f, 1f);

            var paddleDirectionX = paddleSpeed > VelocityEpsilon ? paddleVelocity.x / paddleSpeed : 0f;
            var paddleDirectionY = paddleSpeed > VelocityEpsilon ? paddleVelocity.y / paddleSpeed : 0f;

            var directionX = offsetX * _gameConfig.PositionAngleWeight + paddleDirectionX * _gameConfig.PaddleAngleWeight;
            var directionY = Mathf.Max(
                _gameConfig.UpwardMinComponent,
                1f + offsetY * VerticalOffsetWeight + paddleDirectionY * _gameConfig.PaddleAngleWeight * 0.5f);

            var hitDirection = new Vector2(directionX, directionY).normalized;

            float newSpeed;
            if (paddleSpeed < _gameConfig.StationaryThreshold)
            {
                newSpeed = incomingSpeed * _gameConfig.PassiveAbsorptionWhenStationary;
            }
            else
            {
                var paddleAlongHit = Vector2.Dot(paddleVelocity, hitDirection);
                var hardnessMultiplier = _paddle.Hardness;
                var impulse = paddleAlongHit * _gameConfig.SpeedTransferFactor * hardnessMultiplier;
                newSpeed = incomingSpeed + impulse;
            }

            var maxLoss = incomingSpeed * _gameConfig.MaxSpeedLossPerHit;
            newSpeed = Mathf.Max(newSpeed, incomingSpeed - maxLoss);
            newSpeed = Mathf.Clamp(newSpeed, 0f, _gameConfig.MaxBallSpeed);

            if (newSpeed < _gameConfig.MinBounceVelocity)
            {
                _velocity = hitDirection * Mathf.Max(newSpeed, _gameConfig.MinBounceVelocity * 0.5f);
                var position = _rectTransform.anchoredPosition;
                position.y = paddlePosition.y + halfHeight + radius;
                _rectTransform.anchoredPosition = position;
                BeginDyingBounces();
                return;
            }

            _velocity = hitDirection * newSpeed;
            _velocity = ApplyWallAssist(_velocity, ballPosition);

            var hitPosition = _rectTransform.anchoredPosition;
            hitPosition.y = paddlePosition.y + halfHeight + radius;
            _rectTransform.anchoredPosition = hitPosition;

            BeginRisingToWall(hitPosition);
            _gameSession.AddScore(1);
            _fxSignals?.PaddleHit.OnNext(new PaddleHitFx(
                _rectTransform.position,
                newSpeed,
                offsetX,
                _gameSession.Score.Value));
        }
    }
}
