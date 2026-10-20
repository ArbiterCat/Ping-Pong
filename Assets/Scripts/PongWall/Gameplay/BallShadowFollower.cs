using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    [RequireComponent(typeof(RectTransform))]
    public class BallShadowFollower : MonoBehaviour
    {
        [SerializeField] private RectTransform _maskRect;
        [SerializeField] private float _verticalOffsetAtMinScale;
        [SerializeField] private float _verticalOffsetAtMaxScale = 80f;
        [SerializeField] private float _minBallScale = 0.65f;

        private Ball _ball;
        private RectTransform _ballRectTransform;
        private RectTransform _shadowRect;
        private Image _shadowImage;

        public class Factory : PlaceholderFactory<BallShadowFollower>
        {
        }

        public void Bind(Ball ball)
        {
            _ball = ball;
            _ballRectTransform = ball.transform as RectTransform;

            if (transform.parent is RectTransform parentRect)
                _maskRect = parentRect;
        }

        private void Awake()
        {
            _shadowRect = transform as RectTransform;
            _shadowImage = GetComponent<Image>();

            if (_maskRect == null)
                _maskRect = transform.parent as RectTransform;
        }

        private void OnValidate()
        {
            if (_maskRect == null && transform.parent is RectTransform parentRect)
                _maskRect = parentRect;
        }

        private void LateUpdate()
        {
            if (_ball == null || !_ball.IsInFlight)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            var localPosition = (Vector2)_maskRect.InverseTransformPoint(_ballRectTransform.position);
            var ballScale = _ball.VisualScale;
            var scaleT = Mathf.InverseLerp(_minBallScale, 1f, ballScale);
            var verticalOffset = Mathf.Lerp(_verticalOffsetAtMinScale, _verticalOffsetAtMaxScale, scaleT);

            _shadowRect.anchoredPosition = localPosition + Vector2.down * verticalOffset;
            _shadowRect.localScale = Vector3.one * ballScale;
        }

        private void SetVisible(bool isVisible)
        {
            _shadowImage.enabled = isVisible;
        }
    }
}
