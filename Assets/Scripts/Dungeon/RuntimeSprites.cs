using UnityEngine;

namespace ADCREA.Dungeon
{

    public static class RuntimeSprites
    {
        private static Sprite _solidSquare;

        public static Sprite SolidSquare()
        {
            if (_solidSquare == null)
            {
                Texture2D source = Texture2D.whiteTexture;
                Rect fullTexture = new Rect(0f, 0f, source.width, source.height);
                Vector2 centerPivot = new Vector2(0.5f, 0.5f);

                _solidSquare = Sprite.Create(source, fullTexture, centerPivot, source.width);
            }
            return _solidSquare;
        }
    }
}
