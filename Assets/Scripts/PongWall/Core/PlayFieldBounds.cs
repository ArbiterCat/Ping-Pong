using UnityEngine;

namespace PongWall
{
    public class PlayFieldBounds : MonoBehaviour
    {
        [SerializeField] private RectTransform _gameplayPanel;
        [SerializeField] private PaddleMovementZone _paddleMovementZone;
        [SerializeField] private WallSurface _wallSurface;
        [SerializeField] private DeathLineMarker _deathLine;
        [SerializeField] private SideBoundMarker _leftBound;
        [SerializeField] private SideBoundMarker _rightBound;
        [SerializeField] private RectTransform _ballsRoot;

        public RectTransform GameplayPanel => _gameplayPanel;
        public Transform BallsRoot => _ballsRoot != null ? _ballsRoot : _gameplayPanel;
        public PaddleMovementZone PaddleMovementZone => _paddleMovementZone;
        public WallSurface WallSurface => _wallSurface;

        public float WallBottomY => _wallSurface != null ? _wallSurface.BottomY : 0f;
        public float WallTopY => _wallSurface != null ? _wallSurface.TopY : 0f;
        public float WallCenterX => _wallSurface != null ? _wallSurface.CenterX : 0f;
        public float WallHeight => _wallSurface != null ? _wallSurface.Height : 0f;
        public float WallLeftX => _wallSurface != null ? _wallSurface.LeftX : 0f;
        public float WallRightX => _wallSurface != null ? _wallSurface.RightX : 0f;
        public float WallDepthMin => _wallSurface != null ? _wallSurface.DepthMin : 0f;
        public float WallDepthMax => _wallSurface != null ? _wallSurface.DepthMax : 0f;
        public float DeathY => _deathLine != null ? _deathLine.DeathY : float.MinValue;
        public float LeftBoundX => _leftBound != null ? _leftBound.BoundX : float.MinValue;
        public float RightBoundX => _rightBound != null ? _rightBound.BoundX : float.MaxValue;

        public void Refresh()
        {
            if (_gameplayPanel == null)
                _gameplayPanel = transform as RectTransform;

            _paddleMovementZone?.Initialize(_gameplayPanel);
            _wallSurface?.Initialize(_gameplayPanel);
            _deathLine?.Initialize(_gameplayPanel);
            _leftBound?.Initialize(_gameplayPanel);
            _rightBound?.Initialize(_gameplayPanel);
        }

        public Vector2 ClampPaddlePosition(Vector2 targetCenter, Vector2 paddleHalfSize)
        {
            return _paddleMovementZone.ClampPosition(targetCenter, paddleHalfSize);
        }

        public Vector2 GetPaddleSpawnCenter()
        {
            return _paddleMovementZone.GetSpawnCenter();
        }

        public bool IsOutOfHorizontalBounds(float positionX, float ballRadius)
        {
            return positionX - ballRadius < LeftBoundX || positionX + ballRadius > RightBoundX;
        }

        public bool IsBelowDeathLine(float positionY, float ballRadius)
        {
            return positionY - ballRadius < DeathY;
        }

        private void OnValidate()
        {
            if (_gameplayPanel == null)
                _gameplayPanel = transform as RectTransform;

            Debug.Assert(_ballsRoot != null, $"{nameof(_ballsRoot)} is not assigned.");
        }
    }
}
