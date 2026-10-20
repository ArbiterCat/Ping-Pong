using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PongWall
{
    /// <summary>
    /// Adds a small amount of life to any button: lift on hover, dip on press.
    /// Attach next to a Button component.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField, Range(1f, 1.25f)] private float _hoverScale = 1.04f;
        [SerializeField, Range(0.7f, 1f)] private float _pressScale = 0.96f;
        [SerializeField] private float _duration = 0.12f;

        private RectTransform _rectTransform;
        private ButtonState _state = ButtonState.Normal;

        private enum ButtonState
        {
            Normal,
            Hovered,
            Pressed
        }

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _rectTransform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            _rectTransform?.DOKill();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetState(ButtonState.Hovered);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetState(ButtonState.Normal);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetState(ButtonState.Pressed);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetState(ButtonState.Hovered);
        }

        private void SetState(ButtonState state)
        {
            if (_state == state)
                return;

            _state = state;
            _rectTransform.DOKill();

            var target = state switch
            {
                ButtonState.Hovered => Vector3.one * _hoverScale,
                ButtonState.Pressed => Vector3.one * _pressScale,
                _ => Vector3.one
            };

            _rectTransform.DOScale(target, _duration).SetEase(Ease.OutQuad);
        }
    }
}