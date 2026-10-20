using UnityEngine;

namespace PongWall
{
    public class DeathLineMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform _lineRect;

        private RectTransform _coordinateSpace;

        public float DeathY { get; private set; }

        public void Initialize(RectTransform coordinateSpace)
        {
            _coordinateSpace = coordinateSpace;
            Refresh();
        }

        public void Refresh()
        {
            if (_lineRect == null || _coordinateSpace == null)
                return;

            var bounds = UiBoundsUtility.GetLocalRect(_lineRect, _coordinateSpace);
            DeathY = bounds.yMax;
        }

        private void OnValidate()
        {
            if (_lineRect == null)
                _lineRect = transform as RectTransform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_lineRect == null)
                return;

            var corners = new Vector3[4];
            _lineRect.GetWorldCorners(corners);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);

            for (var index = 0; index < corners.Length; index++)
                Gizmos.DrawLine(corners[index], corners[(index + 1) % corners.Length]);
        }
#endif
    }
}
