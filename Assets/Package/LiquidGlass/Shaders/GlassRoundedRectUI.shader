Shader "LiquidGlass/GlassRoundedRectUI"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite (alpha can mask)", 2D) = "white" {}
        _Color ("Overall Tint (multiplies output)", Color) = (0,0,0,1)

        _RefractionPx    ("Refraction Strength (px)", Range(0, 8)) = 0.5
        _DispGain        ("Dispersion Gain",          Range(0, 2)) = 0.2

        _Thickness       ("Glass Thickness (px)",     Range(1, 128)) =128
        _ReflectionFactor("Reflection Factor (IOR)",  Range(0.8, 1.5)) = 1.13

        _Tint            ("Highlight Tint (RGB) & Amount (A)", Color) = (1,1,1,0.35)

        // Glare band
        _GlareRange          ("Glare Range",        Float) = 54
        _GlareHardness       ("Glare Hardness",     Range(0,1)) = 0.8
        _GlareConvergence    ("Glare Convergence",  Range(0,1)) = 0.3
        _GlareOppositeFactor ("Glare Opposite Fac", Range(0,2)) = 0.8
        _GlareAngle          ("Glare Angle (rad)",  Float) = -0.85
        _GlareIntensity      ("Glare Intensity",    Range(0,2)) = 1.9

        // Fresnel rim
        _FresnelRange        ("Fresnel Range",        Float) = 66
        _FresnelHardness     ("Fresnel Hardness",     Range(0,1)) = 0.2
        _FresnelIntensity    ("Fresnel Intensity",    Range(0,2)) = 0.2

        // Outer Shadow
        [Toggle]_EnableOuterShadow ("Enable Outer Shadow", Float) = 0
        _ShadowColor        ("Shadow Color (RGBA)", Color) = (0,0,0,0.55)
        _ShadowRangePx      ("Shadow Range (px)", Float) = 70
        _ShadowHardness     ("Shadow Hardness", Range(0,1)) = 0.0
        _ShadowIntensity    ("Shadow Intensity", Range(0,2)) = 0.65
        _ShadowOffsetPx     ("Shadow Offset (px)", Vector) = (0,-7.17,0,0)

        _EffectIntensity ("Effect Intensity", Range(0,5)) = 1

        [Toggle]_UseSpriteMask("Use Sprite Alpha As Mask", Float) = 0

        // --- Sprite SDF support (UI only) ---
        [Toggle]_UseShape("Use Sprite Shape SDF", Float) = 0
        _ShapeTex("Shape SDF", 2D) = "white" {}
        _ShapeMaxDistTexel("Shape Max Dist (texel)", Float) = 32
        _ShapeScreenPxPerTexelUV("Shape Px Per Texel (U,V)", Vector) = (1,1,0,0)

        // UI / stencil
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
        [HideInInspector][Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "RoundedRectGlassUI_SDF"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment UNITY_UI_CLIP_RECT
            #pragma shader_feature_local_fragment UNITY_UI_ALPHACLIP
            #pragma multi_compile_instancing
            #pragma multi_compile _ MOBILE_FAST
            #pragma multi_compile _ IS_BLUR

            #if defined(UNITY_RENDER_PIPELINE_UNIVERSAL)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #else
                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                #define TEXTURE2D(name) sampler2D name
                #define SAMPLER(name) sampler2D name
                #define SAMPLE_TEXTURE2D(tex, samp, uv) tex2D(tex, uv)

                float4 TransformObjectToHClip(float3 posOS)
                {
                    return UnityObjectToClipPos(float4(posOS, 1.0));
                }

                float3 TransformObjectToWorld(float3 posOS)
                {
                    return mul(unity_ObjectToWorld, float4(posOS, 1.0)).xyz;
                }
            #endif

            inline float LGU_Get2DClip(float2 position, float4 clipRect)
            {
                #ifdef UNITY_UI_CLIP_RECT
                    float2 inside01 = step(clipRect.xy, position) * step(position, clipRect.zw);
                    return inside01.x * inside01.y;
                #else
                    return 1.0;
                #endif
            }

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;

                half  _RefractionPx;
                half  _DispGain;

                half  _Thickness;
                half  _ReflectionFactor;

                half4 _Tint;

                half  _GlareRange;
                half  _GlareHardness;
                half  _GlareConvergence;
                half  _GlareOppositeFactor;
                half  _GlareAngle;
                half  _GlareIntensity;

                half  _FresnelRange;
                half  _FresnelHardness;
                half  _FresnelIntensity;

                half  _EnableOuterShadow;
                half4 _ShadowColor;
                float _ShadowRangePx;
                half  _ShadowHardness;
                half  _ShadowIntensity;
                float4 _ShadowOffsetPx;

                half  _UseSpriteMask;
                half  _EffectIntensity;

                // Sprite SDF support
                half  _UseShape;
                float _ShapeMaxDistTexel;
                float4 _ShapeScreenPxPerTexelUV;

                // UI clipping
                float4 _ClipRect;
                float  _UIMaskSoftnessX;
                float  _UIMaskSoftnessY;
            CBUFFER_END

            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_BgTexture);   SAMPLER(sampler_BgTexture);
            TEXTURE2D(_BlurTexture); SAMPLER(sampler_BlurTexture);

            TEXTURE2D(_ShapeTex);    SAMPLER(sampler_ShapeTex);

            float4 _MainTex_TexelSize;
            float4 _BlurTex_TexelSize;
            float4 _ShapeTex_TexelSize;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;   // 0..1
                float2 texcoord1: TEXCOORD1;   // rectSizePx (x,y)
                float2 texcoord2: TEXCOORD2;   // (roundedPx, unused)
                float2 texcoord3: TEXCOORD3;   // (unused, typeY)
                float4 color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float2 rectSize  : TEXCOORD1;
                half4  vcolor    : COLOR0;
                float  roundedPx : TEXCOORD2;
                float  type      : TEXCOORD3;
                float2 screenUV  : TEXCOORD4;
                float2 worldXY   : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float2 GetScreenUV(float4 positionCS)
            {
                float2 uv = positionCS.xy / positionCS.w;
                uv = uv * 0.5f + 0.5f;
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

            float RoundedRectSDF_Px(float2 uv, float radiusPx, float2 rectSizePx)
            {
                float2 halfRect = rectSizePx * 0.5;
                float maxR = min(halfRect.x, halfRect.y) - 0.5;
                float r    = clamp(radiusPx, 0.0, maxR);

                float2 p = uv * rectSizePx - halfRect;
                float2 b = halfRect - r;

                float2 d   = abs(p) - b;
                float  outD = length(max(d, 0.0)) - r;
                float  inD  = min(max(d.x, d.y), 0.0);

                return outD + inD; // <0 inside, >0 outside
            }

            float2 RoundedRectGradient(float2 uv, float radiusPx, float2 rectSizePx, float2 texel)
            {
                float dR = RoundedRectSDF_Px(uv + float2(texel.x, 0), radiusPx, rectSizePx);
                float dL = RoundedRectSDF_Px(uv - float2(texel.x, 0), radiusPx, rectSizePx);
                float dU = RoundedRectSDF_Px(uv + float2(0, texel.y), radiusPx, rectSizePx);
                float dD = RoundedRectSDF_Px(uv - float2(0, texel.y), radiusPx, rectSizePx);
                float2 g = float2(dR - dL, dU - dD);
                return normalize(-g + 1e-5);
            }

            // Sprite SDF decode: returns distance in PIXELS (<0 inside, >0 outside)
            float ShapeSDF_Px(float2 uv)
            {
                float enc = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv).r;

                float maxD = max(_ShapeMaxDistTexel, 1e-3);
                float dTexel = (enc * 2.0 - 1.0) * maxD;

                float pxU = max(_ShapeScreenPxPerTexelUV.x, 1e-6);
                float pxV = max(_ShapeScreenPxPerTexelUV.y, 1e-6);
                float pxPerTexel = sqrt(pxU * pxV);

                return dTexel * pxPerTexel;
            }

            float2 ShapeGradient(float2 uv)
            {
                float2 t = _ShapeTex_TexelSize.xy; // 1/w,1/h
                float dR = ShapeSDF_Px(uv + float2(t.x, 0));
                float dL = ShapeSDF_Px(uv - float2(t.x, 0));
                float dU = ShapeSDF_Px(uv + float2(0, t.y));
                float dD = ShapeSDF_Px(uv - float2(0, t.y));
                float2 g = float2(dR - dL, dU - dD);
                return normalize(-g + 1e-5);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos      = TransformObjectToHClip(v.vertex.xyz);
                o.uv       = v.texcoord;
                o.vcolor   = (half4)_Color * (half4)v.color;
                o.rectSize = v.texcoord1;
                o.roundedPx= v.texcoord2.x;
                o.type     = v.texcoord3.y;

                o.screenUV = GetScreenUV(o.pos);

                float3 wpos = TransformObjectToWorld(v.vertex.xyz);
                o.worldXY = wpos.xy;
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float2 rectSizePx = IN.rectSize;
                float2 uv         = saturate(IN.uv);

                float  roundedPx  = IN.roundedPx;
                half4  vcol       = IN.vcolor;

                // Choose SDF source + normal source
                float sdfPx;
                float2 nUV;

                if (_UseShape > 0.5)
                {
                    sdfPx = ShapeSDF_Px(uv);
                    nUV   = ShapeGradient(uv);
                }
                else
                {
                    sdfPx = RoundedRectSDF_Px(uv, max(roundedPx, 0.0), rectSizePx);

                    #ifdef MOBILE_FAST
                        float2 p = uv * 2.0 - 1.0;
                        nUV = normalize(p + 1e-5);
                    #else
                        nUV = RoundedRectGradient(uv, max(roundedPx, 0.0), rectSizePx, _MainTex_TexelSize.xy);
                    #endif
                }

                // Fill alpha from SDF
                float aa = max(fwidth(sdfPx) * 1.5, 1e-4);
                float fillMask = saturate(smoothstep(aa, -aa, sdfPx));

                float spriteA = 1.0;
                if (_UseSpriteMask > 0.5)
                    spriteA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                float alphaFill = fillMask * spriteA;

                // UI clip
                alphaFill *= LGU_Get2DClip(IN.worldXY, _ClipRect);
                alphaFill *= step(0.001, alphaFill);

                // Edge geometry from SDF
                float distInside  = -sdfPx;
                float edgeSimple = saturate(1.0 - distInside / max(_Thickness, 1e-3));
                float xR         = clamp(edgeSimple, 0.0, 0.999);

                float thetaI = asin(xR * xR);
                float nRefl  = max(_ReflectionFactor, 1e-3);
                float sT     = clamp(sin(thetaI) / nRefl, 0.0, 0.999);
                float thetaT = asin(sT);

                float dt = thetaT - thetaI;
                float t  = tan(dt);
                t = (abs(t) < 1e6) ? t : 0.0;

                float edgeFactor = max(0.0, -t) * edgeSimple;

                // Refraction & dispersion
                float2 refrBase = nUV * _RefractionPx * edgeFactor * _EffectIntensity;

                float dGain = _DispGain * 20;
                float2 ofsR = refrBase * (1.0 + dGain * (-0.02));
                float2 ofsG = refrBase * 1.0;
                float2 ofsB = refrBase * (1.0 + dGain * 0.02);

                float2 suv = IN.screenUV;
                #if defined(UNITY_UV_STARTS_AT_TOP)
                    suv.y = 1.0 - suv.y;
                #endif

                half3 behind;
                #ifdef IS_BLUR
                    behind.r = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, ToRTHandleUV(suv + ofsR)).r;
                    behind.g = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, ToRTHandleUV(suv + ofsG)).g;
                    behind.b = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, ToRTHandleUV(suv + ofsB)).b;
                #else
                    behind.r = SAMPLE_TEXTURE2D(_BgTexture, sampler_BgTexture,   ToRTHandleUV(suv + ofsR)).r;
                    behind.g = SAMPLE_TEXTURE2D(_BgTexture, sampler_BgTexture,   ToRTHandleUV(suv + ofsG)).g;
                    behind.b = SAMPLE_TEXTURE2D(_BgTexture, sampler_BgTexture,   ToRTHandleUV(suv + ofsB)).b;
                #endif

                half3 col = behind + _Color.rgb;

                // Glare band
                float glareAngle = (atan2(nUV.y, nUV.x) - 3.14159265 * 0.25 + _GlareAngle) * 2.0;
                float sineTerm   = 0.5 + 0.5 * sin(glareAngle);
                float opp        = step(0.0, sin(glareAngle));
                float oppositeBoost = lerp(_GlareOppositeFactor, 1.0, opp);
                float glareAngleFactor = pow(
                    saturate(sineTerm * 1.2 * oppositeBoost * _GlareIntensity),
                    0.1 + _GlareConvergence * 2.0
                );

                float glareGeoFactor = saturate(
                    pow(1.0 + sdfPx / max(_GlareRange, 1e-3) + _GlareHardness, 5.0)
                );

                half4 blurTexForGlare = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, ToRTHandleUV(suv)) * 2;
                col = lerp(col, blurTexForGlare.rgb, glareAngleFactor * glareGeoFactor);

                // Fresnel rim
                float fresnelFactor = saturate(
                    pow(1.0 + sdfPx / max(_FresnelRange, 1e-3) + _FresnelHardness, 5.0)
                );
                half3 fresnelTint = lerp(half3(1,1,1), _Tint.rgb, _Tint.a * 0.5);
                col = lerp(col, fresnelTint, fresnelFactor * _FresnelIntensity * 0.7);

                float outA = vcol.a * saturate(alphaFill);

                #if defined(UNITY_UI_ALPHACLIP)
                    clip(outA - 0.001);
                #endif

                return half4(col, outA);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
