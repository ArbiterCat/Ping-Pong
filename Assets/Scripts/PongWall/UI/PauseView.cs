using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    public class PauseView : MonoBehaviour, IInitializable
    {
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;

        private GameSession _gameSession;
        private GameRestarter _gameRestarter;

        [Inject]
        public void Construct(GameSession gameSession, GameRestarter gameRestarter)
        {
            _gameSession = gameSession;
            _gameRestarter = gameRestarter;
        }

        public void Initialize()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(Toggle);

            _continueButton.onClick.AddListener(OnContinueClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(Toggle);

            _continueButton.onClick.RemoveListener(OnContinueClicked);
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _panel.DOKill();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            Toggle();
        }

        public void Toggle()
        {
            if (_gameSession.IsPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (!_gameSession.IsPlaying || _gameSession.IsPaused)
                return;

            _gameSession.SetPaused(true);
            Time.timeScale = 0f;
            Show();
        }

        public void Resume()
        {
            if (!_gameSession.IsPaused)
                return;

            _gameSession.SetPaused(false);
            Time.timeScale = 1f;
            Hide();
        }

        private void OnContinueClicked()
        {
            Resume();
        }

        private void OnRestartClicked()
        {
            Resume();
            _gameRestarter.Restart();
        }

        private void Show()
        {
            _panel.DOKill();
            _panel.interactable = true;
            _panel.blocksRaycasts = true;
            _panel.alpha = 0f;
            _panel.transform.localScale = Vector3.one * 0.9f;
            _panel.DOFade(1f, 0.2f).SetUpdate(true);
            _panel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void Hide()
        {
            _panel.DOKill();
            _panel.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                _panel.interactable = false;
                _panel.blocksRaycasts = false;
            });
        }

        private void HideImmediate()
        {
            _panel.DOKill();
            _panel.alpha = 0f;
            _panel.interactable = false;
            _panel.blocksRaycasts = false;
            _panel.transform.localScale = Vector3.one;
        }

        private void OnValidate()
        {
            Debug.Assert(_panel != null, $"{nameof(_panel)} is not assigned.");
            Debug.Assert(_continueButton != null, $"{nameof(_continueButton)} is not assigned.");
            Debug.Assert(_restartButton != null, $"{nameof(_restartButton)} is not assigned.");
        }
    }
}