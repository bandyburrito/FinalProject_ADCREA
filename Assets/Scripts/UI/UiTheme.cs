using System.Collections.Generic;
using UnityEngine;

namespace ADCREA.UI
{
    /// <summary>
    /// Shared palette and drawing helpers for every OnGUI screen, styled after Slay the
    /// Spire: solid dark stone backdrops, bordered panel cards with a name bar, a
    /// parchment title banner and teal pill buttons. Centralized so the menu, the death
    /// screen and the choice screens cannot drift apart visually.
    ///
    /// Solid-colour textures are generated once and cached - OnGUI runs several times
    /// per frame and must never allocate a texture per call.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color BackdropTop = new Color(0.17f, 0.16f, 0.20f);
        public static readonly Color BackdropBottom = new Color(0.09f, 0.08f, 0.11f);
        public static readonly Color Panel = new Color(0.23f, 0.21f, 0.26f);
        public static readonly Color PanelHover = new Color(0.30f, 0.28f, 0.34f);
        public static readonly Color PanelBorder = new Color(0.47f, 0.43f, 0.52f);
        public static readonly Color NameBar = new Color(0.34f, 0.31f, 0.39f);
        public static readonly Color InnerFrame = new Color(0.13f, 0.12f, 0.16f);
        public static readonly Color Parchment = new Color(0.83f, 0.78f, 0.66f);
        public static readonly Color ParchmentText = new Color(0.24f, 0.19f, 0.14f);
        public static readonly Color Gold = new Color(0.95f, 0.80f, 0.35f);
        public static readonly Color Teal = new Color(0.30f, 0.58f, 0.58f);
        public static readonly Color TealHover = new Color(0.38f, 0.70f, 0.70f);
        public static readonly Color TextBody = new Color(0.86f, 0.85f, 0.88f);

        private static readonly Dictionary<Color, Texture2D> SolidCache = new Dictionary<Color, Texture2D>();
        private static Texture2D _backdropGradient;

        public static Texture2D Solid(Color color)
        {
            Texture2D cached;
            if (SolidCache.TryGetValue(color, out cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            // Survives scene reloads: these are session-lifetime UI resources, not
            // scene objects.
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = { color, color, color, color };
            texture.SetPixels(pixels);
            texture.Apply();
            SolidCache[color] = texture;
            return texture;
        }

        /// <summary>Full-screen solid backdrop with a subtle vertical gradient.</summary>
        public static void DrawBackdrop()
        {
            if (_backdropGradient == null)
            {
                const int steps = 64;
                _backdropGradient = new Texture2D(1, steps, TextureFormat.RGBA32, false);
                _backdropGradient.hideFlags = HideFlags.HideAndDontSave;
                _backdropGradient.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < steps; y++)
                {
                    // Texture row 0 is the bottom; darker at the bottom reads as depth.
                    _backdropGradient.SetPixel(0, y, Color.Lerp(BackdropBottom, BackdropTop, (float)y / (steps - 1)));
                }
                _backdropGradient.Apply();
            }
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _backdropGradient);
        }

        public static void DrawPanel(Rect rect, Color fill, Color border, float borderWidth)
        {
            GUI.DrawTexture(rect, Solid(border));
            var inner = new Rect(rect.x + borderWidth, rect.y + borderWidth,
                rect.width - borderWidth * 2f, rect.height - borderWidth * 2f);
            GUI.DrawTexture(inner, Solid(fill));
        }

        /// <summary>The parchment title strip, centred horizontally.</summary>
        public static void DrawBanner(float centerY, string text, GUIStyle textStyle)
        {
            float width = Mathf.Min(680f, Screen.width * 0.8f);
            var banner = new Rect((Screen.width - width) * 0.5f, centerY - 32f, width, 64f);
            DrawPanel(banner, Parchment, ParchmentText, 3f);
            GUI.Label(banner, text, textStyle);
        }
    }
}
