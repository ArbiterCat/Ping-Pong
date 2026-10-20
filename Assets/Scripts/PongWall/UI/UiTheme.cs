using UnityEngine;
using UnityEngine.UI;

namespace PongWall
{
    public enum UiButtonKind
    {
        Primary,
        Secondary,
        Ghost
    }

    /// <summary>
    /// Single source of truth for the look and feel of the interface.
    /// Sprites are generated at edit time (see UiSpriteGenerator) and live in
    /// Resources/UI, so both the editor tooling and runtime code can use them.
    /// </summary>
    public static class UiTheme
    {
        // ------------------------------------------------------------- palette
        public static readonly Color BackgroundTop = Hex(0x0C1226);
        public static readonly Color BackgroundBottom = Hex(0x05080F);
        public static readonly Color Halo = Hex(0x2E7D5B);

        public static readonly Color Surface = Hex(0x121A2E);
        public static readonly Color SurfaceStrong = Hex(0x1B2640);
        public static readonly Color SurfaceTranslucent = new(0.07f, 0.10f, 0.18f, 0.88f);
        public static readonly Color Scrim = new(0.02f, 0.03f, 0.06f, 0.80f);
        public static readonly Color Hairline = new(1f, 1f, 1f, 0.10f);

        public static readonly Color Accent = Hex(0xA7C957);
        public static readonly Color AccentDeep = Hex(0x6E8F2E);
        public static readonly Color Info = Hex(0x38BDF8);
        public static readonly Color Danger = Hex(0xF87171);
        public static readonly Color Gold = Hex(0xFBBF24);

        public static readonly Color TextPrimary = Hex(0xF8FAFC);
        public static readonly Color TextMuted = new(1f, 1f, 1f, 0.55f);
        public static readonly Color TextFaint = new(1f, 1f, 1f, 0.35f);

        // ------------------------------------------------------------ typography
        public const float HeroSize = 132f;
        public const float TitleSize = 96f;
        public const float BigNumberSize = 84f;
        public const float SubNumberSize = 52f;
        public const float BodySize = 40f;
        public const float CaptionSize = 30f;

        public const float CaptionSpacing = 14f;
        public const float TitleSpacing = 10f;

        // --------------------------------------------------------------- metrics
        public static readonly Vector2 HudSize = new(980f, 156f);
        public static readonly Vector2 ButtonSize = new(440f, 96f);
        public static readonly Vector2 CardSize = new(1180f, 880f);

        // --------------------------------------------------------------- sprites
        public static Sprite Panel => Load("panel");
        public static Sprite Card => Load("card");
        public static Sprite Pill => Load("pill");
        public static Sprite Chip => Load("chip");
        public static Sprite Disc => Load("disc");
        public static Sprite Ring => Load("ring");
        public static Sprite Glow => Load("glow");
        public static Sprite FadeDown => Load("fade_down");
        public static Sprite FadeUp => Load("fade_up");
        public static Sprite Solid => Load("solid");

        private static Sprite Load(string name)
        {
            return Resources.Load<Sprite>($"UI/{name}");
        }

        // -------------------------------------------------------------- helpers
        public static Color Hex(uint rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>Applies a generated sprite plus tint to any graphic.</summary>
        public static void Skin(Graphic graphic, Sprite sprite, Color color, bool raycastTarget = false)
        {
            var image = graphic as Image;
            if (image != null)
                image.sprite = sprite;

            graphic.color = color;
            graphic.raycastTarget = raycastTarget;
        }

        public static void StyleButton(Button button, UiButtonKind kind)
        {
            var image = button.targetGraphic as Image;
            var label = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            var textColor = ResolveTextColor(kind, false);
            var disabledText = new Color(textColor.r, textColor.g, textColor.b, 0.35f);

            if (image != null)
            {
                switch (kind)
                {
                    case UiButtonKind.Primary:
                        image.sprite = Pill;
                        break;
                    case UiButtonKind.Secondary:
                        image.sprite = Chip;
                        break;
                    default:
                        image.sprite = Chip;
                        break;
                }

                image.type = Image.Type.Sliced;
                image.raycastTarget = true;
            }

            var colors = button.colors;
            switch (kind)
            {
                case UiButtonKind.Primary:
                    colors.normalColor = Accent;
                    colors.highlightedColor = Color.Lerp(Accent, Color.white, 0.22f);
                    colors.pressedColor = AccentDeep;
                    colors.selectedColor = colors.highlightedColor;
                    colors.disabledColor = new Color(Accent.r, Accent.g, Accent.b, 0.28f);
                    break;

                case UiButtonKind.Secondary:
                    colors.normalColor = SurfaceStrong;
                    colors.highlightedColor = Color.Lerp(SurfaceStrong, Color.white, 0.14f);
                    colors.pressedColor = AccentDeep;
                    colors.selectedColor = colors.highlightedColor;
                    colors.disabledColor = new Color(SurfaceStrong.r, SurfaceStrong.g, SurfaceStrong.b, 0.35f);
                    break;

                default:
                    colors.normalColor = new Color(1f, 1f, 1f, 0f);
                    colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
                    colors.pressedColor = new Color(1f, 1f, 1f, 0.04f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.disabledColor = new Color(1f, 1f, 1f, 0f);
                    break;
            }

            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            if (label != null)
            {
                label.color = textColor;
                label.raycastTarget = false;
            }
        }

        private static Color ResolveTextColor(UiButtonKind kind, bool disabled)
        {
            if (disabled)
                return new Color(1f, 1f, 1f, 0.35f);

            switch (kind)
            {
                case UiButtonKind.Primary:
                    return Hex(0x0B1220);
                case UiButtonKind.Secondary:
                    return TextPrimary;
                default:
                    return TextMuted;
            }
        }

        public static Color ResolveButtonTextColor(UiButtonKind kind)
        {
            return ResolveTextColor(kind, false);
        }
    }
}