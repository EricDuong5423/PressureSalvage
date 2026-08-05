using UnityEngine;

namespace LGU
{
    public interface IGlassRegionProvider
    {
        Vector4 ScreenRectPx { get; }
        float RadiusPx { get; } // used only for rounded-rect fallback
        float EffectIntensity { get; }
        bool IsBlur { get; }
    }

    public interface IGlassShapeProvider
    {
        bool UseShape { get; }
        Texture ShapeSDF { get; }

        // Mapping: uv = invM * (screenPx - origin)
        Vector2 ShapeOriginScreenPx { get; }
        Vector4 ShapeInvM { get; } // (inv00,inv01,inv10,inv11)

        // SDF encoding range in SHAPE TEXELS (not screen px)
        float ShapeMaxDistTexel { get; }

        // How many SCREEN pixels correspond to one SDF TEXEL along U and V.
        Vector2 ShapeScreenPxPerTexelUV { get; } // (px/texel along U, px/texel along V)
    }
}
