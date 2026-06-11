using UnityEngine;

namespace ADCREA.UI
{
    /// <summary>
    /// Single source for the game's font (JetBrains Mono) so every OnGUI label and every
    /// TextMesh renders with the same face. Loaded once from Resources; falls back to an
    /// OS-installed copy and finally to Unity's built-in font, so the project still runs
    /// on a machine where the bundled TTF was stripped.
    /// </summary>
    public static class FontLibrary
    {
        private static Font _uiFont;

        public static Font UiFont()
        {
            if (_uiFont != null)
            {
                return _uiFont;
            }

            _uiFont = Resources.Load<Font>("Fonts/JetBrainsMono-Regular");

            if (_uiFont == null)
            {
                _uiFont = TryOsFont();
            }

            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _uiFont;
        }

        private static Font TryOsFont()
        {
            // CreateDynamicFontFromOSFont silently produces a broken font for unknown
            // names, so the installed list is checked first.
            string[] installed = Font.GetOSInstalledFontNames();
            for (int i = 0; i < installed.Length; i++)
            {
                if (installed[i].Contains("JetBrains Mono"))
                {
                    return Font.CreateDynamicFontFromOSFont(installed[i], 16);
                }
            }
            return null;
        }
    }
}
