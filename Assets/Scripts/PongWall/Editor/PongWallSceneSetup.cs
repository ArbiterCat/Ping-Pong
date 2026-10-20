#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PongWall.Editor
{
    [InitializeOnLoad]
    public static class PongWallSceneSetupBootstrap
    {
        static PongWallSceneSetupBootstrap()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (GameObject.Find("GameplayPanel") != null)
                return;

            if (GameObject.Find("Gameplay") == null && GameObject.Find("Paddle") == null)
                return;

            PongWallSceneSetup.SetupGameplayCanvas();
        }
    }

    public static class PongWallSceneSetup
    {
        private const string MenuPath = "PongWall/Setup Gameplay Canvas";
        private const string GameConfigPath = "Assets/Configs/GameConfig.asset";

        [MenuItem(MenuPath)]
        public static void SetupGameplayCanvas()
        {
            SetupInternal();
        }

        public static void ExecuteSetupFromBatch()
        {
            SetupInternal();
            EditorSceneManager.SaveOpenScenes();
        }

        private static void SetupInternal()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene")
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found in scene.");
                return;
            }

            RemoveLegacyGameplay();

            var gameplayPanel = CreateOrGetGameplayPanel(canvas.transform);
            var playFieldBounds = EnsureComponent<PlayFieldBounds>(gameplayPanel.gameObject);
            EnsureComponent<GameFlowController>(gameplayPanel.gameObject);

            var wall = CreateUiElement("Wall", gameplayPanel.transform, new Color(0.7f, 0.7f, 0.7f, 1f));
            StretchRect(wall, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.92f));
            var wallSurface = EnsureComponent<WallSurface>(wall);

            var deathLine = CreateUiElement("DeathLine", gameplayPanel.transform, new Color(1f, 0.2f, 0.2f, 0.35f));
            StretchRect(deathLine, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.1f));
            var deathLineMarker = EnsureComponent<DeathLineMarker>(deathLine);

            var leftBound = CreateUiElement("LeftBound", gameplayPanel.transform, new Color(1f, 0.6f, 0.2f, 0.25f));
            StretchRect(leftBound, new Vector2(0f, 0.1f), new Vector2(0.03f, 0.82f));
            var leftBoundMarker = EnsureComponent<SideBoundMarker>(leftBound);
            SetSideBoundKind(leftBoundMarker, SideBoundKind.Left);

            var rightBound = CreateUiElement("RightBound", gameplayPanel.transform, new Color(1f, 0.6f, 0.2f, 0.25f));
            StretchRect(rightBound, new Vector2(0.97f, 0.1f), new Vector2(1f, 0.82f));
            var rightBoundMarker = EnsureComponent<SideBoundMarker>(rightBound);
            SetSideBoundKind(rightBoundMarker, SideBoundKind.Right);

            var paddleZone = CreateUiElement("PaddleMovementZone", gameplayPanel.transform, new Color(0.2f, 0.9f, 0.3f, 0.15f));
            StretchRect(paddleZone, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.45f));
            var paddleMovementZone = EnsureComponent<PaddleMovementZone>(paddleZone);

            var ballsRoot = CreateUiElement("BallsRoot", gameplayPanel.transform, new Color(0f, 0f, 0f, 0f));
            StretchRect(ballsRoot, Vector2.zero, Vector2.one);
            ballsRoot.GetComponent<Image>().raycastTarget = false;

            var ball = CreateUiElement("Ball", ballsRoot.transform, new Color(1f, 1f, 1f, 1f));
            var ballSize = GetConfiguredBallSize();
            SetCenterRect(ball, new Vector2(0f, -180f), new Vector2(ballSize, ballSize));
            SetCircleSprite(ball);
            EnsureComponent<Ball>(ball);

            var paddle = CreateUiElement("Paddle", gameplayPanel.transform, new Color(0.75f, 0.75f, 0.75f, 1f));
            SetCenterRect(paddle, new Vector2(0f, -280f), new Vector2(220f, 70f));
            EnsureComponent<Paddle>(paddle);
            EnsureComponent<PaddleInputHandler>(paddle);

            AssignPlayFieldReferences(
                playFieldBounds,
                gameplayPanel,
                paddleMovementZone,
                wallSurface,
                deathLineMarker,
                leftBoundMarker,
                rightBoundMarker,
                ballsRoot.transform as RectTransform);

            playFieldBounds.Refresh();
            EditorUtility.SetDirty(gameplayPanel.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Pong Wall gameplay canvas setup complete.");
        }

        private static void RemoveLegacyGameplay()
        {
            var legacyGameplay = GameObject.Find("Gameplay");
            if (legacyGameplay != null)
                Object.DestroyImmediate(legacyGameplay);

            var legacyFlow = GameObject.Find("GameFlowController");
            if (legacyFlow != null)
                Object.DestroyImmediate(legacyFlow);
        }

        private static RectTransform CreateOrGetGameplayPanel(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("GameplayPanel");
            if (existing != null)
                return existing as RectTransform;

            var panelObject = new GameObject(
                "GameplayPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(PlayFieldBounds),
                typeof(GameFlowController));

            panelObject.transform.SetParent(canvasTransform, false);
            panelObject.layer = canvasTransform.gameObject.layer;

            var panelRect = panelObject.GetComponent<RectTransform>();
            StretchRect(panelObject, Vector2.zero, Vector2.one);

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            return panelRect;
        }

        private static GameObject CreateUiElement(string name, Transform parent, Color color)
        {
            var elementObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            elementObject.transform.SetParent(parent, false);
            elementObject.layer = parent.gameObject.layer;

            var image = elementObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return elementObject;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
                return component;

            return target.AddComponent<T>();
        }

        private static void StretchRect(GameObject target, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rectTransform = target.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetCenterRect(GameObject target, Vector2 anchoredPosition, Vector2 size)
        {
            var rectTransform = target.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private static float GetConfiguredBallSize()
        {
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            return gameConfig != null ? gameConfig.BallSize : 45f;
        }

        private static void SetCircleSprite(GameObject target)
        {
            var image = target.GetComponent<Image>();
            var circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (circleSprite != null)
                image.sprite = circleSprite;
        }

        private static void SetSideBoundKind(SideBoundMarker marker, SideBoundKind kind)
        {
            var serializedObject = new SerializedObject(marker);
            serializedObject.FindProperty("_side").enumValueIndex = (int)kind;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPlayFieldReferences(
            PlayFieldBounds playFieldBounds,
            RectTransform gameplayPanel,
            PaddleMovementZone paddleMovementZone,
            WallSurface wallSurface,
            DeathLineMarker deathLineMarker,
            SideBoundMarker leftBoundMarker,
            SideBoundMarker rightBoundMarker,
            RectTransform ballsRoot)
        {
            var serializedObject = new SerializedObject(playFieldBounds);
            serializedObject.FindProperty("_gameplayPanel").objectReferenceValue = gameplayPanel;
            serializedObject.FindProperty("_paddleMovementZone").objectReferenceValue = paddleMovementZone;
            serializedObject.FindProperty("_wallSurface").objectReferenceValue = wallSurface;
            serializedObject.FindProperty("_deathLine").objectReferenceValue = deathLineMarker;
            serializedObject.FindProperty("_leftBound").objectReferenceValue = leftBoundMarker;
            serializedObject.FindProperty("_rightBound").objectReferenceValue = rightBoundMarker;
            serializedObject.FindProperty("_ballsRoot").objectReferenceValue = ballsRoot;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
