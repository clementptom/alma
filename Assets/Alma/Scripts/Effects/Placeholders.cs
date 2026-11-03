using UnityEngine;

namespace Alma
{
    /// <summary>Generated white sprites so greybox objects are visible before real art exists.</summary>
    public static class Placeholders
    {
        static Sprite square;
        static Sprite circle;

        /// <summary>A 1x1 world-unit white square.</summary>
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "PlaceholderSquare" };
                    var pixels = new Color[16];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    square.name = "PlaceholderSquare";
                }
                return square;
            }
        }

        /// <summary>A 1-world-unit white circle.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    const int size = 32;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "PlaceholderCircle" };
                    var pixels = new Color[size * size];
                    float r = size * 0.5f;
                    for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - r, dy = y + 0.5f - r;
                        pixels[y * size + x] = dx * dx + dy * dy <= r * r ? Color.white : Color.clear;
                    }
                    tex.SetPixels(pixels);
                    tex.Apply();
                    circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                    circle.name = "PlaceholderCircle";
                }
                return circle;
            }
        }
    }
}
