using UnityEngine;

public static class LightSpriteGenerator
{
    private static Sprite _softCircle;
    private static Sprite _whitePixel;

    public static Sprite SoftCircle
    {
        get
        {
            if (_softCircle == null) _softCircle = CreateSoftCircle(128);
            return _softCircle;
        }
    }

    public static Sprite WhitePixel
    {
        get
        {
            if (_whitePixel == null)
            {
                var tex = new Texture2D(2, 2);
                tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                _whitePixel = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            }
            return _whitePixel;
        }
    }

    static Sprite CreateSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}