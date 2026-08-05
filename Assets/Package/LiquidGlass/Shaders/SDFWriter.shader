Shader "LiquidGlass/SDFWriter"
{
    // ==========================================================
    // SubShader 1: URP
    // ==========================================================
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+5100" }
        ZWrite Off
        ZTest Always
        Cull Off

        // =========================
        // PASS 0: crisp SDF -> R (Min union)
        // =========================
        Pass
        {
            Name "WriteSDF_R"
            BlendOp Min
            Blend One One
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragSDF

            #if defined(UNITY_RENDER_PIPELINE_UNIVERSAL)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #else
                #include "UnityCG.cginc"
                #define TEXTURE2D(name) sampler2D name
                #define SAMPLER(name) sampler2D name
                #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)
            #endif

            float4 _ScreenSize;        // target RT size in pixels (W,H,1/W,1/H)
            float4 _ScreenRectPx;      // rect in true screen px
            float4 _ScreenToRTScale;   // (RT_W/ScreenW, RT_H/ScreenH, 0, 0)
            float  _RadiusPx;

            TEXTURE2D(_ShapeTex); SAMPLER(sampler_ShapeTex);
            float  _UseShape;                 // 0/1
            float4 _ShapeOriginPx;            // (x,y,0,0) in true screen px
            float4 _ShapeInvM;                // (inv00,inv01,inv10,inv11)
            float  _ShapeMaxDistTexel;        // max dist in SHAPE TEXELS used during encoding
            float2 _ShapeScreenPxPerTexelUV;  // (screen px per 1 texel in U, in V)
            float4 _ShapeTex_TexelSize;       // (1/w,1/h,w,h) auto by Unity

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(uint id:SV_VertexID)
            {
                float2 p = float2((id==2)?3.0:-1.0, (id==1)?3.0:-1.0);
                v2f o; o.pos=float4(p,0,1); o.uv=0.5*(p+1.0); return o;
            }

            float sdRoundRectPx(float2 pPx, float2 minPx, float2 maxPx, float rPx)
            {
                float2 c = 0.5*(minPx+maxPx);
                float2 e = 0.5*(maxPx-minPx);
                float2 d = abs(pPx - c) - (e - rPx);
                float outside = length(max(d,0));
                float inside  = min(max(d.x,d.y),0);
                return outside + inside - rPx;
            }

            // Decode shape SDF into distance measured in CURRENT TARGET pixels.
            // screenPx: true screen pixels
            // screenToTargetScale: (targetW/ScreenW, targetH/ScreenH)
            float DecodeShape_TargetPx(float2 screenPx, float2 screenToTargetScale)
            {
                float2 d = screenPx - _ShapeOriginPx.xy;

                // screen delta -> uv via inverse 2x2
                float2 uv;
                uv.x = _ShapeInvM.x * d.x + _ShapeInvM.y * d.y;
                uv.y = _ShapeInvM.z * d.x + _ShapeInvM.w * d.y;

                if (any(uv < 0.0) || any(uv > 1.0))
                    return 1e6;

                // half-texel inset to avoid bilinear bleed at edges
                float2 halfTexel = _ShapeTex_TexelSize.xy * 0.5;
                uv = clamp(uv, halfTexel, 1.0 - halfTexel);

                // enc: 0..1, 0.5=edge
                float enc = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv).r;

                // distance in SHAPE TEXELS
                float dTexel = (enc * 2.0 - 1.0) * max(_ShapeMaxDistTexel, 1e-3);

                // texels -> screen px (isotropic metric)
                float pxU = max(_ShapeScreenPxPerTexelUV.x, 1e-6);
                float pxV = max(_ShapeScreenPxPerTexelUV.y, 1e-6);
                float screenPxPerTexel = sqrt(pxU * pxV); // geometric mean

                float dScreenPx = dTexel * screenPxPerTexel;

                // screen px -> target px (isotropic)
                float sx = max(screenToTargetScale.x, 1e-6);
                float sy = max(screenToTargetScale.y, 1e-6);
                float targetScale = sqrt(sx * sy);

                return dScreenPx * targetScale;
            }

            float4 fragSDF(v2f i) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    i.uv.y = 1.0 - i.uv.y;
                #endif

                float2 pTarget = i.uv * _ScreenSize.xy;

                // target px -> screen px
                float2 screenPx = pTarget / max(_ScreenToRTScale.xy, 1e-6);

                float dPx;
                if (_UseShape > 0.5)
                {
                    dPx = DecodeShape_TargetPx(screenPx, _ScreenToRTScale.xy);
                }
                else
                {
                    float2 rMin = _ScreenRectPx.xy * _ScreenToRTScale.xy;
                    float2 rMax = _ScreenRectPx.zw * _ScreenToRTScale.xy;
                    float  rPx  = _RadiusPx * _ScreenToRTScale.y;
                    dPx = sdRoundRectPx(pTarget, rMin, rMax, max(rPx,0));
                }

                dPx = clamp(dPx, -2048.0, 2048.0);
                return float4(dPx, 0, 0, 0);
            }
            ENDHLSL
        }

        // =========================
        // PASS 1: influence -> GBA (Add)
        // G = sumExp, B = sumExp*isBlur, A = sumExp*intensity
        // =========================
        Pass
        {
            Name "WriteSMin_G"
            BlendOp Add
            Blend One One
            ColorMask GBA

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragSMin

            #if defined(UNITY_RENDER_PIPELINE_UNIVERSAL)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #else
                #include "UnityCG.cginc"
                #define TEXTURE2D(name) sampler2D name
                #define SAMPLER(name) sampler2D name
                #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)
            #endif

            float4 _FieldSize;           // FIELD size in pixels (W,H,1/W,1/H)
            float4 _ScreenRectPx;        // rect in true screen px
            float4 _ScreenToFieldScale;  // (FIELD_W/ScreenW, FIELD_H/ScreenH, 0, 0)
            float  _RadiusPx;

            float  _SMinK_Field;         // k in 1/(FIELD pixel)
            float  _IsBlur;
            float  _EffectIntensity;

            TEXTURE2D(_ShapeTex); SAMPLER(sampler_ShapeTex);
            float  _UseShape;
            float4 _ShapeOriginPx;
            float4 _ShapeInvM;
            float  _ShapeMaxDistTexel;
            float2 _ShapeScreenPxPerTexelUV;
            float4 _ShapeTex_TexelSize;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(uint id:SV_VertexID)
            {
                float2 p = float2((id==2)?3.0:-1.0, (id==1)?3.0:-1.0);
                v2f o; o.pos=float4(p,0,1); o.uv=0.5*(p+1.0); return o;
            }

            float sdRoundRectPx(float2 pPx, float2 minPx, float2 maxPx, float rPx)
            {
                float2 c = 0.5*(minPx+maxPx);
                float2 e = 0.5*(maxPx-minPx);
                float2 d = abs(pPx - c) - (e - rPx);
                float outside = length(max(d,0));
                float inside  = min(max(d.x,d.y),0);
                return outside + inside - rPx;
            }

            float DecodeShape_TargetPx(float2 screenPx, float2 screenToTargetScale)
            {
                float2 d = screenPx - _ShapeOriginPx.xy;

                float2 uvRaw;
                uvRaw.x = _ShapeInvM.x * d.x + _ShapeInvM.y * d.y;
                uvRaw.y = _ShapeInvM.z * d.x + _ShapeInvM.w * d.y;

                // Clamp for sampling (avoid bilinear bleed)
                float2 halfTexel = _ShapeTex_TexelSize.xy * 0.5;
                float2 uv = clamp(uvRaw, halfTexel, 1.0 - halfTexel);

                // Sample encoded SDF (0..1 where 0.5=edge)
                float enc = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv).r;

                // Distance in SHAPE TEXELS (clamped to max range)
                float maxD = max(_ShapeMaxDistTexel, 1e-3);
                float dTexel = (enc * 2.0 - 1.0) * maxD;

                // Compute how far outside the 0..1 rect we are, in UV units
                float2 outsideUV = max(max(-uvRaw, 0.0), max(uvRaw - 1.0, 0.0)); // 0 inside, >0 outside

                // Convert outside UV distance to TEXELS (uv / (1/w,h))
                float2 outsideTexel2 = outsideUV / max(_ShapeTex_TexelSize.xy, 1e-6);
                float outsideTexel = length(outsideTexel2);

                // If we're outside, force distance to be positive and continuous:
                // distance = outside-to-rect + max(sampledDistanceAtEdge, 0)
                if (outsideTexel > 0.0)
                    dTexel = outsideTexel + max(dTexel, 0.0);

                // Texels -> screen px (isotropic)
                float pxU = max(_ShapeScreenPxPerTexelUV.x, 1e-6);
                float pxV = max(_ShapeScreenPxPerTexelUV.y, 1e-6);
                float screenPxPerTexel = sqrt(pxU * pxV);
                float dScreenPx = dTexel * screenPxPerTexel;

                // Screen px -> target px (isotropic)
                float sx = max(screenToTargetScale.x, 1e-6);
                float sy = max(screenToTargetScale.y, 1e-6);
                float targetScale = sqrt(sx * sy);

                return dScreenPx * targetScale;
            }

            float4 fragSMin(v2f i) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    i.uv.y = 1.0 - i.uv.y;
                #endif

                float2 pF = i.uv * _FieldSize.xy;

                // FIELD px -> screen px
                float2 screenPx = pF / max(_ScreenToFieldScale.xy, 1e-6);

                float dPx;
                if (_UseShape > 0.5)
                {
                    // returns FIELD-pixel distance
                    dPx = DecodeShape_TargetPx(screenPx, _ScreenToFieldScale.xy);
                }
                else
                {
                    float2 rMin = _ScreenRectPx.xy * _ScreenToFieldScale.xy;
                    float2 rMax = _ScreenRectPx.zw * _ScreenToFieldScale.xy;
                    float  rPx  = _RadiusPx * _ScreenToFieldScale.y;
                    dPx = sdRoundRectPx(pF, rMin, rMax, max(rPx,0));
                }

                dPx = clamp(dPx, -2048.0, 2048.0);

                float k = max(_SMinK_Field, 1e-5);
                float g = exp(-k * dPx);

                float b = g * _IsBlur;
                float a = g * _EffectIntensity;

                return float4(0, g, b, a);
            }
            ENDHLSL
        }
    }

    // ==========================================================
    // SubShader 2: Built-in RP fallback
    // (Same passes, but NO RenderPipeline tag)
    // ==========================================================
    SubShader
    {
        Tags { "Queue"="Geometry+5100" }
        ZWrite Off
        ZTest Always
        Cull Off

        // =========================
        // PASS 0: crisp SDF -> R (Min union)
        // =========================
        Pass
        {
            Name "WriteSDF_R"
            BlendOp Min
            Blend One One
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragSDF

            #include "UnityCG.cginc"
            #define TEXTURE2D(name) sampler2D name
            #define SAMPLER(name) sampler2D name
            #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)

            float4 _ScreenSize;
            float4 _ScreenRectPx;
            float4 _ScreenToRTScale;
            float  _RadiusPx;

            TEXTURE2D(_ShapeTex); SAMPLER(sampler_ShapeTex);
            float  _UseShape;
            float4 _ShapeOriginPx;
            float4 _ShapeInvM;
            float  _ShapeMaxDistTexel;
            float2 _ShapeScreenPxPerTexelUV;
            float4 _ShapeTex_TexelSize;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(uint id:SV_VertexID)
            {
                float2 p = float2((id==2)?3.0:-1.0, (id==1)?3.0:-1.0);
                v2f o; o.pos=float4(p,0,1); o.uv=0.5*(p+1.0); return o;
            }

            float sdRoundRectPx(float2 pPx, float2 minPx, float2 maxPx, float rPx)
            {
                float2 c = 0.5*(minPx+maxPx);
                float2 e = 0.5*(maxPx-minPx);
                float2 d = abs(pPx - c) - (e - rPx);
                float outside = length(max(d,0));
                float inside  = min(max(d.x,d.y),0);
                return outside + inside - rPx;
            }

            float DecodeShape_TargetPx(float2 screenPx, float2 screenToTargetScale)
            {
                float2 d = screenPx - _ShapeOriginPx.xy;

                float2 uv;
                uv.x = _ShapeInvM.x * d.x + _ShapeInvM.y * d.y;
                uv.y = _ShapeInvM.z * d.x + _ShapeInvM.w * d.y;

                if (any(uv < 0.0) || any(uv > 1.0))
                    return 1e6;

                float2 halfTexel = _ShapeTex_TexelSize.xy * 0.5;
                uv = clamp(uv, halfTexel, 1.0 - halfTexel);

                float enc = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv).r;
                float dTexel = (enc * 2.0 - 1.0) * max(_ShapeMaxDistTexel, 1e-3);

                float pxU = max(_ShapeScreenPxPerTexelUV.x, 1e-6);
                float pxV = max(_ShapeScreenPxPerTexelUV.y, 1e-6);
                float screenPxPerTexel = sqrt(pxU * pxV);

                float dScreenPx = dTexel * screenPxPerTexel;

                float sx = max(screenToTargetScale.x, 1e-6);
                float sy = max(screenToTargetScale.y, 1e-6);
                float targetScale = sqrt(sx * sy);

                return dScreenPx * targetScale;
            }

            float4 fragSDF(v2f i) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    i.uv.y = 1.0 - i.uv.y;
                #endif

                float2 pTarget = i.uv * _ScreenSize.xy;
                float2 screenPx = pTarget / max(_ScreenToRTScale.xy, 1e-6);

                float dPx;
                if (_UseShape > 0.5) dPx = DecodeShape_TargetPx(screenPx, _ScreenToRTScale.xy);
                else
                {
                    float2 rMin = _ScreenRectPx.xy * _ScreenToRTScale.xy;
                    float2 rMax = _ScreenRectPx.zw * _ScreenToRTScale.xy;
                    float  rPx  = _RadiusPx * _ScreenToRTScale.y;
                    dPx = sdRoundRectPx(pTarget, rMin, rMax, max(rPx,0));
                }

                dPx = clamp(dPx, -2048.0, 2048.0);
                return float4(dPx, 0, 0, 0);
            }
            ENDHLSL
        }

        // =========================
        // PASS 1: influence -> GBA (Add)
        // =========================
        Pass
        {
            Name "WriteSMin_G"
            BlendOp Add
            Blend One One
            ColorMask GBA

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragSMin

            #include "UnityCG.cginc"
            #define TEXTURE2D(name) sampler2D name
            #define SAMPLER(name) sampler2D name
            #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)

            float4 _FieldSize;
            float4 _ScreenRectPx;
            float4 _ScreenToFieldScale;
            float  _RadiusPx;

            float  _SMinK_Field;
            float  _IsBlur;
            float  _EffectIntensity;

            TEXTURE2D(_ShapeTex); SAMPLER(sampler_ShapeTex);
            float  _UseShape;
            float4 _ShapeOriginPx;
            float4 _ShapeInvM;
            float  _ShapeMaxDistTexel;
            float2 _ShapeScreenPxPerTexelUV;
            float4 _ShapeTex_TexelSize;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(uint id:SV_VertexID)
            {
                float2 p = float2((id==2)?3.0:-1.0, (id==1)?3.0:-1.0);
                v2f o; o.pos=float4(p,0,1); o.uv=0.5*(p+1.0); return o;
            }

            float sdRoundRectPx(float2 pPx, float2 minPx, float2 maxPx, float rPx)
            {
                float2 c = 0.5*(minPx+maxPx);
                float2 e = 0.5*(maxPx-minPx);
                float2 d = abs(pPx - c) - (e - rPx);
                float outside = length(max(d,0));
                float inside  = min(max(d.x,d.y),0);
                return outside + inside - rPx;
            }

            float DecodeShape_TargetPx(float2 screenPx, float2 screenToTargetScale)
            {
                float2 d = screenPx - _ShapeOriginPx.xy;

                float2 uvRaw;
                uvRaw.x = _ShapeInvM.x * d.x + _ShapeInvM.y * d.y;
                uvRaw.y = _ShapeInvM.z * d.x + _ShapeInvM.w * d.y;

                float2 halfTexel = _ShapeTex_TexelSize.xy * 0.5;
                float2 uv = clamp(uvRaw, halfTexel, 1.0 - halfTexel);

                float enc = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv).r;

                float maxD = max(_ShapeMaxDistTexel, 1e-3);
                float dTexel = (enc * 2.0 - 1.0) * maxD;

                float2 outsideUV = max(max(-uvRaw, 0.0), max(uvRaw - 1.0, 0.0));
                float2 outsideTexel2 = outsideUV / max(_ShapeTex_TexelSize.xy, 1e-6);
                float outsideTexel = length(outsideTexel2);

                if (outsideTexel > 0.0)
                    dTexel = outsideTexel + max(dTexel, 0.0);

                float pxU = max(_ShapeScreenPxPerTexelUV.x, 1e-6);
                float pxV = max(_ShapeScreenPxPerTexelUV.y, 1e-6);
                float screenPxPerTexel = sqrt(pxU * pxV);
                float dScreenPx = dTexel * screenPxPerTexel;

                float sx = max(screenToTargetScale.x, 1e-6);
                float sy = max(screenToTargetScale.y, 1e-6);
                float targetScale = sqrt(sx * sy);

                return dScreenPx * targetScale;
            }

            float4 fragSMin(v2f i) : SV_Target
            {
                #if UNITY_UV_STARTS_AT_TOP
                    i.uv.y = 1.0 - i.uv.y;
                #endif

                float2 pF = i.uv * _FieldSize.xy;
                float2 screenPx = pF / max(_ScreenToFieldScale.xy, 1e-6);

                float dPx;
                if (_UseShape > 0.5) dPx = DecodeShape_TargetPx(screenPx, _ScreenToFieldScale.xy);
                else
                {
                    float2 rMin = _ScreenRectPx.xy * _ScreenToFieldScale.xy;
                    float2 rMax = _ScreenRectPx.zw * _ScreenToFieldScale.xy;
                    float  rPx  = _RadiusPx * _ScreenToFieldScale.y;
                    dPx = sdRoundRectPx(pF, rMin, rMax, max(rPx,0));
                }

                dPx = clamp(dPx, -2048.0, 2048.0);

                float k = max(_SMinK_Field, 1e-5);
                float g = exp(-k * dPx);

                float b = g * _IsBlur;
                float a = g * _EffectIntensity;

                return float4(0, g, b, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
