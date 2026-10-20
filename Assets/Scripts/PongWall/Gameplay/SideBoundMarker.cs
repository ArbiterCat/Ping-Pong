using UnityEngine;

namespace PongWall
{
    public enum SideBoundKind
    {
        Left,
        Right
    }

    public class SideBoundMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform _boundRect;
        [SerializeField] private SideBoundKind _side = SideBoundKind.Left;

        private RectTransform _coordinateSpace;

        public SideBoundKind Side => _side;
        public float BoundX { get; private set; }

        public void Initialize(RectTransform coordinateSpace)
        {
            _coordinateSpace = coordinateSpace;
            Refresh();
        }

        public void Refresh()
        {
            if (_boundRect == null || _coordinateSpace == null)
                return;

            var bounds = UiBoundsUtility.GetLocalRect(_boundRect, _coordinateSpace);
            BoundX = _side == SideBoundKind.Left ? bounds.xMax : bounds.xMin;
        }

        private void OnValidate()
        {
            if (_boundRect == null)
                _boundRect = transform as RectTransform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_boundRect == null)
                return;

            var corners = new Vector3[4];
            _boundRect.GetWorldCorners(corners);
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);

            for (var index = 0; index < corners.Length; index++)
                Gizmos.DrawLine(corners[index], corners[(index + 1) % corners.Length]);
        }
#endif
    }
}
