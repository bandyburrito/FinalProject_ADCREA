using UnityEngine;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Shared 1x1 white square sprite built from Unity's built-in white texture.
    /// Doors, pickups and altars are generated at runtime while the real room art is
    /// still being drawn, so they must not depend on any imported image asset.
    /// Tinting and scaling the one shared square covers every marker we need.
    /// </summary>
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

                // Pixels-per-unit equal to the texture width makes the sprite exactly one
                // world unit wide, so a transform scale of 2 means a 2-unit square.
                _solidSquare = Sprite.Create(source, fullTexture, centerPivot, source.width);
            }
            return _solidSquare;
        }
    }
}
