using UnityEngine;

namespace PongWall
{
    public static class UiBoundsUtility
    {
        public static Rect GetLocalRect(RectTransform rectTransform, RectTransform coordinateSpace)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var minimum = coordinateSpace.InverseTransformPoint(corners[0]);
            var maximum = minimum;

            for (var index = 1; index < corners.Length; index++)
            {
                var localCorner = coordinateSpace.InverseTransformPoint(corners[index]);
                minimum = Vector3.Min(minimum, localCorner);
                maximum = Vector3.Max(maximum, localCorner);
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        public static Vector2 GetLocalCenter(RectTransform rectTransform, RectTransform coordinateSpace)
        {
            var bounds = GetLocalRect(rectTransform, coordinateSpace);
            return bounds.center;
        }

        public static Vector2 GetLocalHalfSize(RectTransform rectTransform, RectTransform coordinateSpace)
        {
            var bounds = GetLocalRect(rectTransform, coordinateSpace);
            return bounds.size * 0.5f;
        }

        public static Vector2 ClampCenterInRect(Vector2 targetCenter, Vector2 halfSize, Rect bounds)
        {
            var minimumX = bounds.xMin + halfSize.x;
            var maximumX = bounds.xMax - halfSize.x;
            var minimumY = bounds.yMin + halfSize.y;
            var maximumY = bounds.yMax - halfSize.y;

            if (minimumX > maximumX)
            {
                var centerX = (bounds.xMin + bounds.xMax) * 0.5f;
                minimumX = centerX;
                maximumX = centerX;
            }

            if (minimumY > maximumY)
            {
                var centerY = (bounds.yMin + bounds.yMax) * 0.5f;
                minimumY = centerY;
                maximumY = centerY;
            }

            return new Vector2(
                Mathf.Clamp(targetCenter.x, minimumX, maximumX),
                Mathf.Clamp(targetCenter.y, minimumY, maximumY));
        }
    }
}
