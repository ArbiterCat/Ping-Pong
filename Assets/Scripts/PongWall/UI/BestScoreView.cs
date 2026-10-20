using UniRx;
using UnityEngine;
using Zenject;

namespace PongWall
{
    /// <summary>
    /// Shows the persisted best score in the HUD and keeps it in sync while playing.
    /// Sits on the same GameObject as the label it drives.
    /// </summary>
    public class BestScoreView : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI _label;
        [SerializeField] private bool _showPrefix;

        private readonly CompositeDisposable _disposables = new();
        private string _template = string.Empty;

        [Inject]
        public void Construct(GameSession gameSession, BestScoreStore bestScoreStore)
        {
            if (_label == null)
                _label = GetComponent<TMPro.TextMeshProUGUI>();

            if (_label == null)
                return;

            if (_showPrefix && !string.IsNullOrEmpty(_label.text))
                _template = ExtractPrefix(_label.text);
            else
                _template = string.Empty;

            Render(bestScoreStore.BestScore);

            gameSession.Score
                .Subscribe(_ =>
                {
                    bestScoreStore.BestScore = gameSession.Score.Value;
                    Render(bestScoreStore.BestScore);
                })
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        private void Render(int bestScore)
        {
            if (_label == null)
                return;

            _label.text = $"{_template}{bestScore}";
        }

        /// <summary>
        /// The scene label ships as "Best: 0". Keep the text before the number,
        /// otherwise every refresh would append to the old value ("Best: 014").
        /// </summary>
        private static string ExtractPrefix(string labelText)
        {
            var end = labelText.Length;
            while (end > 0 && (char.IsDigit(labelText[end - 1]) || char.IsWhiteSpace(labelText[end - 1])))
                end--;

            if (end <= 0)
                return "Best: ";

            return labelText.Substring(0, end) + " ";
        }

        private void OnValidate()
        {
            if (_label == null)
                _label = GetComponent<TMPro.TextMeshProUGUI>();
        }
    }
}