using UnityEngine;

// GoldTrail ve DustBurst'ün paylaştığı runtime doku üretimi — projede parçacık
// asset'i yok, yumuşak kenarlı daire dokusu koddan üretiliyor.
public static class ParticleTex
{
    public static Texture2D MakeSoftCircle(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return tex;
    }
}
