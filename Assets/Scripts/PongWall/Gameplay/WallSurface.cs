using UnityEngine;

namespace PongWall
{
    public class WallSurface : MonoBehaviour
    {
        [SerializeField] private RectTransform _surfaceRect;
        [SerializeField] private float _depthRange = 120f;

        private RectTransform _coordinateSpace;
        private Rect _localBounds;

        public float BottomY { get; private set; }
        public float TopY { get; private set; }
        public float CenterX { get; private set; }
        public float Height { get; private set; }
        public float LeftX { get; private set; }
        public float RightX { get; private set; }
        public float DepthMin { get; private set; }
        public float DepthMax { get; private set; }
        public float DepthRange => _depthRange;

        public void Initialize(RectTransform coordinateSpace)
        {
            _coordinateSpace = coordinateSpace;
            Refresh();
        }

        public void Refresh()
        {
            if (_surfaceRect == null || _coordinateSpace == null)
                return;

            _localBounds = UiBoundsUtility.GetLocalRect(_surfaceRect, _coordinateSpace);
            BottomY = _localBounds.yMin;
            TopY = _localBounds.yMax;
            CenterX = _localBounds.center.x;
            Height = _localBounds.height;
            LeftX = _localBounds.xMin;
            RightX = _localBounds.xMax;

            var depthCenter = (_localBounds.yMin + _localBounds.yMax) * 0.5f;
            DepthMin = depthCenter - _depthRange * 0.5f;
            DepthMax = depthCenter + _depthRange * 0.5f;
        }

        private void OnValidate()
        {
            if (_surfaceRect == null)
                _surfaceRect = transform as RectTransform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_surfaceRect == null)
                return;

            var corners = new Vector3[4];
            _surfaceRect.GetWorldCorners(corners);
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);

            for (var index = 0; index < corners.Length; index++)
                Gizmos.DrawLine(corners[index], corners[(index + 1) % corners.Length]);
        }
#endif
    }
}
