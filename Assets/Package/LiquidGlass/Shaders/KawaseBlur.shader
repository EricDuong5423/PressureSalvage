Shader "LiquidGlass/KawaseBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Offset  ("Ring Offset (pixels)", Float) = 2.0
        _Blend   ("Stability Blend 0..1", Range(0,1)) = 0.45
    }

    SubShader
    {
        // Fullscreen blit
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Unity 6+ Blit.hlsl declares _BlitTexture_TexelSize; older versions don't
            #if UNITY_VERSION < 202300
            float4 _BlitTexture_TexelSize;
            #endif

            float _Offset;
            float _Blend;

            half4 Kawase8(float2 uv, float2 texel, float off)
            {
                float2 o = off * texel;

                half4 s =
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( o.x,  0   ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-o.x,  0   ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( 0,    o.y ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( 0,   -o.y ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( o.x,  o.y ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-o.x,  o.y ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( o.x, -o.y ), 0) +
                    SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-o.x, -o.y ), 0);

                return s * (1.0 / 8.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;

                half4 center = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                half4 ring   = Kawase8(uv, texel, max(_Offset, 0.0));

                return lerp(ring, (ring + center) * 0.5, saturate(_Blend));
            }
            ENDHLSL
        }
    }

    // Fallback for Built-in RP (uses _MainTex)
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float     _Offset;
            float     _Blend;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 Kawase8(float2 uv, float2 texel, float off)
            {
                float2 o = off * texel;

                fixed4 s =
                    tex2D(_MainTex, uv + float2( o.x,  0   )) +
                    tex2D(_MainTex, uv + float2(-o.x,  0   )) +
                    tex2D(_MainTex, uv + float2( 0,    o.y )) +
                    tex2D(_MainTex, uv + float2( 0,   -o.y )) +
                    tex2D(_MainTex, uv + float2( o.x,  o.y )) +
                    tex2D(_MainTex, uv + float2(-o.x,  o.y )) +
                    tex2D(_MainTex, uv + float2( o.x, -o.y )) +
                    tex2D(_MainTex, uv + float2(-o.x, -o.y ));

                return s * (1.0 / 8.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                fixed4 center = tex2D(_MainTex, i.uv);
                fixed4 ring   = Kawase8(i.uv, texel, max(_Offset, 0.0));

                return lerp(ring, (ring + center) * 0.5, saturate(_Blend));
            }
            ENDCG
        }
    }

    Fallback Off
}
