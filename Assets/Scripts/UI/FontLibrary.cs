using UnityEngine;

namespace ADCREA.UI
{

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
