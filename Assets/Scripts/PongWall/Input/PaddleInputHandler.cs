using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    [RequireComponent(typeof(RectTransform))]
    public class PaddleInputHandler : MonoBehaviour, IDisposable
    {
        private GameConfig _gameConfig;
        private GameSession _gameSession;
        private PlayFieldBounds _playFieldBounds;
        private Paddle _paddle;
        private Canvas _canvas;
        private RectTransform _rectTransform;
        private readonly CompositeDisposable _disposables = new();
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerEventData;

        [Inject]
        public void Construct(
            GameConfig gameConfig,
            GameSession gameSession,
            PlayFieldBounds playFieldBounds,
            Paddle paddle,
            Canvas canvas)
        {
            _gameConfig = gameConfig;
            _gameSession = gameSession;
            _playFieldBounds = playFieldBounds;
            _paddle = paddle;
            _canvas = canvas;
            _rectTransform = _paddle.transform as RectTransform;

            _gameSession.State
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
                _rectTransform.DOKill();
        }

        private void Update()
        {
            if (_gameSession == null || !_gameSession.IsPlaying || _gameSession.IsPaused)
                return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
                return;

            var screenPosition = pointer.position.ReadValue();
            if (IsPointerOverButton(screenPosition))
                return;

            MoveToScreenPosition(screenPosition);
        }

        public void MoveToScreenPosition(Vector2 screenPosition)
        {
            var gameplayPanel = _playFieldBounds.GameplayPanel;
            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gameplayPanel,
                    screenPosition,
                    camera,
                    out var localPoint))
                return;

            localPoint.y += _gameConfig.PaddleTouchYOffset;

            var halfSize = _paddle.GetHalfSize();
            var targetPosition = _playFieldBounds.ClampPaddlePosition(localPoint, halfSize);

            _rectTransform.DOKill();
            _rectTransform
                .DOAnchorPos(targetPosition, _gameConfig.PaddleFollowDuration)
                .SetEase(Ease.OutQuad);
        }

        public void Dispose()
        {
            _rectTransform.DOKill();
        }

        private bool IsPointerOverButton(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            _pointerEventData ??= new PointerEventData(eventSystem);
            _pointerEventData.Reset();
            _pointerEventData.position = screenPosition;

            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _raycastResults);

            for (var index = 0; index < _raycastResults.Count; index++)
            {
                if (_raycastResults[index].gameObject.GetComponentInParent<Button>() != null)
                    return true;
            }

            return false;
        }
    }
}
