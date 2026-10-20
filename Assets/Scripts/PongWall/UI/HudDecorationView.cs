using System;
using DG.Tweening;
using UniRx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    /// <summary>
    /// Small runtime HUD additions: a live ball counter, a "new best" ribbon and
    /// the control hint. Built in code so the scene does not have to be rebuilt.
    /// </summary>
    public class HudDecorationView : IInitializable, IDisposable
    {
        private const string LayerName = "HudDecorLayer";

        private readonly Canvas _canvas;
        private readonly GameSession _gameSession;
        private readonly BallRoster _ballRoster;
        private readonly BestScoreStore _bestScoreStore;
        private readonly CompositeDisposable _disposables = new();

        private RectTransform _layer;
        private RectTransform _ballsChip;
        private TextMeshProUGUI _ballsValue;
        private CanvasGroup _newBestGroup;
        private RectTransform _newBestRoot;
        private Tweener _newBestTween;
        private CanvasGroup _hintGroup;
        private bool _hintHidden;
        private int _bestAtRunStart;

        [Inject]
        public HudDecorationView(
            Canvas canvas,
            GameSession gameSession,
            BallRoster ballRoster,
            BestScoreStore bestScoreStore)
        {
            _canvas = canvas;
            _gameSession = gameSession;
            _ballRoster = ballRoster;
            _bestScoreStore = bestScoreStore;
        }

        public void Initialize()
        {
            BuildLayer();
            BuildBallCounter();
            BuildNewBestRibbon();
            BuildControlHint();

            _bestAtRunStart = _bestScoreStore.BestScore;
            UpdateBallCount(_ballRoster.ExtraCount.Value);

            _ballRoster.ExtraCount
                .Subscribe(UpdateBallCount)
                .AddTo(_disposables);

            _gameSession.Score
                .Subscribe(OnScoreChanged)
                .AddTo(_disposables);

            _gameSession.State
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _newBestTween?.Kill();
            _newBestGroup?.DOKill();
            _hintGroup?.DOKill();
            _ballsChip?.DOKill();

            if (_layer != null)
                UnityEngine.Object.Destroy(_layer.gameObject);
        }

        private void BuildLayer()
        {
            _layer = _canvas.transform.Find(LayerName) as RectTransform;
            if (_layer == null)
            {
                var gameObject = new GameObject(LayerName, typeof(RectTransform));
                gameObject.layer = _canvas.gameObject.layer;

                _layer = gameObject.GetComponent<RectTransform>();
                _layer.SetParent(_canvas.transform, false);
                _layer.anchorMin = Vector2.zero;
                _layer.anchorMax = Vector2.one;
                _layer.pivot = new Vector2(0.5f, 0.5f);
                _layer.offsetMin = Vector2.zero;
                _layer.offsetMax = Vector2.zero;
                _layer.localScale = Vector3.one;
            }

            // Keep the decoration below the pause and game over scrims.
            var pausePanel = _canvas.transform.Find("PausePanel");
            if (pausePanel != null)
                _layer.SetSiblingIndex(pausePanel.GetSiblingIndex());
        }

        private void BuildBallCounter()
        {
            _ballsChip = CreateRect(_layer, "BallCounterChip");
            _ballsChip.anchorMin = _ballsChip.anchorMax = new Vector2(0.5f, 1f);
            _ballsChip.pivot = new Vector2(0.5f, 1f);
            _ballsChip.sizeDelta = new Vector2(286f, 92f);
            _ballsChip.anchoredPosition = new Vector2(0f, -44f);

            var background = AddImage(_ballsChip, "Background", UiTheme.Pill, UiTheme.SurfaceTranslucent);
            background.type = Image.Type.Sliced;

            var dot = AddImage(_ballsChip, "Dot", UiTheme.Disc, UiTheme.WithAlpha(UiTheme.Accent, 0.85f));
            var dotRect = dot.rectTransform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.sizeDelta = new Vector2(14f, 14f);
            dotRect.anchoredPosition = new Vector2(26f, 0f);

            var caption = AddText(_ballsChip, "Caption", "BALLS");
            var captionRect = caption.rectTransform;
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0f, 0.5f);
            captionRect.pivot = new Vector2(0f, 0.5f);
            captionRect.sizeDelta = new Vector2(160f, 40f);
            captionRect.anchoredPosition = new Vector2(52f, 0f);
            caption.fontSize = 24f;
            caption.fontStyle = FontStyles.Bold;
            caption.characterSpacing = 10f;
            caption.alignment = TextAlignmentOptions.Left;
            caption.color = UiTheme.TextFaint;

            _ballsValue = AddText(_ballsChip, "Value", "1");
            var valueRect = _ballsValue.rectTransform;
            valueRect.anchorMin = valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.sizeDelta = new Vector2(96f, 64f);
            valueRect.anchoredPosition = new Vector2(-26f, 0f);
            _ballsValue.fontSize = 48f;
            _ballsValue.fontStyle = FontStyles.Bold;
            _ballsValue.alignment = TextAlignmentOptions.Right;
            _ballsValue.color = UiTheme.Accent;
        }

        private void BuildNewBestRibbon()
        {
            _newBestRoot = CreateRect(_layer, "HudNewBestRibbon");
            _newBestRoot.anchorMin = _newBestRoot.anchorMax = new Vector2(0.5f, 1f);
            _newBestRoot.pivot = new Vector2(0.5f, 1f);
            _newBestRoot.sizeDelta = new Vector2(300f, 52f);
            _newBestRoot.anchoredPosition = new Vector2(0f, -148f);

            var background = AddImage(_newBestRoot, "Background", UiTheme.Pill, UiTheme.WithAlpha(UiTheme.Gold, 0.16f));
            background.type = Image.Type.Sliced;

            var label = AddText(_newBestRoot, "Label", "NEW BEST");
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 12f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiTheme.Gold;

            _newBestGroup = _newBestRoot.gameObject.AddComponent<CanvasGroup>();
            _newBestGroup.alpha = 0f;
            _newBestGroup.interactable = false;
            _newBestGroup.blocksRaycasts = false;
        }

        private void BuildControlHint()
        {
            var root = CreateRect(_layer, "ControlHint");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(620f, 58f);
            root.anchoredPosition = new Vector2(0f, 34f);

            var background = AddImage(root, "Background", UiTheme.Pill, UiTheme.WithAlpha(UiTheme.Surface, 0.55f));
            background.type = Image.Type.Sliced;

            var label = AddText(root, "Label", "HOLD & DRAG TO MOVE");
            label.fontSize = 25f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 7f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiTheme.TextFaint;

            _hintGroup = root.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.alpha = 0.92f;
            _hintGroup.interactable = false;
            _hintGroup.blocksRaycasts = false;

            _hintGroup
                .DOFade(0f, 0.9f)
                .SetDelay(4.5f)
                .SetUpdate(true)
                .OnComplete(() => _hintGroup.blocksRaycasts = false);
        }

        private void UpdateBallCount(int extraCount)
        {
            if (_ballsValue == null)
                return;

            var total = extraCount + 1;
            _ballsValue.text = total.ToString();
            _ballsValue.color = total > 1 ? UiTheme.Gold : UiTheme.Accent;

            _ballsChip.DOKill();
            _ballsChip.localScale = Vector3.one * 1.08f;
            _ballsChip.DOScale(Vector3.one, 0.24f).SetEase(Ease.OutBack);
        }

        private void OnScoreChanged(int score)
        {
            if (score > 0)
                HideControlHint();

            if (!_gameSession.IsPlaying)
                return;

            var isNewBest = score > 0 && score > _bestAtRunStart;
            if (isNewBest)
                ShowNewBest();
            else if (!_gameSession.IsPaused)
                HideNewBest();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                _bestAtRunStart = _bestScoreStore.BestScore;
                HideNewBest();
                return;
            }

            HideNewBest();
            HideControlHint();
        }

        private void ShowNewBest()
        {
            if (_newBestGroup == null || _newBestGroup.alpha > 0.01f)
                return;

            _newBestGroup.DOKill();
            _newBestRoot.DOKill();
            _newBestGroup.alpha = 1f;
            _newBestRoot.localScale = Vector3.one * 1.15f;
            _newBestRoot.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            _newBestTween?.Kill();
            _newBestTween = _newBestRoot
                .DOScale(1.035f, 0.75f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(0.32f);
        }

        private void HideNewBest()
        {
            if (_newBestGroup == null)
                return;

            _newBestTween?.Kill();
            _newBestGroup.DOKill();
            _newBestRoot.DOKill();
            _newBestTween = null;
            _newBestGroup.alpha = 0f;
            _newBestRoot.localScale = Vector3.one;
        }

        private void HideControlHint()
        {
            if (_hintHidden || _hintGroup == null)
                return;

            _hintHidden = true;
            _hintGroup.DOKill();
            _hintGroup.DOFade(0f, 0.35f).SetUpdate(true);
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;

            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image AddImage(RectTransform parent, string name, Sprite sprite, Color color)
        {
            var rect = CreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (rect.GetComponent<CanvasRenderer>() == null)
                rect.gameObject.AddComponent<CanvasRenderer>();

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, string text)
        {
            var rect = CreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (rect.GetComponent<CanvasRenderer>() == null)
                rect.gameObject.AddComponent<CanvasRenderer>();

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
    }
}
