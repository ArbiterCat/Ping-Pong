using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace PongWall
{
    /// <summary>
    /// Presentation layer: hit sparks, wall ripples, ball trails, floating score
    /// popups and ambient dust. Runs entirely on uGUI images, so it needs no
    /// particle materials, prefabs or scene setup.
    /// </summary>
    public class GameFxController : IInitializable, ITickable, IDisposable
    {
        private const int ParticlePoolSize = 128;
        private const int RingPoolSize = 24;
        private const int LabelPoolSize = 14;
        private const int DustCount = 46;
        private const float TrailInterval = 0.035f;

        private readonly Canvas _canvas;
        private readonly BallRoster _ballRoster;
        private readonly GameSession _gameSession;
        private readonly GameFxSignals _signals;
        private readonly CompositeDisposable _disposables = new();
        private readonly List<Ball> _activeBalls = new();
        private readonly List<Particle> _particles = new(ParticlePoolSize);
        private readonly List<Ring> _rings = new(RingPoolSize);
        private readonly List<FloatingLabel> _labels = new(LabelPoolSize);
        private readonly List<Dust> _dust = new(DustCount);

        private RectTransform _fxLayer;
        private RectTransform _dustLayer;
        private Image _flash;
        private Color _flashColor = Color.clear;
        private float _flashAlpha;
        private float _trailTimer;
        private float _time;

        private Sprite _glowSprite;
        private Sprite _discSprite;
        private Sprite _ringSprite;
        private Sprite _solidSprite;

        [Inject]
        public GameFxController(
            Canvas canvas,
            BallRoster ballRoster,
            GameSession gameSession,
            GameFxSignals signals)
        {
            _canvas = canvas;
            _ballRoster = ballRoster;
            _gameSession = gameSession;
            _signals = signals;
        }

        public void Initialize()
        {
            _glowSprite = UiTheme.Glow;
            _discSprite = UiTheme.Disc;
            _ringSprite = UiTheme.Ring;
            _solidSprite = UiTheme.Solid;

            BuildLayers();
            BuildParticles();
            BuildRings();
            BuildLabels();
            BuildFlash();
            BuildDust();

            _signals.PaddleHit.Subscribe(OnPaddleHit).AddTo(_disposables);
            _signals.WallHit.Subscribe(OnWallHit).AddTo(_disposables);
            _signals.BallLost.Subscribe(OnBallLost).AddTo(_disposables);
            _signals.ExtraBallSpawned.Subscribe(OnExtraBallSpawned).AddTo(_disposables);
        }

        public void Tick()
        {
            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;
            _time += unscaledDeltaTime;

            UpdateParticles(deltaTime);
            UpdateRings(deltaTime);
            UpdateLabels(deltaTime);
            UpdateDust(unscaledDeltaTime);
            UpdateFlash(unscaledDeltaTime);

            if (_gameSession.IsPlaying && !_gameSession.IsPaused)
                EmitTrails(deltaTime);
        }

        public void Dispose()
        {
            _disposables.Dispose();

            if (_dustLayer != null)
                UnityEngine.Object.Destroy(_dustLayer.gameObject);
            if (_fxLayer != null)
                UnityEngine.Object.Destroy(_fxLayer.gameObject);
        }

        // ----------------------------------------------------------------- layers

        private void BuildLayers()
        {
            var gameplay = _canvas.transform.Find("GameplayPanel");
            var gameplayIndex = gameplay != null ? gameplay.GetSiblingIndex() : 3;

            _dustLayer = GetOrCreateLayer("AmbientFxLayer", gameplayIndex);

            var hud = _canvas.transform.Find("HudBar");
            var fxIndex = hud != null ? hud.GetSiblingIndex() : _canvas.transform.childCount;
            _fxLayer = GetOrCreateLayer("ScreenFxLayer", fxIndex);

            Canvas.ForceUpdateCanvases();
        }

        private RectTransform GetOrCreateLayer(string name, int siblingIndex)
        {
            var existing = _canvas.transform.Find(name) as RectTransform;
            if (existing != null)
            {
                existing.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, _canvas.transform.childCount - 1));
                return existing;
            }

            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = _canvas.gameObject.layer;

            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(_canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, _canvas.transform.childCount - 1));
            return rect;
        }

        // --------------------------------------------------------------- building

        private void BuildParticles()
        {
            for (var index = 0; index < ParticlePoolSize; index++)
            {
                var image = CreateImage(_fxLayer, "FxParticle");
                var particle = new Particle(image);
                particle.SetActive(false);
                _particles.Add(particle);
            }
        }

        private void BuildRings()
        {
            for (var index = 0; index < RingPoolSize; index++)
            {
                var image = CreateImage(_fxLayer, "FxRing");
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                var ring = new Ring(image);
                ring.SetActive(false);
                _rings.Add(ring);
            }
        }

        private void BuildLabels()
        {
            for (var index = 0; index < LabelPoolSize; index++)
            {
                var gameObject = new GameObject("FxLabel", typeof(RectTransform), typeof(CanvasRenderer));
                gameObject.layer = _canvas.gameObject.layer;

                var rect = gameObject.GetComponent<RectTransform>();
                rect.SetParent(_fxLayer, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(360f, 80f);

                var label = gameObject.AddComponent<TextMeshProUGUI>();
                label.font = TMP_Settings.defaultFontAsset;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                var floating = new FloatingLabel(label);
                floating.SetActive(false);
                _labels.Add(floating);
            }
        }

        private void BuildFlash()
        {
            _flash = CreateImage(_fxLayer, "FxFlash");
            _flash.sprite = _solidSprite;
            _flash.type = Image.Type.Simple;
            _flash.color = Color.clear;

            var rect = _flash.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
        }

        private void BuildDust()
        {
            Canvas.ForceUpdateCanvases();
            var area = _dustLayer.rect;
            var width = area.width > 1f ? area.width : 2960f;
            var height = area.height > 1f ? area.height : 1440f;
            var minX = -width * 0.5f;
            var minY = -height * 0.5f;

            for (var index = 0; index < DustCount; index++)
            {
                var image = CreateImage(_dustLayer, "AmbientDust");
                image.sprite = index % 4 == 0 ? _glowSprite : _discSprite;
                image.color = WithAlpha(Color.white, 0f);

                var size = UnityEngine.Random.Range(3f, 11f);
                var rect = image.rectTransform;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(
                    minX + UnityEngine.Random.value * width,
                    minY + UnityEngine.Random.value * height);

                var tint = index % 5 == 0
                    ? UiTheme.Info
                    : index % 3 == 0
                        ? UiTheme.Gold
                        : UiTheme.Accent;

                _dust.Add(new Dust(
                    rect,
                    image,
                    tint,
                    UnityEngine.Random.Range(10f, 30f),
                    UnityEngine.Random.Range(0.4f, 1.4f),
                    UnityEngine.Random.value * Mathf.PI * 2f,
                    UnityEngine.Random.Range(0.05f, 0.16f),
                    size));
            }
        }

        // ----------------------------------------------------------------- events

        private void OnPaddleHit(PaddleHitFx fx)
        {
            var position = ToLayerPosition(fx.WorldPosition);
            var intensity = Mathf.InverseLerp(300f, 2000f, fx.Speed);
            var count = Mathf.RoundToInt(Mathf.Lerp(9f, 18f, intensity));

            for (var index = 0; index < count; index++)
            {
                var angle = UnityEngine.Random.Range(0.12f, 0.88f) * Mathf.PI;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                direction.x += fx.HorizontalOffset * 0.45f;
                direction.Normalize();

                var tint = UnityEngine.Random.value < 0.22f ? UiTheme.Gold : UiTheme.Accent;
                var speed = UnityEngine.Random.Range(220f, 520f) * Mathf.Lerp(0.75f, 1.25f, intensity);
                var size = UnityEngine.Random.Range(9f, 22f);
                var lifetime = UnityEngine.Random.Range(0.32f, 0.68f);

                SpawnParticle(
                    position + new Vector2(UnityEngine.Random.Range(-12f, 12f), 10f),
                    direction * speed,
                    lifetime,
                    size,
                    size * 0.15f,
                    WithAlpha(tint, 0.95f),
                    WithAlpha(tint, 0f),
                    -820f,
                    1.1f,
                    UnityEngine.Random.value < 0.55f ? _glowSprite : _discSprite);
            }

            SpawnParticle(
                position,
                Vector2.up * 40f,
                0.24f,
                78f,
                10f,
                WithAlpha(Color.white, 0.55f),
                WithAlpha(UiTheme.Accent, 0f),
                0f,
                0f,
                _glowSprite);

            SpawnRing(position + Vector2.up * 8f, 44f, 190f, 0.38f, UiTheme.Accent);
            SpawnLabel(position + new Vector2(0f, 46f), "+1", UiTheme.Accent, 46f);
            AddFlash(UiTheme.Accent, 0.055f);
        }

        private void OnWallHit(WallHitFx fx)
        {
            var position = ToLayerPosition(fx.WorldPosition);
            var intensity = Mathf.InverseLerp(300f, 2000f, fx.Speed);
            var count = Mathf.RoundToInt(Mathf.Lerp(7f, 14f, intensity));

            for (var index = 0; index < count; index++)
            {
                var angle = UnityEngine.Random.Range(Mathf.PI * 1.06f, Mathf.PI * 1.94f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var tint = UnityEngine.Random.value < 0.3f ? UiTheme.Accent : UiTheme.Info;
                var speed = UnityEngine.Random.Range(180f, 460f) * Mathf.Lerp(0.7f, 1.2f, intensity);
                var size = UnityEngine.Random.Range(7f, 16f);

                SpawnParticle(
                    position,
                    direction * speed,
                    UnityEngine.Random.Range(0.28f, 0.58f),
                    size,
                    size * 0.1f,
                    WithAlpha(tint, 0.9f),
                    WithAlpha(tint, 0f),
                    -560f,
                    1.4f,
                    UnityEngine.Random.value < 0.4f ? _glowSprite : _discSprite);
            }

            SpawnParticle(
                position,
                Vector2.zero,
                0.28f,
                90f,
                24f,
                WithAlpha(UiTheme.Info, 0.45f),
                WithAlpha(UiTheme.Info, 0f),
                0f,
                0f,
                _glowSprite);

            SpawnRing(position, 60f, 250f, 0.45f, UiTheme.Info);
            AddFlash(UiTheme.Info, 0.032f);
        }

        private void OnBallLost(BallLostFx fx)
        {
            var position = ToLayerPosition(fx.WorldPosition);

            for (var index = 0; index < 24; index++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var tint = UnityEngine.Random.value < 0.3f ? UiTheme.Gold : UiTheme.Danger;
                var speed = UnityEngine.Random.Range(180f, 640f);
                var size = UnityEngine.Random.Range(9f, 24f);

                SpawnParticle(
                    position,
                    direction * speed,
                    UnityEngine.Random.Range(0.45f, 0.9f),
                    size,
                    size * 0.1f,
                    WithAlpha(tint, 0.95f),
                    WithAlpha(tint, 0f),
                    -680f,
                    0.9f,
                    UnityEngine.Random.value < 0.5f ? _glowSprite : _discSprite);
            }

            SpawnRing(position, 50f, 320f, 0.55f, UiTheme.Danger);
            AddFlash(UiTheme.Danger, 0.07f);
        }

        private void OnExtraBallSpawned(ExtraBallFx fx)
        {
            var position = ToLayerPosition(fx.WorldPosition);

            for (var index = 0; index < 12; index++)
            {
                var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var speed = UnityEngine.Random.Range(160f, 400f);

                SpawnParticle(
                    position,
                    direction * speed,
                    UnityEngine.Random.Range(0.3f, 0.6f),
                    UnityEngine.Random.Range(7f, 15f),
                    0f,
                    WithAlpha(UiTheme.Gold, 0.9f),
                    WithAlpha(UiTheme.Gold, 0f),
                    -420f,
                    1.2f,
                    _discSprite);
            }

            SpawnRing(position, 40f, 170f, 0.35f, UiTheme.Gold);
            SpawnLabel(position + new Vector2(0f, 42f), "EXTRA BALL", UiTheme.Gold, 32f);
            AddFlash(UiTheme.Gold, 0.04f);
        }

        // ---------------------------------------------------------------- updates

        private void UpdateParticles(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (var index = 0; index < _particles.Count; index++)
            {
                var particle = _particles[index];
                if (!particle.Active)
                    continue;

                particle.Age += deltaTime;
                if (particle.Age >= particle.Lifetime)
                {
                    particle.SetActive(false);
                    continue;
                }

                var t = Mathf.Clamp01(particle.Age / particle.Lifetime);
                particle.Velocity.y += particle.Gravity * deltaTime;
                particle.Velocity *= Mathf.Max(0f, 1f - particle.Drag * deltaTime);
                particle.Rect.anchoredPosition += particle.Velocity * deltaTime;

                var size = Mathf.Lerp(particle.StartSize, particle.EndSize, t);
                particle.Rect.sizeDelta = new Vector2(size, size);
                particle.Image.color = Color.Lerp(particle.StartColor, particle.EndColor, t);
            }
        }

        private void UpdateRings(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (var index = 0; index < _rings.Count; index++)
            {
                var ring = _rings[index];
                if (!ring.Active)
                    continue;

                ring.Age += deltaTime;
                if (ring.Age >= ring.Lifetime)
                {
                    ring.SetActive(false);
                    continue;
                }

                var t = Mathf.Clamp01(ring.Age / ring.Lifetime);
                var size = Mathf.Lerp(ring.StartSize, ring.EndSize, t);
                ring.Rect.sizeDelta = new Vector2(size, size);
                ring.Image.color = Color.Lerp(ring.StartColor, ring.EndColor, t);
            }
        }

        private void UpdateLabels(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (var index = 0; index < _labels.Count; index++)
            {
                var label = _labels[index];
                if (!label.Active)
                    continue;

                label.Age += deltaTime;
                if (label.Age >= label.Lifetime)
                {
                    label.SetActive(false);
                    continue;
                }

                var t = Mathf.Clamp01(label.Age / label.Lifetime);
                label.Rect.anchoredPosition += label.Velocity * deltaTime;
                label.Velocity *= Mathf.Max(0f, 1f - 1.2f * deltaTime);
                label.Text.color = Color.Lerp(label.StartColor, label.EndColor, t);

                var pop = 1f + 0.24f * Mathf.Sin(Mathf.Clamp01(t * 2.2f) * Mathf.PI);
                label.Rect.localScale = Vector3.one * pop;
            }
        }

        private void UpdateDust(float deltaTime)
        {
            if (deltaTime <= 0f || _dustLayer == null)
                return;

            var area = _dustLayer.rect;
            var width = area.width > 1f ? area.width : 2960f;
            var height = area.height > 1f ? area.height : 1440f;
            var minX = -width * 0.5f;
            var maxX = width * 0.5f;
            var minY = -height * 0.5f;
            var maxY = height * 0.5f;

            for (var index = 0; index < _dust.Count; index++)
            {
                var dust = _dust[index];
                var position = dust.Rect.anchoredPosition;

                position.y += dust.Speed * deltaTime;
                position.x += Mathf.Sin(_time * dust.Sway + dust.Phase) * 6f * deltaTime;

                if (position.y > maxY + dust.Size)
                {
                    position.y = minY - dust.Size;
                    position.x = UnityEngine.Random.Range(minX, maxX);
                }

                if (position.x < minX - dust.Size)
                    position.x = maxX + dust.Size;
                else if (position.x > maxX + dust.Size)
                    position.x = minX - dust.Size;

                dust.Rect.anchoredPosition = position;

                var twinkle = 0.55f + 0.45f * Mathf.Sin(_time * 1.7f + dust.Phase);
                dust.Image.color = WithAlpha(dust.Tint, dust.Alpha * twinkle);
            }
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flash == null)
                return;

            _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, deltaTime * 0.9f);
            _flash.color = WithAlpha(_flashColor, _flashAlpha);
        }

        private void EmitTrails(float deltaTime)
        {
            _trailTimer += deltaTime;
            if (_trailTimer < TrailInterval)
                return;

            _trailTimer = 0f;
            _ballRoster.CopyActiveBalls(_activeBalls);

            for (var index = 0; index < _activeBalls.Count; index++)
            {
                var ball = _activeBalls[index];
                if (!ball.IsInFlight || ball.IsDying)
                    continue;

                var position = ToLayerPosition(ball.transform.position);
                var size = Mathf.Clamp(ball.Radius * 1.6f, 8f, 34f) * Mathf.Max(0.5f, ball.VisualScale);
                var tint = index == 0 ? UiTheme.Accent : UiTheme.Info;
                var drift = ball.Velocity.sqrMagnitude > 0.01f
                    ? -ball.Velocity.normalized * 26f
                    : Vector2.zero;

                SpawnParticle(
                    position,
                    drift,
                    0.26f,
                    size,
                    size * 0.1f,
                    WithAlpha(tint, 0.16f),
                    WithAlpha(tint, 0f),
                    0f,
                    0f,
                    _glowSprite);
            }
        }

        // ---------------------------------------------------------------- spawners

        private void SpawnParticle(
            Vector2 position,
            Vector2 velocity,
            float lifetime,
            float startSize,
            float endSize,
            Color startColor,
            Color endColor,
            float gravity,
            float drag,
            Sprite sprite)
        {
            var particle = TakeParticle();
            if (particle == null)
                return;

            particle.SetActive(true);
            particle.Age = 0f;
            particle.Lifetime = Mathf.Max(0.01f, lifetime);
            particle.StartSize = startSize;
            particle.EndSize = endSize;
            particle.StartColor = startColor;
            particle.EndColor = endColor;
            particle.Velocity = velocity;
            particle.Gravity = gravity;
            particle.Drag = drag;

            particle.Image.sprite = sprite;
            particle.Image.color = startColor;
            particle.Rect.anchoredPosition = position;
            particle.Rect.sizeDelta = new Vector2(startSize, startSize);
            particle.Rect.localScale = Vector3.one;
        }

        private void SpawnRing(Vector2 position, float startSize, float endSize, float lifetime, Color tint)
        {
            Ring ring = null;
            for (var index = 0; index < _rings.Count; index++)
            {
                if (!_rings[index].Active)
                {
                    ring = _rings[index];
                    break;
                }
            }

            if (ring == null)
                ring = _rings[0];

            ring.SetActive(true);
            ring.Age = 0f;
            ring.Lifetime = Mathf.Max(0.01f, lifetime);
            ring.StartSize = startSize;
            ring.EndSize = endSize;
            ring.StartColor = WithAlpha(tint, 0.85f);
            ring.EndColor = WithAlpha(tint, 0f);

            ring.Image.sprite = _ringSprite;
            ring.Image.color = ring.StartColor;
            ring.Rect.anchoredPosition = position;
            ring.Rect.sizeDelta = new Vector2(startSize, startSize);
            ring.Rect.localScale = Vector3.one;
        }

        private void SpawnLabel(Vector2 position, string text, Color tint, float fontSize)
        {
            FloatingLabel label = null;
            for (var index = 0; index < _labels.Count; index++)
            {
                if (!_labels[index].Active)
                {
                    label = _labels[index];
                    break;
                }
            }

            if (label == null)
                label = _labels[0];

            label.SetActive(true);
            label.Age = 0f;
            label.Lifetime = 0.85f;
            label.StartColor = WithAlpha(tint, 1f);
            label.EndColor = WithAlpha(tint, 0f);
            label.Velocity = new Vector2(0f, 92f);
            label.Rect.anchoredPosition = position;
            label.Rect.localScale = Vector3.one;
            label.Text.text = text;
            label.Text.fontSize = fontSize;
            label.Text.color = label.StartColor;
        }

        private void AddFlash(Color tint, float alpha)
        {
            _flashColor = tint;
            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
        }

        // ------------------------------------------------------------------ pools

        private Particle TakeParticle()
        {
            Particle oldest = null;
            var oldestProgress = -1f;

            for (var index = 0; index < _particles.Count; index++)
            {
                var particle = _particles[index];
                if (!particle.Active)
                    return particle;

                var progress = particle.Age / particle.Lifetime;
                if (progress > oldestProgress)
                {
                    oldestProgress = progress;
                    oldest = particle;
                }
            }

            return oldest;
        }

        private Vector2 ToLayerPosition(Vector3 worldPosition)
        {
            var local = _fxLayer.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.y);
        }

        private static Image CreateImage(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.layer = parent.gameObject.layer;

            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(16f, 16f);

            var image = gameObject.AddComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        // ------------------------------------------------------------------ types

        private sealed class Particle
        {
            public readonly RectTransform Rect;
            public readonly Image Image;
            public bool Active;
            public float Age;
            public float Lifetime = 1f;
            public float StartSize;
            public float EndSize;
            public Color StartColor;
            public Color EndColor;
            public Vector2 Velocity;
            public float Gravity;
            public float Drag;

            public Particle(Image image)
            {
                Image = image;
                Rect = image.rectTransform;
            }

            public void SetActive(bool active)
            {
                Active = active;
                Rect.gameObject.SetActive(active);
            }
        }

        private sealed class Ring
        {
            public readonly RectTransform Rect;
            public readonly Image Image;
            public bool Active;
            public float Age;
            public float Lifetime = 1f;
            public float StartSize;
            public float EndSize;
            public Color StartColor;
            public Color EndColor;

            public Ring(Image image)
            {
                Image = image;
                Rect = image.rectTransform;
            }

            public void SetActive(bool active)
            {
                Active = active;
                Rect.gameObject.SetActive(active);
            }
        }

        private sealed class FloatingLabel
        {
            public readonly RectTransform Rect;
            public readonly TextMeshProUGUI Text;
            public bool Active;
            public float Age;
            public float Lifetime = 1f;
            public Color StartColor;
            public Color EndColor;
            public Vector2 Velocity;

            public FloatingLabel(TextMeshProUGUI text)
            {
                Text = text;
                Rect = text.rectTransform;
            }

            public void SetActive(bool active)
            {
                Active = active;
                Rect.gameObject.SetActive(active);
            }
        }

        private sealed class Dust
        {
            public readonly RectTransform Rect;
            public readonly Image Image;
            public readonly Color Tint;
            public readonly float Speed;
            public readonly float Sway;
            public readonly float Phase;
            public readonly float Alpha;
            public readonly float Size;

            public Dust(
                RectTransform rect,
                Image image,
                Color tint,
                float speed,
                float sway,
                float phase,
                float alpha,
                float size)
            {
                Rect = rect;
                Image = image;
                Tint = tint;
                Speed = speed;
                Sway = sway;
                Phase = phase;
                Alpha = alpha;
                Size = size;
            }
        }
    }
}
