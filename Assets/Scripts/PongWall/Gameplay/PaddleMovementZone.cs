using UnityEngine;

namespace PongWall
{
    public class PaddleMovementZone : MonoBehaviour
    {
        [SerializeField] private RectTransform _zoneRect;

        private RectTransform _coordinateSpace;

        public Rect LocalBounds { get; private set; }

        public void Initialize(RectTransform coordinateSpace)
        {
            _coordinateSpace = coordinateSpace;
            Refresh();
        }

        public void Refresh()
        {
            if (_zoneRect == null || _coordinateSpace == null)
                return;

            LocalBounds = UiBoundsUtility.GetLocalRect(_zoneRect, _coordinateSpace);
        }

        public Vector2 ClampPosition(Vector2 targetCenter, Vector2 paddleHalfSize)
        {
            return UiBoundsUtility.ClampCenterInRect(targetCenter, paddleHalfSize, LocalBounds);
        }

        public Vector2 GetSpawnCenter()
        {
            return LocalBounds.center;
        }

        private void OnValidate()
        {
            if (_zoneRect == null)
                _zoneRect = transform as RectTransform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_zoneRect == null)
                return;

            var corners = new Vector3[4];
            _zoneRect.GetWorldCorners(corners);
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);

            for (var index = 0; index < corners.Length; index++)
                Gizmos.DrawLine(corners[index], corners[(index + 1) % corners.Length]);
        }
#endif
    }
}
