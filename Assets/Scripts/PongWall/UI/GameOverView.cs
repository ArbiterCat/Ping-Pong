using DG.Tweening;
using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    public class GameOverView : MonoBehaviour, IInitializable
    {
        private const string NewBestBadgeName = "NewBestBadge";

        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private TextMeshProUGUI _finalScoreLabel;
        [SerializeField] private TextMeshProUGUI _bestScoreLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseButton;

        private GameSession _gameSession;
        private GameRestarter _gameRestarter;
        private BestScoreStore _bestScoreStore;
        private readonly CompositeDisposable _disposables = new();

        private RectTransform _card;
        private TextMeshProUGUI _title;
        private CanvasGroup _newBestGroup;
        private Tweener _scoreTween;
        private int _bestAtRunStart;

        [Inject]
        public void Construct(
            GameSession gameSession,
            GameRestarter gameRestarter,
            BestScoreStore bestScoreStore)
        {
            _gameSession = gameSession;
            _gameRestarter = gameRestarter;
            _bestScoreStore = bestScoreStore;

            gameSession.State
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);

            gameSession.Score
                .Subscribe(UpdateFinalScore)
                .AddTo(_disposables);
        }

        public void Initialize()
        {
            _finalScoreLabel.raycastTarget = false;

            if (_bestScoreLabel != null)
                _bestScoreLabel.raycastTarget = false;

            // Older builds of the builder put a PAUSE button on the game over card.
            // Pausing a finished run does nothing, so hide the dead control.
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.gameObject.SetActive(false);
            }

            _restartButton.onClick.AddListener(OnRestartClicked);

            _card = transform.Find("GameOverCard") as RectTransform;
            if (_card != null)
            {
                _title = _card.Find("GameOverTitle")?.GetComponent<TextMeshProUGUI>();
                BuildNewBestBadge();
                EnsureRestartHint();
            }

            _bestAtRunStart = _bestScoreStore.BestScore;
            HideImmediate();
        }

        private void Update()
        {
            if (_gameSession == null || _gameSession.State.Value != GameState.GameOver)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                _gameRestarter.Restart();
        }

        private void OnDestroy()
        {
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _disposables.Dispose();
            _scoreTween?.Kill();
            _panel.DOKill();
            _newBestGroup?.DOKill();
        }

        private void OnValidate()
        {
            Debug.Assert(_panel != null, $"{nameof(_panel)} is not assigned.");
            Debug.Assert(_finalScoreLabel != null, $"{nameof(_finalScoreLabel)} is not assigned.");
            Debug.Assert(_restartButton != null, $"{nameof(_restartButton)} is not assigned.");
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                _bestScoreStore.Flush();

                var score = _gameSession.Score.Value;
                var isNewBest = score > 0 && score > _bestAtRunStart;
                Show(_gameSession.LastEndWasImmediate, score, isNewBest);
                return;
            }

            // A fresh run starts here, so remember the record it has to beat.
            _bestAtRunStart = _bestScoreStore.BestScore;
            HideImmediate();
        }

        private void UpdateFinalScore(int score)
        {
            _bestScoreStore.BestScore = score;
            SetFinalScoreText(score);

            if (_bestScoreLabel == null)
                return;

            _bestScoreLabel.text = $"Best: {_bestScoreStore.BestScore}";
        }

        private void OnRestartClicked()
        {
            _gameRestarter.Restart();
        }

        private void Show(bool immediate, int score, bool newBest)
        {
            _panel.DOKill();
            _panel.interactable = true;
            _panel.blocksRaycasts = true;
            _restartButton.interactable = true;

            if (immediate)
            {
                _panel.alpha = 1f;
                _panel.transform.localScale = Vector3.one;
                SetFinalScoreText(score);
            }
            else
            {
                _panel.alpha = 0f;
                _panel.transform.localScale = Vector3.one * 0.9f;
                _panel.DOFade(1f, 0.25f);
                _panel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                AnimateFinalScore(score);

                if (_title != null)
                {
                    _title.rectTransform.DOKill();
                    _title.rectTransform.localScale = Vector3.one;
                    _title.rectTransform
                        .DOPunchScale(Vector3.one * 0.12f, 0.45f, 8, 0.6f)
                        .SetDelay(0.10f);
                }
            }

            AnimateNewBestBadge(newBest, immediate);
        }

        private void HideImmediate()
        {
            _panel.DOKill();
            _scoreTween?.Kill();
            _newBestGroup?.DOKill();

            if (_newBestGroup != null)
                _newBestGroup.alpha = 0f;

            _panel.alpha = 0f;
            _panel.interactable = false;
            _panel.blocksRaycasts = false;
            _panel.transform.localScale = Vector3.one;
        }

        private void AnimateFinalScore(int score)
        {
            _scoreTween?.Kill();

            if (score <= 0)
            {
                SetFinalScoreText(0);
                return;
            }

            var displayed = 0;
            SetFinalScoreText(0);

            var duration = Mathf.Clamp(0.40f + score * 0.015f, 0.45f, 1.15f);
            _scoreTween = DOTween
                .To(
                    () => displayed,
                    value =>
                    {
                        displayed = value;
                        SetFinalScoreText(value);
                    },
                    score,
                    duration)
                .SetEase(Ease.OutCubic);
        }

        private void SetFinalScoreText(int score)
        {
            if (_finalScoreLabel != null)
                _finalScoreLabel.text = $"Score: {score}";
        }

        private void BuildNewBestBadge()
        {
            if (_newBestGroup != null || _card == null)
                return;

            var existing = _card.Find(NewBestBadgeName) as RectTransform;
            if (existing != null)
            {
                _newBestGroup = existing.GetComponent<CanvasGroup>();
                return;
            }

            var root = CreateUiObject(_card, NewBestBadgeName);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(380f, 64f);
            root.anchoredPosition = new Vector2(0f, -128f);

            var glow = CreateImage(root, "Glow", UiTheme.Glow, UiTheme.WithAlpha(UiTheme.Gold, 0.16f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            glow.rectTransform.anchoredPosition = Vector2.zero;
            glow.rectTransform.sizeDelta = new Vector2(520f, 170f);

            var background = CreateImage(root, "Pill", UiTheme.Pill, UiTheme.WithAlpha(UiTheme.Accent, 0.14f));
            background.type = Image.Type.Sliced;

            var label = CreateLabel(root, "Label", "NEW BEST");
            label.fontSize = 34f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiTheme.Gold;

            _newBestGroup = root.gameObject.AddComponent<CanvasGroup>();
            _newBestGroup.alpha = 0f;
            _newBestGroup.interactable = false;
            _newBestGroup.blocksRaycasts = false;
        }

        private void EnsureRestartHint()
        {
            if (_card == null || _card.Find("GameOverHint") != null)
                return;

            var hint = CreateLabel(_card, "GameOverHint", "R / ENTER  -  RESTART");
            var rect = hint.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(800f, 44f);
            rect.anchoredPosition = new Vector2(0f, 66f);
            hint.fontSize = 30f;
            hint.characterSpacing = 10f;
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = UiTheme.TextFaint;
        }

        private void AnimateNewBestBadge(bool newBest, bool immediate)
        {
            if (_newBestGroup == null)
                return;

            _newBestGroup.DOKill();
            var rect = _newBestGroup.transform;

            if (!newBest)
            {
                _newBestGroup.alpha = 0f;
                rect.localScale = Vector3.one;
                return;
            }

            if (immediate)
            {
                _newBestGroup.alpha = 1f;
                rect.localScale = Vector3.one;
                return;
            }

            _newBestGroup.alpha = 0f;
            rect.localScale = Vector3.one * 0.7f;
            _newBestGroup.DOFade(1f, 0.22f).SetDelay(0.28f);
            rect.DOScale(1f, 0.38f).SetEase(Ease.OutBack).SetDelay(0.28f);
        }

        private static RectTransform CreateUiObject(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rect = CreateUiObject(parent, name);
            if (rect.GetComponent<CanvasRenderer>() == null)
                rect.gameObject.AddComponent<CanvasRenderer>();

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text)
        {
            var rect = CreateUiObject(parent, name);
            if (rect.GetComponent<CanvasRenderer>() == null)
                rect.gameObject.AddComponent<CanvasRenderer>();

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.raycastTarget = false;

            var rectTransform = label.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return label;
        }
    }
}
