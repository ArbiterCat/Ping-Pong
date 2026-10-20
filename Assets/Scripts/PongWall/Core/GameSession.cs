using UniRx;

namespace PongWall
{
    public class GameSession
    {
        private readonly ReactiveProperty<int> _score = new(0);
        private readonly ReactiveProperty<GameState> _state = new(GameState.Playing);
        private readonly ReactiveProperty<bool> _paused = new(false);

        public IReadOnlyReactiveProperty<int> Score => _score;
        public IReadOnlyReactiveProperty<GameState> State => _state;
        public IReadOnlyReactiveProperty<bool> Paused => _paused;
        public bool IsPlaying => _state.Value == GameState.Playing;
        public bool IsPaused => _paused.Value;
        public bool LastEndWasImmediate { get; private set; }

        public void SetPaused(bool paused)
        {
            if (!IsPlaying)
                return;

            _paused.Value = paused;
        }

        public void AddScore(int amount)
        {
            if (!IsPlaying)
                return;

            _score.Value += amount;
        }

        public void EndGame(bool immediate = false)
        {
            if (!IsPlaying)
                return;

            LastEndWasImmediate = immediate;
            _state.Value = GameState.GameOver;
        }

        public void ResetSession()
        {
            _score.Value = 0;
            LastEndWasImmediate = false;
            _state.Value = GameState.Playing;
            _paused.Value = false;
        }
    }
}
