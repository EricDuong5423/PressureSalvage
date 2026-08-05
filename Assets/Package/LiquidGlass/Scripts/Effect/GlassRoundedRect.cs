using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LGU
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(CanvasRenderer))]
    public class GlassRoundedRect : Image
    {
        [Header("Shape")]
        [Range(0, 1)] public float cornerRadius = 0.3f;

        [Header("Master Intensity")]
        [Range(0, 5)] public float effectIntensity = 1f;

        public enum FillStyle { None, HorizontalForward, HorizontalBackwards, VerticalForward, VerticalBackwards }
        public FillStyle fillStyle = FillStyle.None;

        [Header("Optional Sprite Shape (UI Image)")]
        public bool useSpriteShape = false;

        [Header("Liquid Glass")]
        [Range(0, 8)] public float refractionPx = 0.3f;
        [Range(0, 2)] public float dispersionGain = 0.6f;

        [Range(1, 500)] public float thicknessPx = 90;
        [Range(0.8f, 1.5f)] public float reflectionFactor = 1.15f;

        [Header("Fresnel Rim")]
        public Color highlightTint = new Color(1, 1, 1, 0.35f);
        [Range(0, 120)] public float fresnelRange = 5;
        [Range(0, 1)] public float fresnelHardness = 0.2f;
        [Range(0, 1)] public float fresnelIntensity = 0.2f;

        [Header("Glare Band")]
        [Range(0, 120)] public float glareRange = 6;
        [Range(0, 1)] public float glareHardness = 0.8f;
        [Range(0, 1)] public float glareConvergence = 0.3f;
        [Range(0, 2)] public float glareOppositeFactor = 0.8f;
        public float glareAngle = -0.85f;          // radians
        [Range(0, 2)] public float glareIntensity = 1.9f;

        [Header("Overall Tint")]
        public bool isBlur = false;
        private bool _bool_is_blur = false;

        [Header("Blur Input")]
        public RenderTexture blurTexture; // assign scene blur RT here

        [Header("Shadow")]
        public bool showShadow = false;
        public Color shadowColor;
        public float shadowRangePx = 70f;
        public float shadowHardness = 0.0f;
        public float shadowIntensity = 0.65f;
        public Vector2 shadowOffset = new Vector2(5, -5);

        private Color _gizmoColor = new Color(1f, 0.75f, 0.2f, 0.9f);
        static readonly Vector3[] _gizmoCorners = new Vector3[4];

        // ===== Shader property IDs =====
        static readonly int _Color = Shader.PropertyToID("_Color");
        static readonly int _EffectIntensity = Shader.PropertyToID("_EffectIntensity");
        static readonly int _RefractionPx = Shader.PropertyToID("_RefractionPx");
        static readonly int _DispGain = Shader.PropertyToID("_DispGain");
        static readonly int _Thickness = Shader.PropertyToID("_Thickness");
        static readonly int _ReflectionFactor = Shader.PropertyToID("_ReflectionFactor");
        static readonly int _Tint = Shader.PropertyToID("_Tint");
        static readonly int _GlareRange = Shader.PropertyToID("_GlareRange");
        static readonly int _GlareHardness = Shader.PropertyToID("_GlareHardness");
        static readonly int _GlareConvergence = Shader.PropertyToID("_GlareConvergence");
        static readonly int _GlareOppositeFactor = Shader.PropertyToID("_GlareOppositeFactor");
        static readonly int _GlareAngle = Shader.PropertyToID("_GlareAngle");
        static readonly int _GlareIntensity = Shader.PropertyToID("_GlareIntensity");

        static readonly int _FresnelRange = Shader.PropertyToID("_FresnelRange");
        static readonly int _FresnelHardness = Shader.PropertyToID("_FresnelHardness");
        static readonly int _FresnelIntensity = Shader.PropertyToID("_FresnelIntensity");

        static readonly int _BlurTexture = Shader.PropertyToID("_BlurTexture");
        static readonly int _BlurTex_TexelSize = Shader.PropertyToID("_BlurTex_TexelSize");

        static readonly int _EnableOuterShadow = Shader.PropertyToID("_EnableOuterShadow");
        static readonly int _ShadowColor = Shader.PropertyToID("_ShadowColor");
        static readonly int _ShadowRangePx = Shader.PropertyToID("_ShadowRangePx");
        static readonly int _ShadowHardness = Shader.PropertyToID("_ShadowHardness");
        static readonly int _ShadowIntensity = Shader.PropertyToID("_ShadowIntensity");

        // Sprite SDF shape
        static readonly int _UseShape = Shader.PropertyToID("_UseShape");
        static readonly int _ShapeTex = Shader.PropertyToID("_ShapeTex");
        static readonly int _ShapeMaxDistTexel = Shader.PropertyToID("_ShapeMaxDistTexel");
        static readonly int _ShapeScreenPxPerTexelUV = Shader.PropertyToID("_ShapeScreenPxPerTexelUV");

        static readonly string KW_IS_BLUR = "IS_BLUR";

        protected override void Awake()
        {
            base.Awake();
            LoadMateial(force: true);
            ApplyMaterialParams();
        }

        private void LoadMateial(bool force = false)
        {
            if (material == null || force)
            {
                var m = Resources.Load<Material>(isBlur ? "MaterialGlassRoundedRectBlur" : "MaterialGlassRoundedRect");
                if (m != null) material = new Material(m);
                else
                {
                    var sh = Shader.Find("LiquidGlass/GlassRoundedRectUI");
                    if (sh != null) material = new Material(sh);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
            ApplyMaterialParams();
            SetVerticesDirty();
        }

        void EnsureCanvasChannels()
        {
            if (!canvas) return;
            var c = canvas.additionalShaderChannels;
            if (!c.HasFlag(AdditionalCanvasShaderChannels.TexCoord1))
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            if (!c.HasFlag(AdditionalCanvasShaderChannels.TexCoord2))
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            if (!c.HasFlag(AdditionalCanvasShaderChannels.TexCoord3))
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
        }

        void ApplyMaterialParams()
        {
            if (material == null)
                LoadMateial();

            if (isBlur != _bool_is_blur)
            {
                _bool_is_blur = isBlur;
                LoadMateial(force: true);
            }

            if (!material) return;

            material.SetFloat(_EffectIntensity, effectIntensity);

            material.SetFloat(_RefractionPx, refractionPx);
            material.SetFloat(_DispGain, dispersionGain);

            material.SetFloat(_Thickness, thicknessPx);
            material.SetFloat(_ReflectionFactor, reflectionFactor);

            material.SetColor(_Tint, highlightTint);

            material.SetFloat(_GlareRange, glareRange);
            material.SetFloat(_GlareHardness, glareHardness);
            material.SetFloat(_GlareConvergence, glareConvergence);
            material.SetFloat(_GlareOppositeFactor, glareOppositeFactor);
            material.SetFloat(_GlareAngle, glareAngle);
            material.SetFloat(_GlareIntensity, glareIntensity);

            material.SetFloat(_FresnelRange, fresnelRange);
            material.SetFloat(_FresnelHardness, fresnelHardness);
            material.SetFloat(_FresnelIntensity, fresnelIntensity);

            material.SetFloat(_EnableOuterShadow, showShadow ? 1 : 0);
            material.SetColor(_ShadowColor, shadowColor);
            material.SetFloat(_ShadowRangePx, shadowRangePx);
            material.SetFloat(_ShadowHardness, shadowHardness);
            material.SetFloat(_ShadowIntensity, shadowIntensity);

            // Blur texture + texel size
            if (blurTexture != null)
            {
                material.SetTexture(_BlurTexture, blurTexture);
                var w = Mathf.Max(1, blurTexture.width);
                var h = Mathf.Max(1, blurTexture.height);
                material.SetVector(_BlurTex_TexelSize, new Vector4(1f / w, 1f / h, w, h));
            }

            if (isBlur) material.EnableKeyword(KW_IS_BLUR);
            else material.DisableKeyword(KW_IS_BLUR);

            // -------------------------
            // Sprite SDF support (UI)
            // -------------------------
            bool enableShape =
                useSpriteShape &&
                sprite != null &&
                type == Type.Simple; // keep it simple & predictable

            if (enableShape && SpriteSDFCache.TryGet(sprite, out var sdf) && sdf.tex != null)
            {
                material.SetFloat(_UseShape, 1f);
                material.SetTexture(_ShapeTex, sdf.tex);
                material.SetFloat(_ShapeMaxDistTexel, sdf.maxDistTexel);

                // rect px per SDF texel (U and V)
                var r = GetPixelAdjustedRect();
                float pxPerTexU = r.width / Mathf.Max(1, sdf.width);
                float pxPerTexV = r.height / Mathf.Max(1, sdf.height);
                material.SetVector(_ShapeScreenPxPerTexelUV, new Vector4(pxPerTexU, pxPerTexV, 0, 0));
            }
            else
            {
                material.SetFloat(_UseShape, 0f);
                material.SetTexture(_ShapeTex, Texture2D.whiteTexture);
                material.SetFloat(_ShapeMaxDistTexel, 1f);
                material.SetVector(_ShapeScreenPxPerTexelUV, new Vector4(1, 1, 0, 0));
            }
        }

        public override void SetMaterialDirty()
        {
            base.SetMaterialDirty();
            ApplyMaterialParams();
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();
            ApplyMaterialParams();
        }

        public void Refresh()
        {
            SetVerticesDirty();
            ApplyMaterialParams();
        }

        void LateUpdate()
        {
            // Keep texel size fresh if RT can resize
            if (material != null && blurTexture != null)
            {
                var w = Mathf.Max(1, blurTexture.width);
                var h = Mathf.Max(1, blurTexture.height);
                material.SetVector(_BlurTex_TexelSize, new Vector4(1f / w, 1f / h, w, h));
            }

            // In case sprite changes in editor / runtime
            if (material != null)
                ApplyMaterialParams();
        }

        // ===== Mesh generation: pack rect size, corner radius, and padding in UVs =====
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();
            var v = GetVertexPositions(r);

            vh.Clear();

            Vector2 rectSizeOriginal = r.size;

            float minHalf = Mathf.Min(rectSizeOriginal.x, rectSizeOriginal.y) * 0.5f;
            float maxRadiusPx = Mathf.Max(0f, minHalf - 0.5f);
            float radiusPx = Mathf.Clamp(cornerRadius * minHalf, 0f, maxRadiusPx);
            Vector2 roundedAndBorder = new Vector2(radiusPx, 0f);

            var uvs = GetUVValues(r);
            Vector2 uv1 = new Vector2(uvs.x, uvs.y);
            Vector2 uv2 = new Vector2(uvs.x, uvs.w);
            Vector2 uv3 = new Vector2(uvs.z, uvs.w);
            Vector2 uv4 = new Vector2(uvs.z, uvs.y);

            Vector2 typeV = new Vector2();

            int i = 0;

            typeV = new Vector2(0, 0); //Fill
            vh.AddVert(new Vector3(v.x, v.y), color, uv1, r.size, roundedAndBorder, typeV, new Vector3(1, 0, 0), Vector4.zero);
            vh.AddVert(new Vector3(v.x, v.w), color, uv2, r.size, roundedAndBorder, typeV, new Vector3(0, 1, 0), Vector4.zero);
            vh.AddVert(new Vector3(v.z, v.w), color, uv3, r.size, roundedAndBorder, typeV, new Vector3(0, 1, 1), Vector4.zero);
            vh.AddVert(new Vector3(v.z, v.y), color, uv4, r.size, roundedAndBorder, typeV, new Vector3(0, 0, 1), Vector4.zero);

            vh.AddTriangle(i + 0, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i + 0);
            i += 4;
        }

        private Vector4 GetVertexPositions(Rect r)
        {
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
            if (fillStyle == FillStyle.None) return v;

            float oneMinusFill = 1f - fillAmount;
            switch (fillStyle)
            {
                case FillStyle.HorizontalBackwards: v.x += r.width * oneMinusFill; break;
                case FillStyle.HorizontalForward: v.z -= r.width * oneMinusFill; break;
                case FillStyle.VerticalBackwards: v.y += r.height * oneMinusFill; break;
                case FillStyle.VerticalForward: v.w -= r.height * oneMinusFill; break;
            }
            return v;
        }

        private Vector4 GetUVValues(Rect r)
        {
            var uvs = new Vector4(0, 0, 1, 1);
            if (fillStyle == FillStyle.None) return uvs;

            float oneMinusFill = 1f - fillAmount;
            switch (fillStyle)
            {
                case FillStyle.HorizontalBackwards: uvs.x = oneMinusFill; break;
                case FillStyle.HorizontalForward: uvs.z = fillAmount; break;
                case FillStyle.VerticalBackwards: uvs.y = oneMinusFill; break;
                case FillStyle.VerticalForward: uvs.w = fillAmount; break;
            }
            return uvs;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }

        void OnDrawGizmosSelected()
        {
            var rt = rectTransform;
            if (!rt) return;

            rt.GetWorldCorners(_gizmoCorners);

            float width = Vector3.Distance(_gizmoCorners[0], _gizmoCorners[3]);
            float height = Vector3.Distance(_gizmoCorners[0], _gizmoCorners[1]);
            float minAxis = Mathf.Min(width, height);

            float r = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis;
            // if (r <= 0.001f)
            // {
            //     Handles.color = _gizmoColor;
            //     Handles.DrawAAPolyLine(
            //         2.5f,
            //         _gizmoCorners[0], _gizmoCorners[1], _gizmoCorners[2], _gizmoCorners[3], _gizmoCorners[0]
            //     );
            //     return;
            // }

            Handles.color = _gizmoColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Utils.DrawRoundedRectWorld(_gizmoCorners, r, 12);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            LoadMateial(force: true);
            ApplyMaterialParams();
            SetAllDirty();
            if (!Application.isPlaying)
            {
                UnityEditor.SceneView.RepaintAll();
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            LoadMateial();
            EnsureCanvasChannels();
            Refresh();
        }
#endif
    }
}
