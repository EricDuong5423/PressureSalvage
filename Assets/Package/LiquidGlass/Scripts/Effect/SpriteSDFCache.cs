using System.Collections.Generic;
using UnityEngine;

namespace LGU
{
    public static class SpriteSDFCache
    {
        public struct SDFResult
        {
            public Texture2D tex;          // RHalf if possible
            public float maxDistTexel;     // decode range in SHAPE TEXELS
            public int width, height;
        }

        static readonly Dictionary<int, SDFResult> _cache = new();

        const int kMaxSDFSize = 256;
        const float kMaxDistFrac = 0.35f;     // maxDist = frac * min(w,h)
        const byte kAlphaThreshold = 16;

        public static bool TryGet(Sprite sprite, out SDFResult res)
        {
            res = default;
            if (!sprite) return false;

            int key = sprite.GetInstanceID();
            if (_cache.TryGetValue(key, out res) && res.tex) return true;

            res = Build(sprite);
            if (res.tex) _cache[key] = res;
            return res.tex != null;
        }

        static SDFResult Build(Sprite sprite)
        {
            var tex = sprite.texture;
            if (!tex || !tex.isReadable)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGU] Sprite '{sprite.name}' texture is not readable. Enable Read/Write in import settings.");
#endif
                return default;
            }

            Rect r = sprite.textureRect;
            int srcW = Mathf.Max(1, (int)r.width);
            int srcH = Mathf.Max(1, (int)r.height);

            float scale = 1f;
            int w = srcW, h = srcH;
            int maxSide = Mathf.Max(srcW, srcH);
            if (maxSide > kMaxSDFSize)
            {
                scale = (float)kMaxSDFSize / maxSide;
                w = Mathf.Max(8, Mathf.RoundToInt(srcW * scale));
                h = Mathf.Max(8, Mathf.RoundToInt(srcH * scale));
            }

            // Rect fetch (GetPixels32 has no rect overload)
            Color[] srcF = tex.GetPixels((int)r.x, (int)r.y, srcW, srcH);
            var src = new Color32[srcF.Length];
            for (int i = 0; i < src.Length; i++) src[i] = srcF[i];

            bool[] inside = new bool[w * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Clamp(Mathf.RoundToInt(y / scale), 0, srcH - 1);
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt(x / scale), 0, srcW - 1);
                    inside[y * w + x] = src[sy * srcW + sx].a >= kAlphaThreshold;
                }
            }

            float[] distToInside = DistanceTransform(inside, w, h, targetIsTrue: true);
            float[] distToOutside = DistanceTransform(inside, w, h, targetIsTrue: false);

            float[] sdf = new float[w * h];
            for (int i = 0; i < sdf.Length; i++)
                sdf[i] = inside[i] ? -distToOutside[i] : distToInside[i];

            float maxDistTexel = Mathf.Max(1f, kMaxDistFrac * Mathf.Min(w, h));

            TextureFormat fmt = SystemInfo.SupportsTextureFormat(TextureFormat.RHalf)
                ? TextureFormat.RHalf
                : TextureFormat.RGBA32;

            var outTex = new Texture2D(w, h, fmt, mipChain: false, linear: true);
            outTex.wrapMode = TextureWrapMode.Clamp;
            outTex.filterMode = FilterMode.Bilinear;
            outTex.name = $"LGU_SDF_{sprite.name}";

            if (fmt == TextureFormat.RHalf)
            {
                var cols = new Color[w * h];
                for (int i = 0; i < sdf.Length; i++)
                {
                    float d = Mathf.Clamp(sdf[i], -maxDistTexel, maxDistTexel);
                    float enc = Mathf.Clamp01(0.5f + 0.5f * (d / maxDistTexel));
                    cols[i] = new Color(enc, 0, 0, 1);
                }
                outTex.SetPixels(cols);
            }
            else
            {
                var cols = new Color32[w * h];
                for (int i = 0; i < sdf.Length; i++)
                {
                    float d = Mathf.Clamp(sdf[i], -maxDistTexel, maxDistTexel);
                    float enc = Mathf.Clamp01(0.5f + 0.5f * (d / maxDistTexel));
                    byte b = (byte)Mathf.RoundToInt(enc * 255f);
                    cols[i] = new Color32(b, 0, 0, 255);
                }
                outTex.SetPixels32(cols);
            }

            outTex.Apply(false, false);

            return new SDFResult
            {
                tex = outTex,
                maxDistTexel = maxDistTexel,
                width = w,
                height = h
            };
        }

        static float[] DistanceTransform(bool[] mask, int w, int h, bool targetIsTrue)
        {
            const float INF = 1e9f;
            float[] d = new float[w * h];

            for (int i = 0; i < d.Length; i++)
                d[i] = (mask[i] == targetIsTrue) ? 0f : INF;

            float w1 = 1f;
            float w2 = 1.41421356f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + w1);
                    if (y > 0) v = Mathf.Min(v, d[i - w] + w1);
                    if (x > 0 && y > 0) v = Mathf.Min(v, d[i - w - 1] + w2);
                    if (x < w - 1 && y > 0) v = Mathf.Min(v, d[i - w + 1] + w2);
                    d[i] = v;
                }

            for (int y = h - 1; y >= 0; y--)
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + w1);
                    if (y < h - 1) v = Mathf.Min(v, d[i + w] + w1);
                    if (x < w - 1 && y < h - 1) v = Mathf.Min(v, d[i + w + 1] + w2);
                    if (x > 0 && y < h - 1) v = Mathf.Min(v, d[i + w - 1] + w2);
                    d[i] = v;
                }

            return d;
        }
    }
}
