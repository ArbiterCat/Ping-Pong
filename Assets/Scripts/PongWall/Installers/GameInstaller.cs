using UnityEngine;
using Zenject;

namespace PongWall
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private GameObject _ballPrefab;
        [SerializeField] private GameObject _ballShadowPrefab;

        public override void InstallBindings()
        {
            Container.BindInstance(_gameConfig);
            Container.Bind<Canvas>().FromComponentInHierarchy().AsSingle();
            Container.Bind<PlayFieldBounds>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameSession>().AsSingle();
            Container.Bind<BestScoreStore>().AsSingle();
            Container.Bind<GameFxSignals>().AsSingle();
            Container.Bind<GameRestarter>().AsSingle().NonLazy();
            Container.Bind<BallRoster>().AsSingle();
            Container.BindInterfacesAndSelfTo<BallCollisionResolver>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameFxController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<HudDecorationView>().AsSingle().NonLazy();

            Container.Bind<Paddle>().FromComponentInHierarchy().AsSingle();
            Container.Bind<Ball>().FromComponentInHierarchy().AsSingle();
            Container.Bind<BallShadowFollower>().FromComponentInHierarchy().AsSingle();
            Container.BindFactory<Ball, Ball.Factory>().FromComponentInNewPrefab(_ballPrefab);
            Container.BindFactory<BallShadowFollower, BallShadowFollower.Factory>()
                .FromComponentInNewPrefab(_ballShadowPrefab);
            Container.Bind<GameFlowController>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PaddleInputHandler>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ScoreView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ExtraBallButtonsView>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GameOverView>()
                .FromComponentInHierarchy(includeInactive: true)
                .AsSingle()
                .NonLazy();

            BindOptionalSceneComponent<BestScoreView>();
            BindOptionalSceneComponent<PauseView>();
        }

        /// <summary>
        /// Binds a UI view only when it is actually present in the scene, so a missing
        /// (not yet generated) UI object degrades gracefully instead of breaking the DI graph.
        /// </summary>
        private void BindOptionalSceneComponent<T>() where T : Component
        {
            var instance = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (instance == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GameInstaller)}] {nameof(T)} is not present in the scene. " +
                    $"Run 'PongWall/Setup UI' to create it.");
                return;
            }

            Container.BindInterfacesAndSelfTo(typeof(T))
                .FromInstance(instance)
                .AsSingle()
                .NonLazy();
        }

        private void OnValidate()
        {
            Debug.Assert(_gameConfig != null, $"{nameof(_gameConfig)} is not assigned.");
            Debug.Assert(_ballPrefab != null, $"{nameof(_ballPrefab)} is not assigned.");
            Debug.Assert(_ballShadowPrefab != null, $"{nameof(_ballShadowPrefab)} is not assigned.");
        }
    }
}
