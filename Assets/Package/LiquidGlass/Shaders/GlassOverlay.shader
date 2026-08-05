Shader "LiquidGlass/GlassOverlay"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite (alpha optional)", 2D) = "white" {}
        _Color ("Overall Tint Mult", Color) = (0,0,0,1)

        // Interpreted as 0..1 where 1 = 96px at 1080p (scaled to screen size)
        _RefractionPx    ("Refraction Strength (0..1 -> up to 96px@1080p)", Range(0, 1.5)) = 0.5
        _DispGain        ("Dispersion Gain",          Range(0, 2)) = 0.6

        _Thickness       ("Glass Thickness (px)",     Range(1, 128)) = 90
        _ReflectionFactor("Reflection Factor (IOR)",  Range(0.8, 1.5)) = 1.15

        _Tint            ("Highlight Tint (RGB) & Amount (A)", Color) = (1,1,1,0.35)

        _GlareRange          ("Glare Range",        Float) = 6
        _GlareHardness       ("Glare Hardness",     Range(0,1)) = 0.45
        _GlareConvergence    ("Glare Convergence",  Range(0,1)) = 0.3
        _GlareOppositeFactor ("Glare Opposite Fac", Range(0,2)) = 0.8
        _GlareAngle          ("Glare Angle (rad)",  Float) = -0.85
        _GlareIntensity      ("Glare Intensity",    Range(0,2)) = 1.9

        _FresnelRange        ("Fresnel Range",        Float) = 5
        _FresnelHardness     ("Fresnel Hardness",     Range(0,1)) = 0.2
        _FresnelIntensity    ("Fresnel Intensity",    Range(0,2)) = 0.2

        _MeltAlphaMix ("Melt Alpha Mix", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GlassOverlay"

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag

            // ---------- PIPELINE-SPECIFIC INCLUDES ----------
            #if defined(UNITY_RENDER_PIPELINE_UNIVERSAL)
                // URP path
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #else
                // Built-in path
                #include "UnityCG.cginc"

                // SRP-style helpers for Built-in so we can share the same code:
                #define TEXTURE2D(name) sampler2D name
                #define SAMPLER(name) sampler2D name
                #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)

                float4 TransformObjectToHClip(float3 posOS)
                {
                    return UnityObjectToClipPos(float4(posOS, 1.0));
                }
            #endif
            // ------------------------------------------------

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
                half  _RefractionPx, _DispGain;
                half  _Thickness, _ReflectionFactor;
                half4 _Tint;
                half  _GlareRange, _GlareHardness, _GlareConvergence, _GlareOppositeFactor, _GlareAngle, _GlareIntensity;
                half  _FresnelRange, _FresnelHardness, _FresnelIntensity;
                half  _MeltAlphaMix;
            CBUFFER_END

            // Textures / globals – same for both pipelines
            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_BgTexture);      SAMPLER(sampler_BgTexture);
            TEXTURE2D(_BlurTexture);    SAMPLER(sampler_BlurTexture);
            TEXTURE2D(_LiquidField);    SAMPLER(sampler_LiquidField);

            float4 _FieldSize;     // (W,H,1/W,1/H)
            float  _SMinK;
            float  _SMinK_Field;

            float4 _CanvasRefRes;  // (refW, refH, 0, 0)
            float  _CanvasMatch;   // CanvasScaler match

            float4 _BlurTex_TexelSize;
            float  _ResScale;
            float  _InvResScale;

            float4 _ScreenSize;    // (W,H,1/W,1/H) set from C# / SDF pass

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float2 suv : TEXCOORD1;  // screen UV
                float4 col : COLOR;
            };

            float2 ScreenUV(float4 cs)
            {
                float2 uv = cs.xy / cs.w;
                uv = uv * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                return uv;
            }

            float2 ToRTHandleUV(float2 uv)
            {
                #if defined(UNITY_RENDER_PIPELINE_UNIVERSAL)
                    return uv * _RTHandleScale.xy;
                #else
                    return uv;
                #endif
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = v.uv;
                o.suv = ScreenUV(o.pos);
                o.col = _Color * v.color;
                return o;
            }

            // Central-diff gradient of crisp SDF from field (R)
            float2 FieldNormal(float2 suv)
            {
                float2 texel = _FieldSize.zw; // (1/W,1/H)
                float r  = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv).r;
                float rx = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv + float2(texel.x,0)).r;
                float lx = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv - float2(texel.x,0)).r;
                float uy = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv + float2(0,texel.y)).r;
                float dy = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv - float2(0,texel.y)).r;
                float2 g = float2(rx - lx, uy - dy);
                return normalize(-g + 1e-6);
            }

            half4 frag(v2f i) : SV_Target
            {
                // --- 1) Screen UV, flipped once ---
                float2 suv = i.suv;
                #if UNITY_UV_STARTS_AT_TOP
                    suv.y = 1.0 - suv.y;
                #endif

                // --- 2) UI scale (CanvasScaler-like) ---
                float screenW = max(_ScreenSize.x, 1.0);
                float screenH = max(_ScreenSize.y, 1.0);

                float refW = max(_CanvasRefRes.x, 1.0);
                float refH = max(_CanvasRefRes.y, 1.0);
                float match = saturate(_CanvasMatch);

                float logW = log2(screenW / refW);
                float logH = log2(screenH / refH);
                float uiScale = exp2(lerp(logW, logH, match));  // 1 at reference, >1 on bigger screens

                // --- 3) SDF: crisp & smooth-min ---
                float4 rgbField = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv);
                float  sdPx     = rgbField.r;                    // crisp SDF in RT pixels
                float  sumExp   = max(rgbField.g, 1e-8);
                float  kField   = max(_SMinK_Field, 1e-5);
                float  sdSMin   = -log(sumExp) / kField;         // smooth-min SDF in FIELD pixels

                // Blur mask & effect intensity from field channels
                // Decode (PASS 1 stores: G=sumExp, B=sumExp*isBlur, A=sumExp*intensity)
                float blurMask = saturate(rgbField.b / sumExp);          // 0 or 1
                float effectIntensity = rgbField.a / sumExp;   // 0..1 (or >1  to remove saturate)

                // FIELD px -> RT px
                float fieldH        = max(_FieldSize.y, 1.0);
                float fieldToScreen = screenH / fieldH;
                float sdSMinPx      = sdSMin * fieldToScreen;

                // Alpha from crisp vs melt
                float aaCrisp = fwidth(sdPx);
                float aaMelt  = fwidth(sdSMinPx);
                half  aCrisp  = saturate(0.5 - sdPx     / max(aaCrisp * 2.0, 1e-5));
                half  aMelt   = saturate(0.5 - sdSMinPx / max(aaMelt  * 2.0, 1e-5));
                half  alpha   = lerp(aCrisp, aMelt, _MeltAlphaMix);

                // blended SDF (for edge behavior)
                float tM       = _MeltAlphaMix;
                float tEdge    = tM * tM;
                float sdBlendPx = lerp(sdPx, sdSMinPx, tEdge);
                float dInPx     = max(-sdBlendPx, 0.0);

                // How much refraction we want at this pixel based on distance from the edge
                float rimWidthPx = _Thickness * uiScale * effectIntensity;    // edge band width in pixels
                float refrRim    = saturate(1.0 - dInPx / rimWidthPx); // 1 at edge, 0 at center
                refrRim          = refrRim * refrRim;                  // square to concentrate near edge

                // --- 4) Normal from field (crisp + smooth-min) ---
                float2 nCrisp = FieldNormal(suv);

                float2 texel = _FieldSize.zw;   // (1/W, 1/H)
                float  kF    = max(_SMinK_Field, 1e-5);
                float  invK  = 1.0 / kF;

                float gC  = rgbField.g;
                float gRx = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv + float2(texel.x, 0)).g;
                float gLx = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv - float2(texel.x, 0)).g;
                float gUy = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv + float2(0, texel.y)).g;
                float gDy = SAMPLE_TEXTURE2D(_LiquidField, sampler_LiquidField, suv - float2(0, texel.y)).g;

                float sdC  = -log(max(gC,  1e-8)) * invK * fieldToScreen;
                float sdRx = -log(max(gRx, 1e-8)) * invK * fieldToScreen;
                float sdLx = -log(max(gLx, 1e-8)) * invK * fieldToScreen;
                float sdUy = -log(max(gUy, 1e-8)) * invK * fieldToScreen;
                float sdDy = -log(max(gDy, 1e-8)) * invK * fieldToScreen;

                float2 gSmooth = float2(sdRx - sdLx, sdUy - sdDy);
                float2 nSmooth = normalize(-gSmooth + 1e-6);

                float tN  = _MeltAlphaMix * _MeltAlphaMix * _MeltAlphaMix;
                float2 nUV = normalize(lerp(nCrisp, nSmooth, tN));

                // --- 5) Edge / thickness ---
                float nmerged     = -sdBlendPx;
                float thicknessPx = max(_Thickness * uiScale, 1e-3);
                float edgeSimple  = saturate(1.0 - nmerged / thicknessPx);

                float xR     = clamp(edgeSimple, 0.0, 0.999);
                float thetaI = asin(xR * xR);
                float nRefl  = max(_ReflectionFactor, 1e-3);
                float sT     = clamp(sin(thetaI) / nRefl, 0.0, 0.999);
                float thetaT = asin(sT);

                float dt = thetaT - thetaI;
                float t  = tan(dt);
                t        = (isfinite(t)) ? t : 0.0;

                float edgeFactor = max(0.0, -t) * edgeSimple;

                // --- 6) Refraction & dispersion ---
                // Treat _RefractionPx as 0..1, remapped to 0..kMaxPx at 1080p, then normalized by current screen size
                const float kRefHeight = 1080.0;
                const float kMaxPx     = 96.0; // maximum physical px shift at 1080p when _RefractionPx = 1

                float refPx1080 = _RefractionPx * kMaxPx;                  // pixels at 1080p
                float refPxNow  = refPx1080 * (_ScreenSize.y / kRefHeight); // scale with actual screen height
                float2 pxToUV   = _ScreenSize.zw;                          // (1/width, 1/height)

                float2 refrBase = nUV * (refPxNow * pxToUV) * edgeFactor * refrRim * effectIntensity;

                float dGain = _DispGain * 8;
                float2 ofsR = refrBase * (1.0 + dGain * (-0.02));
                float2 ofsG = refrBase * (1.0);
                float2 ofsB = refrBase * (1.0 + dGain * ( 0.02));

                half3 behindSharp;
                behindSharp.r = SAMPLE_TEXTURE2D(_BgTexture,   sampler_BgTexture,   ToRTHandleUV(suv + ofsR)).r;
                behindSharp.g = SAMPLE_TEXTURE2D(_BgTexture,   sampler_BgTexture,   ToRTHandleUV(suv + ofsG)).g;
                behindSharp.b = SAMPLE_TEXTURE2D(_BgTexture,   sampler_BgTexture,   ToRTHandleUV(suv + ofsB)).b;

                half3 behindBlur;
                behindBlur.r = SAMPLE_TEXTURE2D(_BlurTexture,  sampler_BlurTexture, ToRTHandleUV(suv + ofsR)).r;
                behindBlur.g = SAMPLE_TEXTURE2D(_BlurTexture,  sampler_BlurTexture, ToRTHandleUV(suv + ofsG)).g;
                behindBlur.b = SAMPLE_TEXTURE2D(_BlurTexture,  sampler_BlurTexture, ToRTHandleUV(suv + ofsB)).b;

                half3 behind = lerp(behindSharp, behindBlur, blurMask);

                float3 col = behind + _Color.rgb;

                // --- 7) Glare & Fresnel ---
                float glareRangePx   = _GlareRange   * uiScale;
                float fresnelRangePx = _FresnelRange * uiScale;

                float glareAngle = (atan2(nUV.y, nUV.x) - 3.14159265 * 0.25 + _GlareAngle) * 2.0;
                float sineTerm   = 0.5 + 0.5 * sin(glareAngle);
                float opp        = step(0.0, sin(glareAngle));
                float oppositeBoost = lerp(_GlareOppositeFactor, 1.0, opp);
                float glareAngleFactor = pow(
                    saturate(sineTerm * 1.2 * oppositeBoost * _GlareIntensity),
                    0.1 + _GlareConvergence * 2.0
                );

                float glareFall      = saturate(1.0 - dInPx / max(glareRangePx,   1e-3));
                float glareGeoFactor = saturate(pow(glareFall + _GlareHardness, 5.0));

                float3 glareTap = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, ToRTHandleUV(suv)).rgb * 3.0;
                col = lerp(col, glareTap, glareAngleFactor * glareGeoFactor);

                float fresnelFall   = saturate(1.0 - dInPx / max(fresnelRangePx, 1e-3));
                float fresnelFactor = saturate(pow(fresnelFall + _FresnelHardness, 5.0));
                float3 fresnelTint  = lerp(float3(1,1,1), _Tint.rgb, _Tint.a * 0.5);
                col = lerp(col, fresnelTint, fresnelFactor * _FresnelIntensity * 0.7);

                return float4(col, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
