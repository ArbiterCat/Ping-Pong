using UnityEngine;

namespace PongWall
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PongWall/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Ball")]
        [SerializeField] private float _ballSize = 45f;
        [SerializeField] private float _ballSpeed = 8f;
        [SerializeField] private float _launchAngleRandomRange = 1.5f;
        [SerializeField] private float _maxBallSpeed = 14f;
        [SerializeField] private float _minSpeedToContinue = 0.5f;
        [SerializeField] private float _ballLaunchOffset = 60f;

        [Header("Ball Hit Physics")]
        [SerializeField] private float _stationaryThreshold = 0.3f;
        [SerializeField] private float _passiveAbsorptionWhenStationary = 0.62f;
        [SerializeField] private float _speedTransferFactor = 1.2f;
        [SerializeField] private float _maxSpeedLossPerHit = 0.3f;
        [SerializeField] private float _positionAngleWeight = 4f;
        [SerializeField] private float _paddleAngleWeight = 3f;
        [SerializeField] private float _upwardMinComponent = 0.5f;

        [Header("Trajectory Assist")]
        [SerializeField] private float _postHitWallAssist = 0.55f;
        [SerializeField] private float _maxHorizontalSpeedRatio = 0.55f;
        [SerializeField] private float _postWallPaddleAssist = 0f;
        [SerializeField] private float _postWallReturnSpeedRetain = 0.95f;
        [SerializeField] private float _wallContactSpeedSpread = 0.15f;
        [SerializeField] private float _returnSteerStrength = 0f;

        [Header("Ball Flight Visuals")]
        [SerializeField] private float _tableBounceMinScale = 0.65f;
        [SerializeField] private float _returnArcMinScale = 0.75f;

        [Header("Dying Top-Down")]
        [SerializeField] private float _dyingSpeedDecay = 200f;
        [SerializeField] private float _dyingBounceWaveLength = 90f;
        [SerializeField] private float _dyingStopSpeed = 15f;
        [SerializeField] private float _minBounceVelocity = 45f;
        [SerializeField] private float _dyingSettleDuration = 0.35f;

        [Header("Wall 3D Bounce")]
        [SerializeField] private float _wallDepthTransferFactor = 0.08f;
        [SerializeField] private float _wallDepthDamping = 0.85f;

        [Header("Ball Collision")]
        [SerializeField] private float _ballCollisionScaleTolerance = 0.08f;
        [SerializeField, Range(0.1f, 1f)] private float _ballCollisionRadiusMultiplier = 0.65f;
        [SerializeField] private float _sameDirectionPushFactor = 0.25f;
        [SerializeField] private float _sideDeflection = 40f;
        [SerializeField] private float _oppositeNudge = 25f;

        [Header("Paddle")]
        [SerializeField] private float _paddleFollowDuration = 0.15f;
        [SerializeField] private float _paddleTouchYOffset = 80f;
        [SerializeField, Range(0.3f, 1f)] private float _paddleMinScale = 0.65f;

        public float BallSize => _ballSize;
        public float BallSpeed => _ballSpeed;
        public float LaunchAngleRandomRange => _launchAngleRandomRange;
        public float MaxBallSpeed => _maxBallSpeed;
        public float MinSpeedToContinue => _minSpeedToContinue;
        public float BallLaunchOffset => _ballLaunchOffset;
        public float StationaryThreshold => _stationaryThreshold;
        public float PassiveAbsorptionWhenStationary => _passiveAbsorptionWhenStationary;
        public float SpeedTransferFactor => _speedTransferFactor;
        public float MaxSpeedLossPerHit => _maxSpeedLossPerHit;
        public float PositionAngleWeight => _positionAngleWeight;
        public float PaddleAngleWeight => _paddleAngleWeight;
        public float UpwardMinComponent => _upwardMinComponent;
        public float PostHitWallAssist => _postHitWallAssist;
        public float MaxHorizontalSpeedRatio => _maxHorizontalSpeedRatio;
        public float PostWallPaddleAssist => _postWallPaddleAssist;
        public float PostWallReturnSpeedRetain => _postWallReturnSpeedRetain;
        public float WallContactSpeedSpread => _wallContactSpeedSpread;
        public float ReturnSteerStrength => _returnSteerStrength;
        public float TableBounceMinScale => _tableBounceMinScale;
        public float ReturnArcMinScale => _returnArcMinScale;
        public float DyingSpeedDecay => _dyingSpeedDecay;
        public float DyingBounceWaveLength => _dyingBounceWaveLength;
        public float DyingStopSpeed => _dyingStopSpeed;
        public float MinBounceVelocity => _minBounceVelocity;
        public float DyingSettleDuration => _dyingSettleDuration;
        public float WallDepthTransferFactor => _wallDepthTransferFactor;
        public float WallDepthDamping => _wallDepthDamping;
        public float BallCollisionScaleTolerance => _ballCollisionScaleTolerance;
        public float BallCollisionRadiusMultiplier => _ballCollisionRadiusMultiplier;
        public float SameDirectionPushFactor => _sameDirectionPushFactor;
        public float SideDeflection => _sideDeflection;
        public float OppositeNudge => _oppositeNudge;
        public float PaddleFollowDuration => _paddleFollowDuration;
        public float PaddleTouchYOffset => _paddleTouchYOffset;
        public float PaddleMinScale => _paddleMinScale;
    }
}
