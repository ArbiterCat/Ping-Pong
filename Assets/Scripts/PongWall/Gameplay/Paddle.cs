using UnityEngine;
using Zenject;

namespace PongWall
{
    [RequireComponent(typeof(RectTransform))]
    public class Paddle : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _hardness = 0.8f;
        [SerializeField] private float _maxTiltDegrees = 25f;
        [SerializeField] private float _rotationSpeed = 8f;
        [SerializeField] private RectTransform _paddleSprite;
        [SerializeField] private float _maxPositionTiltDegrees = 30f;

        private RectTransform _rectTransform;
        private PlayFieldBounds _playFieldBounds;
        private GameConfig _gameConfig;
        private LazyInject<BallRoster> _ballRoster;
        private Vector2 _previousAnchoredPosition;
        private float _currentTiltZ;
        private Vector3 _spriteBaseEulerAngles;

        public float Hardness => _hardness;
        public Vector2 Velocity { get; private set; }
        public Vector2 Position => _rectTransform.anchoredPosition;

        public float HalfWidth => GetHalfSize().x;
        public float Height => GetHalfSize().y * 2f;

        [Inject]
        public void Construct(
            PlayFieldBounds playFieldBounds,
            GameConfig gameConfig,
            LazyInject<BallRoster> ballRoster)
        {
            _playFieldBounds = playFieldBounds;
            _gameConfig = gameConfig;
            _ballRoster = ballRoster;
            _rectTransform = transform as RectTransform;
            _previousAnchoredPosition = _rectTransform.anchoredPosition;

            if (_paddleSprite != null)
                _spriteBaseEulerAngles = _paddleSprite.localEulerAngles;
        }

        private void LateUpdate()
        {
            ApplyDepthScale();
            var currentPosition = _playFieldBounds.ClampPaddlePosition(
                _rectTransform.anchoredPosition,
                GetHalfSize());
            _rectTransform.anchoredPosition = currentPosition;

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            Velocity = (currentPosition - _previousAnchoredPosition) / deltaTime;
            _previousAnchoredPosition = currentPosition;

            UpdateTilt(deltaTime);
            UpdateSpritePositionTilt();
        }

        private void UpdateTilt(float deltaTime)
        {
            var targetTilt = 0f;
            var roster = _ballRoster?.Value;

            if (roster != null && roster.TryGetTiltTarget(Position.y, out var tiltTarget))
            {
                var zoneCenter = _playFieldBounds.GetPaddleSpawnCenter();
                var deltaX = zoneCenter.x - Position.x;
                var deltaY = tiltTarget.Position.y - Position.y;
                targetTilt = -Mathf.Clamp(
                    Mathf.Atan2(deltaX, deltaY) * Mathf.Rad2Deg,
                    -_maxTiltDegrees,
                    _maxTiltDegrees);
            }

            _currentTiltZ = Mathf.LerpAngle(_currentTiltZ, targetTilt, _rotationSpeed * deltaTime);
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, _currentTiltZ);
        }

        private void UpdateSpritePositionTilt()
        {
            if (_paddleSprite == null)
                return;

            var bounds = _playFieldBounds.PaddleMovementZone.LocalBounds;
            var halfWidth = HalfWidth;
            var minX = bounds.xMin + halfWidth;
            var maxX = bounds.xMax - halfWidth;
            var tiltZ = Mathf.Lerp(
                -_maxPositionTiltDegrees,
                _maxPositionTiltDegrees,
                Mathf.InverseLerp(minX, maxX, Position.x));

            _paddleSprite.localRotation = Quaternion.Euler(
                _spriteBaseEulerAngles.x,
                _spriteBaseEulerAngles.y,
                tiltZ);
        }

        public Vector2 GetHalfSize()
        {
            return UiBoundsUtility.GetLocalHalfSize(_rectTransform, _playFieldBounds.GameplayPanel);
        }

        public void ResetPosition(Vector2 position)
        {
            _rectTransform.anchoredPosition = position;
            ApplyDepthScale();
            position = _playFieldBounds.ClampPaddlePosition(position, GetHalfSize());
            _rectTransform.anchoredPosition = position;
            _previousAnchoredPosition = position;
            Velocity = Vector2.zero;
            _currentTiltZ = 0f;
            _rectTransform.localRotation = Quaternion.identity;

            if (_paddleSprite != null)
            {
                _paddleSprite.localRotation = Quaternion.Euler(
                    _spriteBaseEulerAngles.x,
                    _spriteBaseEulerAngles.y,
                    0f);
            }
        }

        private void ApplyDepthScale()
        {
            var bounds = _playFieldBounds.PaddleMovementZone.LocalBounds;
            var heightT = Mathf.InverseLerp(bounds.yMin, bounds.yMax, _rectTransform.anchoredPosition.y);
            var scale = Mathf.Lerp(1f, _gameConfig.PaddleMinScale, heightT);
            _rectTransform.localScale = Vector3.one * scale;
        }

        private void OnValidate()
        {
            _rectTransform = transform as RectTransform;

            if (_paddleSprite == null)
            {
                var spriteTransform = transform.Find("PaddleSprite");
                if (spriteTransform != null)
                    _paddleSprite = spriteTransform as RectTransform;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_rectTransform == null)
                return;

            var corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);

            for (var index = 0; index < corners.Length; index++)
                Gizmos.DrawLine(corners[index], corners[(index + 1) % corners.Length]);
        }
#endif
    }
}
