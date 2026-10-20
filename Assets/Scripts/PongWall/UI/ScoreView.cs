using DG.Tweening;
using UniRx;
using TMPro;
using UnityEngine;
using Zenject;

namespace PongWall
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [Header("Punch")]
        [SerializeField] private float _punchScale = 1.18f;
        [SerializeField] private float _punchDuration = 0.28f;

        private readonly CompositeDisposable _disposables = new();

        private RectTransform _rectTransform;
        private Color _baseColor = Color.white;

        [Inject]
        public void Construct(GameSession gameSession)
        {
            _rectTransform = _scoreLabel.rectTransform;
            _baseColor = _scoreLabel.color;

            gameSession.Score
                .Subscribe(OnScoreChanged)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _rectTransform?.DOKill();
        }

        private void OnValidate()
        {
            Debug.Assert(_scoreLabel != null, $"{nameof(_scoreLabel)} is not assigned.");
        }

        private void OnScoreChanged(int score)
        {
            if (score == 0)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.one;
                _scoreLabel.color = _baseColor;
                _scoreLabel.text = "0";
                return;
            }

            Punch();
            _scoreLabel.text = score.ToString();
        }

        private void Punch()
        {
            _rectTransform.DOKill();
            _rectTransform.localScale = Vector3.one * _punchScale;
            _scoreLabel.color = UiTheme.WithAlpha(UiTheme.Accent, 1f);

            _rectTransform.DOScale(Vector3.one, _punchDuration).SetEase(Ease.OutBack);
            _scoreLabel.DOColor(_baseColor, _punchDuration * 1.6f).SetEase(Ease.OutQuad);
        }
    }
}