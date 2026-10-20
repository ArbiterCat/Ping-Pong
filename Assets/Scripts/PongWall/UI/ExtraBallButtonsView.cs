using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    public class ExtraBallButtonsView : MonoBehaviour
    {
        [SerializeField] private Button _spawnButton;
        [SerializeField] private Button _removeButton;

        private BallRoster _ballRoster;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(BallRoster ballRoster, GameSession gameSession)
        {
            _ballRoster = ballRoster;
            _spawnButton.onClick.AddListener(OnSpawnClicked);
            _removeButton.onClick.AddListener(OnRemoveClicked);

            Observable.CombineLatest(
                    gameSession.State,
                    ballRoster.ExtraCount,
                    (state, extraCount) => (
                        IsPlaying: state == GameState.Playing,
                        ExtraCount: extraCount,
                        CanSpawn: ballRoster.CanSpawnExtra))
                .Subscribe(UpdateInteractable)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _spawnButton.onClick.RemoveListener(OnSpawnClicked);
            _removeButton.onClick.RemoveListener(OnRemoveClicked);
            _disposables.Dispose();
        }

        private void OnValidate()
        {
            Debug.Assert(_spawnButton != null, $"{nameof(_spawnButton)} is not assigned.");
            Debug.Assert(_removeButton != null, $"{nameof(_removeButton)} is not assigned.");
        }

        private void UpdateInteractable((bool IsPlaying, int ExtraCount, bool CanSpawn) state)
        {
            _spawnButton.interactable = state.IsPlaying && state.CanSpawn;
            _removeButton.interactable = state.IsPlaying && state.ExtraCount > 0;
        }

        private void OnSpawnClicked()
        {
            _ballRoster.SpawnExtra();
        }

        private void OnRemoveClicked()
        {
            _ballRoster.RemoveLastExtra();
        }
    }
}
