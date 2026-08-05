using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

#if UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace LGU
{
    // Deduplicate overlapping regions so identical blobs do not inflate the field.
    internal struct GlassRegionKey : System.IEquatable<GlassRegionKey>
    {
        const float kQuant = 16f; // ~0.0625 px steps to absorb float noise

        readonly int x0, y0, x1, y1;
        readonly int radius;
        readonly int effect;
        readonly bool isBlur;
        readonly bool useShape;

        readonly int shapeId;
        readonly int originX, originY;
        readonly int inv00, inv01, inv10, inv11;
        readonly int maxDist;
        readonly int pxU, pxV;

        static int Q(float v) => Mathf.RoundToInt(v * kQuant);

        public GlassRegionKey(IGlassRegionProvider r, IGlassShapeProvider s)
        {
            x0 = Q(r.ScreenRectPx.x);
            y0 = Q(r.ScreenRectPx.y);
            x1 = Q(r.ScreenRectPx.z);
            y1 = Q(r.ScreenRectPx.w);
            radius = Q(r.RadiusPx);
            effect = Q(r.EffectIntensity);
            isBlur = r.IsBlur;

            useShape = s != null && s.UseShape && s.ShapeSDF;
            if (useShape)
            {
                shapeId = s.ShapeSDF ? s.ShapeSDF.GetInstanceID() : 0;
                originX = Q(s.ShapeOriginScreenPx.x);
                originY = Q(s.ShapeOriginScreenPx.y);
                inv00 = Q(s.ShapeInvM.x);
                inv01 = Q(s.ShapeInvM.y);
                inv10 = Q(s.ShapeInvM.z);
                inv11 = Q(s.ShapeInvM.w);
                maxDist = Q(s.ShapeMaxDistTexel);
                pxU = Q(s.ShapeScreenPxPerTexelUV.x);
                pxV = Q(s.ShapeScreenPxPerTexelUV.y);
            }
            else
            {
                shapeId = 0;
                originX = originY = inv00 = inv01 = inv10 = inv11 = maxDist = pxU = pxV = 0;
            }
        }

        public static bool TryCreate(object src, out GlassRegionKey key)
        {
            if (src is IGlassRegionProvider r)
            {
                key = new GlassRegionKey(r, src as IGlassShapeProvider);
                return true;
            }

            key = default;
            return false;
        }

        public bool Equals(GlassRegionKey other)
        {
            return x0 == other.x0 && y0 == other.y0 &&
                   x1 == other.x1 && y1 == other.y1 &&
                   radius == other.radius &&
                   effect == other.effect &&
                   isBlur == other.isBlur &&
                   useShape == other.useShape &&
                   shapeId == other.shapeId &&
                   originX == other.originX && originY == other.originY &&
                   inv00 == other.inv00 && inv01 == other.inv01 &&
                   inv10 == other.inv10 && inv11 == other.inv11 &&
                   maxDist == other.maxDist &&
                   pxU == other.pxU && pxV == other.pxV;
        }

        public override bool Equals(object obj) => obj is GlassRegionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = x0;
                h = (h * 397) ^ y0;
                h = (h * 397) ^ x1;
                h = (h * 397) ^ y1;
                h = (h * 397) ^ radius;
                h = (h * 397) ^ effect;
                h = (h * 397) ^ (isBlur ? 1 : 0);
                h = (h * 397) ^ (useShape ? 1 : 0);
                h = (h * 397) ^ shapeId;
                h = (h * 397) ^ originX;
                h = (h * 397) ^ originY;
                h = (h * 397) ^ inv00;
                h = (h * 397) ^ inv01;
                h = (h * 397) ^ inv10;
                h = (h * 397) ^ inv11;
                h = (h * 397) ^ maxDist;
                h = (h * 397) ^ pxU;
                h = (h * 397) ^ pxV;
                return h;
            }
        }
    }

    public class LiquidGlassBlurRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            [Header("Blur")]
            [Range(0, 20)] public int blurPasses = 2;
            [Range(1, 100)] public int blurDownsample = 4;

            [Header("SDF Field / Mask")]
            [Range(1, 4)] public int fieldDownsample = 1;     // 1 = full-res field; 2..4 downsample
            [Range(0.01f, 0.05f)] public float _SMinK = 0.02f; // base k at ref resolution

            [Header("Perf / Safety")]
            public bool skipSceneViewCamera = true;
            public bool skipPreviewCamera = true;
        }

        public static LiquidGlassBlurRendererFeature Instance;

        public Settings settings = new Settings();

        UIBlurRenderPass blurRenderPass;
        GlassSDFPass glassSDFPass;

        public override void Create()
        {
            if (Instance == null) Instance = this;

            blurRenderPass = new UIBlurRenderPass(settings)
            {
                renderPassEvent = settings.renderPassEvent
            };

            glassSDFPass = new GlassSDFPass(settings)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camData = renderingData.cameraData;

            if (settings.skipSceneViewCamera && camData.isSceneViewCamera) return;
#if UNITY_EDITOR
            if (settings.skipPreviewCamera && camData.cameraType == CameraType.Preview) return;
#endif
            if (camData.renderType != CameraRenderType.Base) return;

            renderer.EnqueuePass(blurRenderPass);
            renderer.EnqueuePass(glassSDFPass);
        }

        protected override void Dispose(bool disposing)
        {
            blurRenderPass?.Dispose();
            glassSDFPass?.Dispose();
            blurRenderPass = null;
            glassSDFPass = null;
        }

        // --------------------------------------------------------------------

        class UIBlurRenderPass : ScriptableRenderPass
        {
            const float kRefHeight = 1080f;
            static readonly Vector4 kScaleBias = new Vector4(1f, 1f, 0f, 0f);

            static readonly int BGTexID         = Shader.PropertyToID("_BgTexture");
            static readonly int BlurTexID       = Shader.PropertyToID("_BlurTexture");
            static readonly int BlurTexelSizeID = Shader.PropertyToID("_BlurTex_TexelSize");
            static readonly int OffsetID        = Shader.PropertyToID("_Offset");
            static readonly int _ResScale       = Shader.PropertyToID("_ResScale");
            static readonly int _InvResScale    = Shader.PropertyToID("_InvResScale");
            static readonly int _MainTexID      = Shader.PropertyToID("_MainTex");

            // Unity 6 Blit.hlsl properties
            static readonly int _BlitTextureID          = Shader.PropertyToID("_BlitTexture");
            static readonly int _BlitTexture_TexelSizeID = Shader.PropertyToID("_BlitTexture_TexelSize");
            static readonly int _BlitScaleBiasID        = Shader.PropertyToID("_BlitScaleBias");

            readonly Settings settings;
            Material blurMaterial;

            const string kTag = "LGU_Blur";

            int rtWidth, rtHeight;

#if UNITY_2022_1_OR_NEWER
            // Unity 6+ (URP): RTHandle workflow
            RTHandle sourceHandle;
            RTHandle tmp1;
            RTHandle tmp2;
#else
            // Legacy (Unity 2021/2022/2023/2024 URP): TemporaryRT workflow
            readonly int tmpId1 = Shader.PropertyToID("LGU_tmpBlurRT1");
            readonly int tmpId2 = Shader.PropertyToID("LGU_tmpBlurRT2");

            RenderTargetIdentifier tmpRT1;
            RenderTargetIdentifier tmpRT2;

            RTHandle sourceHandle;
#endif

            public UIBlurRenderPass(Settings settings)
            {
                this.settings = settings;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // cameraColorTargetHandle exists in URP 12+
                sourceHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

#if UNITY_2022_1_OR_NEWER
                if (!blurMaterial)
                    blurMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/KawaseBlur"));

                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;

                int ds = Mathf.Max(1, settings.blurDownsample);
                rtWidth  = Mathf.Max(1, desc.width / ds);
                rtHeight = Mathf.Max(1, desc.height / ds);

                desc.width = rtWidth;
                desc.height = rtHeight;

                RenderingUtils.ReAllocateIfNeeded(ref tmp1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LGU_tmpBlurRT1");
                RenderingUtils.ReAllocateIfNeeded(ref tmp2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LGU_tmpBlurRT2");

                // ✅ Unity 6: ConfigureTarget wants RTHandle
                ConfigureTarget(tmp1);
#endif
            }

#if !UNITY_2022_1_OR_NEWER
            // Legacy path keeps your original structure to support old Unity/URP.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (!blurMaterial)
                    blurMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/KawaseBlur"));

                int ds = Mathf.Max(1, settings.blurDownsample);
                rtWidth  = Mathf.Max(1, cameraTextureDescriptor.width  / ds);
                rtHeight = Mathf.Max(1, cameraTextureDescriptor.height / ds);

                cmd.GetTemporaryRT(tmpId1, rtWidth, rtHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
                cmd.GetTemporaryRT(tmpId2, rtWidth, rtHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

                tmpRT1 = new RenderTargetIdentifier(tmpId1);
                tmpRT2 = new RenderTargetIdentifier(tmpId2);

                // ✅ Legacy: OK (Unity 6 complains, so we #if it away)
#pragma warning disable CS0619
                ConfigureTarget(tmpRT1);
#pragma warning restore CS0619
            }
#endif

#if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
class BlurPassData
{
    public TextureHandle source;
    public TextureHandle destination;
    public Material blurMaterial;
    public float resScale;
    public int srcWidth;    // source texture dimensions for _BlitTexture_TexelSize
    public int srcHeight;
    public int dstWidth;    // destination dimensions for _BlurTex_TexelSize
    public int dstHeight;
    public float offset;
    public bool setTexelSize;
}

public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    if (!blurMaterial)
        blurMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/KawaseBlur"));
    if (!blurMaterial)
        return;

    var resourceData = frameData.Get<UniversalResourceData>();
    var cameraData   = frameData.Get<UniversalCameraData>();

    var cameraDesc = cameraData.cameraTargetDescriptor;
    cameraDesc.depthBufferBits = 0;
    cameraDesc.msaaSamples = 1;
    cameraDesc.useMipMap = false;
    cameraDesc.autoGenerateMips = false;

    int ds = Mathf.Max(1, settings.blurDownsample);
    rtWidth  = Mathf.Max(1, cameraDesc.width / ds);
    rtHeight = Mathf.Max(1, cameraDesc.height / ds);

    cameraDesc.width  = rtWidth;
    cameraDesc.height = rtHeight;

    RenderingUtils.ReAllocateIfNeeded(ref tmp1, cameraDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LGU_tmpBlurRT1");
    RenderingUtils.ReAllocateIfNeeded(ref tmp2, cameraDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "LGU_tmpBlurRT2");

    var source     = resourceData.activeColorTexture;
    var tmp1Handle = renderGraph.ImportTexture(tmp1);
    var tmp2Handle = renderGraph.ImportTexture(tmp2);

    float resScale = (float)cameraData.cameraTargetDescriptor.height / kRefHeight;
    int passes = Mathf.Max(1, settings.blurPasses);

    int fullWidth = cameraData.cameraTargetDescriptor.width;
    int fullHeight = cameraData.cameraTargetDescriptor.height;

    void AddBlurPass(TextureHandle src, TextureHandle dst, float offset, bool setBg, bool setBlur, bool setTexelSize, int srcW, int srcH)
    {
        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(kTag, out var passData))
        {
            passData.source = src;
            passData.destination = dst;
            passData.blurMaterial = blurMaterial;
            passData.resScale = resScale;
            passData.srcWidth = srcW;
            passData.srcHeight = srcH;
            passData.dstWidth = rtWidth;
            passData.dstHeight = rtHeight;
            passData.offset = offset;
            passData.setTexelSize = setTexelSize;

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.destination, 0, AccessFlags.WriteAll);
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);

            if (setBg)
                builder.SetGlobalTextureAfterPass(passData.source, BGTexID);
            if (setBlur)
                builder.SetGlobalTextureAfterPass(passData.destination, BlurTexID);

            builder.SetRenderFunc((BlurPassData data, RasterGraphContext ctx) =>
            {
                var cmd = ctx.cmd;

                cmd.SetGlobalFloat(_ResScale, data.resScale);
                cmd.SetGlobalFloat(_InvResScale, 1f / Mathf.Max(data.resScale, 1e-5f));
                cmd.SetGlobalFloat(OffsetID, data.offset);

                // Blit.hlsl expects _BlitTexture, _BlitTexture_TexelSize, and _BlitScaleBias
                // TexelSize is based on SOURCE texture dimensions
                cmd.SetGlobalTexture(_BlitTextureID, data.source);
                cmd.SetGlobalVector(_BlitTexture_TexelSizeID, new Vector4(
                    1f / Mathf.Max(data.srcWidth, 1),
                    1f / Mathf.Max(data.srcHeight, 1),
                    data.srcWidth, data.srcHeight
                ));
                cmd.SetGlobalVector(_BlitScaleBiasID, kScaleBias);

                // Draw fullscreen triangle (Blit.hlsl Vert generates UVs procedurally)
                cmd.DrawProcedural(Matrix4x4.identity, data.blurMaterial, 0, MeshTopology.Triangles, 3, 1);

                if (data.setTexelSize)
                {
                    // _BlurTex_TexelSize is based on destination (downsampled) dimensions
                    cmd.SetGlobalVector(BlurTexelSizeID, new Vector4(
                        1f / Mathf.Max(data.dstWidth, 1),
                        1f / Mathf.Max(data.dstHeight, 1),
                        data.dstWidth, data.dstHeight
                    ));
                }
            });
        }
    }

    bool onlyPass = passes == 1;
    // First pass: full-res source → downsampled tmp1
    AddBlurPass(source, tmp1Handle, 1.5f * resScale, true, onlyPass, onlyPass, fullWidth, fullHeight);

    if (!onlyPass)
    {
        var a = tmp1Handle;
        var b = tmp2Handle;
        for (int i = 1; i < passes; i++)
        {
            bool isLast = i == passes - 1;
            // Subsequent passes: downsampled → downsampled
            AddBlurPass(a, b, (0.5f + i) * resScale, false, isLast, isLast, rtWidth, rtHeight);
            var t = a; a = b; b = t;
        }
    }
}
#endif


            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!blurMaterial) return;

                var cmd = CommandBufferPool.Get(kTag);

                int passes = Mathf.Max(1, settings.blurPasses);

                float resScale = (float)renderingData.cameraData.cameraTargetDescriptor.height / kRefHeight;
                cmd.SetGlobalFloat(_ResScale, resScale);
                cmd.SetGlobalFloat(_InvResScale, 1f / Mathf.Max(resScale, 1e-5f));

#if UNITY_2022_1_OR_NEWER
                if (sourceHandle == null || tmp1 == null || tmp2 == null)
                {
                    CommandBufferPool.Release(cmd);
                    return;
                }

                // First downsample + blur (Blitter sets _BlitTexture automatically)
                cmd.SetGlobalFloat(OffsetID, 1.5f * resScale);
                Blitter.BlitCameraTexture(cmd, sourceHandle, tmp1, blurMaterial, 0);

                // Publish original BG (full-res)
                cmd.SetGlobalTexture(BGTexID, sourceHandle);

                // Additional ping-pong passes
                RTHandle a = tmp1;
                RTHandle b = tmp2;

                for (int i = 1; i < passes; i++)
                {
                    cmd.SetGlobalFloat(OffsetID, (0.5f + i) * resScale);
                    Blitter.BlitCameraTexture(cmd, a, b, blurMaterial, 0);

                    var t = a; a = b; b = t;
                }

                // Publish blur
                cmd.SetGlobalTexture(BlurTexID, a);
#else
                // URP 12-14 (Unity 2022/2023): avoid cmd.Blit() with Blit.hlsl shaders
                // (causes keyword space conflicts). Use manual SetRenderTarget + DrawProcedural.

                var srcDesc = renderingData.cameraData.cameraTargetDescriptor;
                cmd.SetGlobalVector(_BlitScaleBiasID, kScaleBias);

                // First downsample + blur (full-res source -> downsampled tmp1)
                cmd.SetGlobalFloat(OffsetID, 1.5f * resScale);
                cmd.SetGlobalTexture(_BlitTextureID, sourceHandle);
                cmd.SetGlobalVector(_BlitTexture_TexelSizeID, new Vector4(
                    1f / Mathf.Max(srcDesc.width, 1),
                    1f / Mathf.Max(srcDesc.height, 1),
                    srcDesc.width, srcDesc.height
                ));
                cmd.SetRenderTarget(tmpRT1);
                cmd.DrawProcedural(Matrix4x4.identity, blurMaterial, 0, MeshTopology.Triangles, 3, 1);

                // Publish original BG (full-res)
                cmd.SetGlobalTexture(BGTexID, sourceHandle);

                // Update texel size for downsampled passes
                cmd.SetGlobalVector(_BlitTexture_TexelSizeID, new Vector4(
                    1f / Mathf.Max(rtWidth, 1),
                    1f / Mathf.Max(rtHeight, 1),
                    rtWidth, rtHeight
                ));

                // Additional ping-pong passes (downsampled -> downsampled)
                for (int i = 1; i < passes; i++)
                {
                    cmd.SetGlobalFloat(OffsetID, (0.5f + i) * resScale);
                    cmd.SetGlobalTexture(_BlitTextureID, tmpRT1);
                    cmd.SetRenderTarget(tmpRT2);
                    cmd.DrawProcedural(Matrix4x4.identity, blurMaterial, 0, MeshTopology.Triangles, 3, 1);

                    var r = tmpRT1; tmpRT1 = tmpRT2; tmpRT2 = r;
                }

                // Publish blur RT
                cmd.SetGlobalTexture(BlurTexID, tmpRT1);
#endif

                cmd.SetGlobalVector(BlurTexelSizeID, new Vector4(
                    1f / Mathf.Max(rtWidth, 1),
                    1f / Mathf.Max(rtHeight, 1),
                    rtWidth, rtHeight
                ));

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

#if !UNITY_2022_1_OR_NEWER
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (cmd == null) return;
                cmd.ReleaseTemporaryRT(tmpId1);
                cmd.ReleaseTemporaryRT(tmpId2);
            }
#endif

            public void Dispose()
            {
#if UNITY_2022_1_OR_NEWER
                tmp1?.Release();
                tmp2?.Release();
                tmp1 = tmp2 = null;
#endif
                CoreUtils.Destroy(blurMaterial);
                blurMaterial = null;
            }
        }

        // --------------------------------------------------------------------

        class GlassSDFPass : ScriptableRenderPass
        {
            const float kRefHeight = 1080f;

            readonly Settings s;
            Material mat;
            readonly HashSet<GlassRegionKey> uniqueRegions = new HashSet<GlassRegionKey>();

            // Existing globals
            static readonly int _LiquidFieldID       = Shader.PropertyToID("_LiquidField");
            static readonly int _ScreenSize          = Shader.PropertyToID("_ScreenSize");         // (W,H,1/W,1/H)
            static readonly int _FieldSize           = Shader.PropertyToID("_FieldSize");          // (W,H,1/W,1/H)
            static readonly int _ScreenRectPx        = Shader.PropertyToID("_ScreenRectPx");       // (xMin,yMin,xMax,yMax)
            static readonly int _RadiusPx            = Shader.PropertyToID("_RadiusPx");
            static readonly int _ScreenToRTScale     = Shader.PropertyToID("_ScreenToRTScale");
            static readonly int _ScreenToFieldScale  = Shader.PropertyToID("_ScreenToFieldScale");
            static readonly int _SMinK_Field         = Shader.PropertyToID("_SMinK_Field");
            static readonly int _SMinK               = Shader.PropertyToID("_SMinK");
            static readonly int _IsBlur              = Shader.PropertyToID("_IsBlur");
            static readonly int _EffectIntensity     = Shader.PropertyToID("_EffectIntensity");

            // Shape globals
            static readonly int _UseShape                 = Shader.PropertyToID("_UseShape");
            static readonly int _ShapeTex                 = Shader.PropertyToID("_ShapeTex");
            static readonly int _ShapeOriginPx            = Shader.PropertyToID("_ShapeOriginPx");
            static readonly int _ShapeInvM                = Shader.PropertyToID("_ShapeInvM");
            static readonly int _ShapeMaxDistTexel        = Shader.PropertyToID("_ShapeMaxDistTexel");
            static readonly int _ShapeScreenPxPerTexelUV  = Shader.PropertyToID("_ShapeScreenPxPerTexelUV");

#if UNITY_2022_1_OR_NEWER
            RTHandle fieldHandle;
#else
            readonly int fieldId = Shader.PropertyToID("_LiquidFieldRT");
            RenderTargetIdentifier fieldRT;
#endif

            public GlassSDFPass(Settings settings) { s = settings; }

#if UNITY_2022_1_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData rd)
            {
                if (!mat) mat = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/SDFWriter"));

                var cameraDesc = rd.cameraData.cameraTargetDescriptor;
                cameraDesc.depthBufferBits = 0;
                cameraDesc.msaaSamples = 1;
                cameraDesc.useMipMap = false;
                cameraDesc.autoGenerateMips = false;
                cameraDesc.colorFormat = RenderTextureFormat.ARGBHalf;

                int ds = Mathf.Max(1, s.fieldDownsample);
                int w = Mathf.Max(1, cameraDesc.width / ds);
                int h = Mathf.Max(1, cameraDesc.height / ds);
                cameraDesc.width = w;
                cameraDesc.height = h;

                RenderingUtils.ReAllocateIfNeeded(ref fieldHandle, cameraDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_LiquidFieldRT");

                ConfigureTarget(fieldHandle);
                ConfigureClear(ClearFlag.Color, new Color(4096f, 0f, 0f, 0f));

                SetupGlobals(cmd, rd.cameraData.cameraTargetDescriptor.width, rd.cameraData.cameraTargetDescriptor.height, w, h);
            }
#else
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTexDesc)
            {
                if (!mat) mat = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/SDFWriter"));

                int ds = Mathf.Max(1, s.fieldDownsample);
                int w = Mathf.Max(1, cameraTexDesc.width / ds);
                int h = Mathf.Max(1, cameraTexDesc.height / ds);

                cmd.GetTemporaryRT(fieldId, w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf);

                fieldRT = new RenderTargetIdentifier(fieldId);
#pragma warning disable CS0619
                ConfigureTarget(fieldRT);
#pragma warning restore CS0619

                ConfigureClear(ClearFlag.Color, new Color(4096f, 0f, 0f, 0f));

                SetupGlobals(cmd, cameraTexDesc.width, cameraTexDesc.height, w, h);
            }
#endif

            void SetupGlobals(CommandBuffer cmd, int screenWInt, int screenHInt, int fieldWInt, int fieldHInt)
            {
                // sizes
                cmd.SetGlobalVector(_ScreenSize, new Vector4(
                    screenWInt, screenHInt,
                    1f / Mathf.Max(screenWInt, 1),
                    1f / Mathf.Max(screenHInt, 1)
                ));

                cmd.SetGlobalVector(_FieldSize, new Vector4(
                    fieldWInt, fieldHInt,
                    1f / Mathf.Max(fieldWInt, 1),
                    1f / Mathf.Max(fieldHInt, 1)
                ));

                float screenW = Mathf.Max(screenWInt, 1);
                float screenH = Mathf.Max(screenHInt, 1);

                // screen px -> field px
                cmd.SetGlobalVector(_ScreenToFieldScale, new Vector4(
                    (float)fieldWInt / screenW,
                    (float)fieldHInt / screenH,
                    0, 0
                ));

                // screen px -> RT px (here RT is the camera target)
                cmd.SetGlobalVector(_ScreenToRTScale, new Vector4(
                    (float)screenWInt / screenW,
                    (float)screenHInt / screenH,
                    0, 0
                ));

                // CanvasScaler-derived scale (from CanvasScalerSync globals)
                Vector4 refResV = Shader.GetGlobalVector("_CanvasRefRes");
                float refW = refResV.x > 0 ? refResV.x : screenW;
                float refH = refResV.y > 0 ? refResV.y : screenH;

                float match = Shader.GetGlobalFloat("_CanvasMatch");
                if (refResV.x <= 0 || refResV.y <= 0) match = 0.5f;

                float logW = Mathf.Log(screenW / Mathf.Max(refW, 1f), 2f);
                float logH = Mathf.Log(screenH / Mathf.Max(refH, 1f), 2f);
                float uiScale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, match));

                float refMin = Mathf.Min(refW, refH);
                float kCanvas = Mathf.Max(s._SMinK, 1e-6f) * (kRefHeight / Mathf.Max(refMin, 1f));

                float kScreen = kCanvas / Mathf.Max(uiScale, 1e-6f);
                float kField = kCanvas * (screenH / (Mathf.Max(uiScale, 1e-6f) * Mathf.Max(fieldHInt, 1)));

                cmd.SetGlobalFloat(_SMinK, kScreen);
                cmd.SetGlobalFloat(_SMinK_Field, kField);
            }

#if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
            void SetupGlobals(RasterCommandBuffer cmd, int screenWInt, int screenHInt, int fieldWInt, int fieldHInt)
            {
                // sizes
                cmd.SetGlobalVector(_ScreenSize, new Vector4(
                    screenWInt, screenHInt,
                    1f / Mathf.Max(screenWInt, 1),
                    1f / Mathf.Max(screenHInt, 1)
                ));

                cmd.SetGlobalVector(_FieldSize, new Vector4(
                    fieldWInt, fieldHInt,
                    1f / Mathf.Max(fieldWInt, 1),
                    1f / Mathf.Max(fieldHInt, 1)
                ));

                float screenW = Mathf.Max(screenWInt, 1);
                float screenH = Mathf.Max(screenHInt, 1);

                // screen px -> field px
                cmd.SetGlobalVector(_ScreenToFieldScale, new Vector4(
                    (float)fieldWInt / screenW,
                    (float)fieldHInt / screenH,
                    0, 0
                ));

                // screen px -> RT px (here RT is the camera target)
                cmd.SetGlobalVector(_ScreenToRTScale, new Vector4(
                    (float)screenWInt / screenW,
                    (float)screenHInt / screenH,
                    0, 0
                ));

                // CanvasScaler-derived scale (from CanvasScalerSync globals)
                Vector4 refResV = Shader.GetGlobalVector("_CanvasRefRes");
                float refW = refResV.x > 0 ? refResV.x : screenW;
                float refH = refResV.y > 0 ? refResV.y : screenH;

                float match = Shader.GetGlobalFloat("_CanvasMatch");
                if (refResV.x <= 0 || refResV.y <= 0) match = 0.5f;

                float logW = Mathf.Log(screenW / Mathf.Max(refW, 1f), 2f);
                float logH = Mathf.Log(screenH / Mathf.Max(refH, 1f), 2f);
                float uiScale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, match));

                float refMin = Mathf.Min(refW, refH);
                float kCanvas = Mathf.Max(s._SMinK, 1e-6f) * (kRefHeight / Mathf.Max(refMin, 1f));

                float kScreen = kCanvas / Mathf.Max(uiScale, 1e-6f);
                float kField = kCanvas * (screenH / (Mathf.Max(uiScale, 1e-6f) * Mathf.Max(fieldHInt, 1)));

                cmd.SetGlobalFloat(_SMinK, kScreen);
                cmd.SetGlobalFloat(_SMinK_Field, kField);
            }
#endif

            bool AddUnique(object region)
            {
                if (!GlassRegionKey.TryCreate(region, out var key))
                    return false;

                return uniqueRegions.Add(key);
            }

            #if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
class SdfPassData
{
    public TextureHandle field;
    public Material mat;
    public int screenW;
    public int screenH;
    public int fieldW;
    public int fieldH;
    public List<GlassRegion> ui;
    public List<GlassRegionWorld> world;
}

public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    if (!mat)
        mat = CoreUtils.CreateEngineMaterial(Shader.Find("LiquidGlass/SDFWriter"));
    if (!mat)
        return;

    var cameraData = frameData.Get<UniversalCameraData>();

    var cameraDesc = cameraData.cameraTargetDescriptor;
    cameraDesc.depthBufferBits = 0;
    cameraDesc.msaaSamples = 1;
    cameraDesc.useMipMap = false;
    cameraDesc.autoGenerateMips = false;
    cameraDesc.colorFormat = RenderTextureFormat.ARGBHalf;

    int ds = Mathf.Max(1, s.fieldDownsample);
    int w = Mathf.Max(1, cameraDesc.width / ds);
    int h = Mathf.Max(1, cameraDesc.height / ds);
    cameraDesc.width = w;
    cameraDesc.height = h;

    RenderingUtils.ReAllocateIfNeeded(ref fieldHandle, cameraDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_LiquidFieldRT");
    var field = renderGraph.ImportTexture(fieldHandle);

    var pass = this;

    using (var builder = renderGraph.AddRasterRenderPass<SdfPassData>("LGU_SDF", out var passData))
    {
        passData.field = field;
        passData.mat = mat;
        passData.screenW = cameraData.cameraTargetDescriptor.width;
        passData.screenH = cameraData.cameraTargetDescriptor.height;
        passData.fieldW = w;
        passData.fieldH = h;
        passData.ui = GlassRegion.Active;
        passData.world = GlassRegionWorld.Active;

        builder.SetRenderAttachment(passData.field, 0, AccessFlags.WriteAll);
        builder.SetGlobalTextureAfterPass(passData.field, _LiquidFieldID);
        builder.AllowGlobalStateModification(true);
        builder.AllowPassCulling(false);

        builder.SetRenderFunc((SdfPassData data, RasterGraphContext ctx) =>
        {
            var cmd = ctx.cmd;

            cmd.ClearRenderTarget(false, true, new Color(4096f, 0f, 0f, 0f));

            pass.SetupGlobals(cmd, data.screenW, data.screenH, data.fieldW, data.fieldH);

            int count = (data.ui?.Count ?? 0) + (data.world?.Count ?? 0);
            if (count == 0)
            {
                cmd.SetGlobalFloat(_UseShape, 0f);
                return;
            }

            // PASS 0: crisp SDF into R
            pass.uniqueRegions.Clear();
            if (data.ui != null)
                for (int i = 0; i < data.ui.Count; i++)
                {
                    var r = data.ui[i];
                    if (!pass.AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, data.mat, 0, MeshTopology.Triangles, 3, 1);
                }

            if (data.world != null)
                for (int i = 0; i < data.world.Count; i++)
                {
                    var r = data.world[i];
                    if (!pass.AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, data.mat, 0, MeshTopology.Triangles, 3, 1);
                }

            // PASS 1: influence into GBA
            pass.uniqueRegions.Clear();
            if (data.ui != null)
                for (int i = 0; i < data.ui.Count; i++)
                {
                    var r = data.ui[i];
                    if (!pass.AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                    cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, data.mat, 1, MeshTopology.Triangles, 3, 1);
                }

            if (data.world != null)
                for (int i = 0; i < data.world.Count; i++)
                {
                    var r = data.world[i];
                    if (!pass.AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                    cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, data.mat, 1, MeshTopology.Triangles, 3, 1);
                }
            cmd.SetGlobalFloat(_UseShape, 0f);
        });
    }
}
#endif


            public override void Execute(ScriptableRenderContext ctx, ref RenderingData rd)
            {
                if (!mat) return;

                var cmd = CommandBufferPool.Get("LGU_SDF");

#if UNITY_2022_1_OR_NEWER
                if (fieldHandle == null)
                {
                    CommandBufferPool.Release(cmd);
                    return;
                }

                cmd.SetRenderTarget(fieldHandle);
#else
                cmd.SetRenderTarget(fieldRT);
#endif

                var ui = GlassRegion.Active;
                var world = GlassRegionWorld.Active;

                int count = (ui?.Count ?? 0) + (world?.Count ?? 0);
                if (count == 0)
                {
#if UNITY_2022_1_OR_NEWER
                    cmd.SetGlobalTexture(_LiquidFieldID, fieldHandle);
#else
                    cmd.SetGlobalTexture(_LiquidFieldID, fieldRT);
#endif
                    ctx.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                    return;
                }

                // PASS 0: crisp SDF into R
                uniqueRegions.Clear();
                if (ui != null)
                    for (int i = 0; i < ui.Count; i++)
                    {
                        var r = ui[i];
                        if (!AddUnique(r)) continue;
                        cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                        cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                        ApplyShape(cmd, r);
                        cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3, 1);
                    }

                if (world != null)
                    for (int i = 0; i < world.Count; i++)
                    {
                        var r = world[i];
                        if (!AddUnique(r)) continue;
                        cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                        cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                        ApplyShape(cmd, r);
                        cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3, 1);
                    }

                // PASS 1: influence into GBA
                uniqueRegions.Clear();
                if (ui != null)
                    for (int i = 0; i < ui.Count; i++)
                    {
                        var r = ui[i];
                        if (!AddUnique(r)) continue;
                        cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                        cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                        cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                        cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                        ApplyShape(cmd, r);
                        cmd.DrawProcedural(Matrix4x4.identity, mat, 1, MeshTopology.Triangles, 3, 1);
                    }

                if (world != null)
                    for (int i = 0; i < world.Count; i++)
                    {
                        var r = world[i];
                        if (!AddUnique(r)) continue;
                        cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                        cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                        cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                        cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                        ApplyShape(cmd, r);
                        cmd.DrawProcedural(Matrix4x4.identity, mat, 1, MeshTopology.Triangles, 3, 1);
                    }

#if UNITY_2022_1_OR_NEWER
                cmd.SetGlobalTexture(_LiquidFieldID, fieldHandle);
#else
                cmd.SetGlobalTexture(_LiquidFieldID, fieldRT);
#endif

                // reset
                cmd.SetGlobalFloat(_UseShape, 0f);

                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            static void ApplyShape(CommandBuffer cmd, object provider)
            {
                if (provider is IGlassShapeProvider sp && sp.UseShape && sp.ShapeSDF)
                {
                    cmd.SetGlobalFloat(_UseShape, 1f);
                    cmd.SetGlobalTexture(_ShapeTex, sp.ShapeSDF);
                    cmd.SetGlobalVector(_ShapeOriginPx, new Vector4(sp.ShapeOriginScreenPx.x, sp.ShapeOriginScreenPx.y, 0, 0));
                    cmd.SetGlobalVector(_ShapeInvM, sp.ShapeInvM);

                    cmd.SetGlobalFloat(_ShapeMaxDistTexel, Mathf.Max(sp.ShapeMaxDistTexel, 1e-3f));

                    cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV,
                        new Vector4(sp.ShapeScreenPxPerTexelUV.x, sp.ShapeScreenPxPerTexelUV.y, 0, 0));
                }
                else
                {
                    cmd.SetGlobalFloat(_UseShape, 0f);
                    cmd.SetGlobalTexture(_ShapeTex, Texture2D.whiteTexture);
                    cmd.SetGlobalVector(_ShapeOriginPx, Vector4.zero);
                    cmd.SetGlobalVector(_ShapeInvM, Vector4.zero);

                    cmd.SetGlobalFloat(_ShapeMaxDistTexel, 1f);
                    cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV, new Vector4(1, 1, 0, 0));
                }
            }

#if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
            static void ApplyShape(RasterCommandBuffer cmd, object provider)
            {
                if (provider is IGlassShapeProvider sp && sp.UseShape && sp.ShapeSDF)
                {
                    cmd.SetGlobalFloat(_UseShape, 1f);
                    Shader.SetGlobalTexture(_ShapeTex, sp.ShapeSDF);
                    cmd.SetGlobalVector(_ShapeOriginPx, new Vector4(sp.ShapeOriginScreenPx.x, sp.ShapeOriginScreenPx.y, 0, 0));
                    cmd.SetGlobalVector(_ShapeInvM, sp.ShapeInvM);

                    cmd.SetGlobalFloat(_ShapeMaxDistTexel, Mathf.Max(sp.ShapeMaxDistTexel, 1e-3f));

                    cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV,
                        new Vector4(sp.ShapeScreenPxPerTexelUV.x, sp.ShapeScreenPxPerTexelUV.y, 0, 0));
                }
                else
                {
                    cmd.SetGlobalFloat(_UseShape, 0f);
                    Shader.SetGlobalTexture(_ShapeTex, Texture2D.whiteTexture);
                    cmd.SetGlobalVector(_ShapeOriginPx, Vector4.zero);
                    cmd.SetGlobalVector(_ShapeInvM, Vector4.zero);

                    cmd.SetGlobalFloat(_ShapeMaxDistTexel, 1f);
                    cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV, new Vector4(1, 1, 0, 0));
                }
            }
#endif

#if !UNITY_2022_1_OR_NEWER
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (cmd == null) return;
                cmd.ReleaseTemporaryRT(fieldId);
            }
#endif

            public void Dispose()
            {
#if UNITY_2022_1_OR_NEWER
                fieldHandle?.Release();
                fieldHandle = null;
#endif
                CoreUtils.Destroy(mat);
                mat = null;
            }
        }
    }
}
#else
using UnityEngine;
using UnityEngine.Rendering;

namespace LGU
{
    internal struct GlassRegionKey : System.IEquatable<GlassRegionKey>
    {
        const float kQuant = 16f;

        readonly int x0, y0, x1, y1;
        readonly int radius;
        readonly int effect;
        readonly bool isBlur;
        readonly bool useShape;

        readonly int shapeId;
        readonly int originX, originY;
        readonly int inv00, inv01, inv10, inv11;
        readonly int maxDist;
        readonly int pxU, pxV;

        static int Q(float v) => Mathf.RoundToInt(v * kQuant);

        public GlassRegionKey(IGlassRegionProvider r, IGlassShapeProvider s)
        {
            x0 = Q(r.ScreenRectPx.x);
            y0 = Q(r.ScreenRectPx.y);
            x1 = Q(r.ScreenRectPx.z);
            y1 = Q(r.ScreenRectPx.w);
            radius = Q(r.RadiusPx);
            effect = Q(r.EffectIntensity);
            isBlur = r.IsBlur;

            useShape = s != null && s.UseShape && s.ShapeSDF;
            if (useShape)
            {
                shapeId = s.ShapeSDF ? s.ShapeSDF.GetInstanceID() : 0;
                originX = Q(s.ShapeOriginScreenPx.x);
                originY = Q(s.ShapeOriginScreenPx.y);
                inv00 = Q(s.ShapeInvM.x);
                inv01 = Q(s.ShapeInvM.y);
                inv10 = Q(s.ShapeInvM.z);
                inv11 = Q(s.ShapeInvM.w);
                maxDist = Q(s.ShapeMaxDistTexel);
                pxU = Q(s.ShapeScreenPxPerTexelUV.x);
                pxV = Q(s.ShapeScreenPxPerTexelUV.y);
            }
            else
            {
                shapeId = 0;
                originX = originY = inv00 = inv01 = inv10 = inv11 = maxDist = pxU = pxV = 0;
            }
        }

        public static bool TryCreate(object src, out GlassRegionKey key)
        {
            if (src is IGlassRegionProvider r)
            {
                key = new GlassRegionKey(r, src as IGlassShapeProvider);
                return true;
            }

            key = default;
            return false;
        }

        public bool Equals(GlassRegionKey other)
        {
            return x0 == other.x0 && y0 == other.y0 &&
                   x1 == other.x1 && y1 == other.y1 &&
                   radius == other.radius &&
                   effect == other.effect &&
                   isBlur == other.isBlur &&
                   useShape == other.useShape &&
                   shapeId == other.shapeId &&
                   originX == other.originX && originY == other.originY &&
                   inv00 == other.inv00 && inv01 == other.inv01 &&
                   inv10 == other.inv10 && inv11 == other.inv11 &&
                   maxDist == other.maxDist &&
                   pxU == other.pxU && pxV == other.pxV;
        }

        public override bool Equals(object obj) => obj is GlassRegionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = x0;
                h = (h * 397) ^ y0;
                h = (h * 397) ^ x1;
                h = (h * 397) ^ y1;
                h = (h * 397) ^ radius;
                h = (h * 397) ^ effect;
                h = (h * 397) ^ (isBlur ? 1 : 0);
                h = (h * 397) ^ (useShape ? 1 : 0);
                h = (h * 397) ^ shapeId;
                h = (h * 397) ^ originX;
                h = (h * 397) ^ originY;
                h = (h * 397) ^ inv00;
                h = (h * 397) ^ inv01;
                h = (h * 397) ^ inv10;
                h = (h * 397) ^ inv11;
                h = (h * 397) ^ maxDist;
                h = (h * 397) ^ pxU;
                h = (h * 397) ^ pxV;
                return h;
            }
        }
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class LiquidGlassBuiltInEffect : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Header("Blur")]
            [Range(0, 20)] public int blurPasses = 2;
            [Range(1, 100)] public int blurDownsample = 4;

            [Header("SDF Field / Mask")]
            [Range(1, 4)] public int fieldDownsample = 1;     // 1 = full-res field; 2..4 downsample
            [Range(0.01f, 0.05f)] public float _SMinK = 0.02f; // base k at ref resolution

            [Header("Perf / Safety")]
            public bool skipSceneViewCamera = true;
            public bool skipPreviewCamera = true;
        }

        public Settings settings = new Settings();

        const float kRefHeight = 1080f;

        // Global IDs (must match shaders)
        static readonly int BGTexID         = Shader.PropertyToID("_BgTexture");
        static readonly int BlurTexID       = Shader.PropertyToID("_BlurTexture");
        static readonly int BlurTexelSizeID = Shader.PropertyToID("_BlurTex_TexelSize");
        static readonly int OffsetID        = Shader.PropertyToID("_Offset");
        static readonly int _ResScale       = Shader.PropertyToID("_ResScale");
        static readonly int _InvResScale    = Shader.PropertyToID("_InvResScale");

        static readonly int _LiquidFieldID  = Shader.PropertyToID("_LiquidField");

        // Field globals
        static readonly int _ScreenSize          = Shader.PropertyToID("_ScreenSize");
        static readonly int _FieldSize           = Shader.PropertyToID("_FieldSize");
        static readonly int _ScreenRectPx        = Shader.PropertyToID("_ScreenRectPx");
        static readonly int _RadiusPx            = Shader.PropertyToID("_RadiusPx");
        static readonly int _ScreenToRTScale     = Shader.PropertyToID("_ScreenToRTScale");
        static readonly int _ScreenToFieldScale  = Shader.PropertyToID("_ScreenToFieldScale");
        static readonly int _SMinK_Field         = Shader.PropertyToID("_SMinK_Field");
        static readonly int _SMinK               = Shader.PropertyToID("_SMinK");
        static readonly int _IsBlur              = Shader.PropertyToID("_IsBlur");
        static readonly int _EffectIntensity     = Shader.PropertyToID("_EffectIntensity");

        // Shape globals (same names already use in SDFWriter)
        static readonly int _UseShape                = Shader.PropertyToID("_UseShape");
        static readonly int _ShapeTex                = Shader.PropertyToID("_ShapeTex");
        static readonly int _ShapeOriginPx           = Shader.PropertyToID("_ShapeOriginPx");
        static readonly int _ShapeInvM               = Shader.PropertyToID("_ShapeInvM");
        static readonly int _ShapeMaxDistTexel       = Shader.PropertyToID("_ShapeMaxDistTexel");
        static readonly int _ShapeScreenPxPerTexelUV = Shader.PropertyToID("_ShapeScreenPxPerTexelUV");

        Camera _cam;

        Material _blurMat;
        Material _sdfMat;

        RenderTexture _bgRT;       // published background (copy of src, mobile-safe)
        RenderTexture _blurRT;     // published blur
        RenderTexture _fieldRT;    // published field
        readonly HashSet<GlassRegionKey> _uniqueRegions = new HashSet<GlassRegionKey>();

        int _lastBgW = -1, _lastBgH = -1;
        int _lastBlurW = -1, _lastBlurH = -1;
        int _lastFieldW = -1, _lastFieldH = -1;

        void OnEnable()
        {
            _cam = GetComponent<Camera>();
            EnsureMaterials();
        }

        void OnDisable()
        {
            ReleaseRTs();
            DestroyMaterials();
        }

        void EnsureMaterials()
        {
            if (_blurMat == null)
            {
                var sh = Shader.Find("LiquidGlass/KawaseBlur");
                if (sh) _blurMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (_sdfMat == null)
            {
                var sh = Shader.Find("LiquidGlass/SDFWriter");
                if (sh) _sdfMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            }
        }

        void DestroyMaterials()
        {
            if (_blurMat) DestroyImmediate(_blurMat);
            if (_sdfMat) DestroyImmediate(_sdfMat);
            _blurMat = null;
            _sdfMat = null;
        }

        void ReleaseRTs()
        {
            if (_bgRT) { _bgRT.Release(); DestroyImmediate(_bgRT); }
            if (_blurRT) { _blurRT.Release(); DestroyImmediate(_blurRT); }
            if (_fieldRT) { _fieldRT.Release(); DestroyImmediate(_fieldRT); }
            _bgRT = null;
            _blurRT = null;
            _fieldRT = null;

            _lastBgW = _lastBgH = -1;
            _lastBlurW = _lastBlurH = -1;
            _lastFieldW = _lastFieldH = -1;
        }

        bool ShouldSkip()
        {
#if UNITY_EDITOR
            if (settings.skipSceneViewCamera && _cam && _cam.cameraType == CameraType.SceneView)
                return true;

            if (settings.skipPreviewCamera && _cam && _cam.cameraType == CameraType.Preview)
                return true;
#endif
            return false;
        }

        RenderTextureFormat PickBlurFormat()
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                return RenderTextureFormat.ARGB32;

            return RenderTextureFormat.Default;
        }

        RenderTextureFormat PickFieldFormat()
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                return RenderTextureFormat.ARGBHalf;

            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                return RenderTextureFormat.ARGBFloat;

            return RenderTextureFormat.ARGB32;
        }

        void EnsureBg(RenderTexture src)
        {
            if (_bgRT == null || _lastBgW != src.width || _lastBgH != src.height)
            {
                if (_bgRT) { _bgRT.Release(); DestroyImmediate(_bgRT); }

                _bgRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    name = "LGU_BgTexture_BI",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _bgRT.Create();
                _lastBgW = src.width;
                _lastBgH = src.height;
            }

            Graphics.Blit(src, _bgRT);
        }

        // Built-in RP hook: called after camera rendered, before presenting
        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (ShouldSkip() || src == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            EnsureMaterials();
            if (_blurMat == null || _sdfMat == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            // Keep output chain intact (so other image effects still work)
            Graphics.Blit(src, dest);

            // Publish BG (full-res) - mobile-safe copy of src
            EnsureBg(src);
            Shader.SetGlobalTexture(BGTexID, _bgRT);

            // Publish resolution scale
            float resScale = (float)src.height / kRefHeight;
            Shader.SetGlobalFloat(_ResScale, resScale);
            Shader.SetGlobalFloat(_InvResScale, 1f / Mathf.Max(resScale, 1e-5f));

            // 1) Blur
            BuildBlur(src, resScale);

            // 2) SDF Field
            BuildField(src);

            // Global textures used by overlay shaders
            if (_blurRT) Shader.SetGlobalTexture(BlurTexID, _blurRT);
            if (_fieldRT) Shader.SetGlobalTexture(_LiquidFieldID, _fieldRT);
        }

        void BuildBlur(RenderTexture src, float resScale)
        {
            int ds = Mathf.Max(1, settings.blurDownsample);
            int bw = Mathf.Max(1, src.width / ds);
            int bh = Mathf.Max(1, src.height / ds);

            // allocate published blur RT (persistent so UI can sample it after OnRenderImage)
            if (_blurRT == null || _lastBlurW != bw || _lastBlurH != bh)
            {
                if (_blurRT) { _blurRT.Release(); DestroyImmediate(_blurRT); }
                _blurRT = new RenderTexture(bw, bh, 0, PickBlurFormat(), RenderTextureReadWrite.Linear)
                {
                    name = "LGU_BlurTexture_BI",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _blurRT.Create();
                _lastBlurW = bw;
                _lastBlurH = bh;
            }

            // temp ping-pong
            RenderTexture tmp1 = RenderTexture.GetTemporary(bw, bh, 0, PickBlurFormat(), RenderTextureReadWrite.Linear);
            RenderTexture tmp2 = RenderTexture.GetTemporary(bw, bh, 0, PickBlurFormat(), RenderTextureReadWrite.Linear);
            tmp1.filterMode = FilterMode.Bilinear;
            tmp2.filterMode = FilterMode.Bilinear;

            int passes = Mathf.Max(1, settings.blurPasses);

            // first downsample + blur
            _blurMat.SetFloat(OffsetID, 1.5f * resScale);
            Graphics.Blit(src, tmp1, _blurMat);

            // extra passes
            for (int i = 1; i < passes; i++)
            {
                _blurMat.SetFloat(OffsetID, (0.5f + i) * resScale);
                Graphics.Blit(tmp1, tmp2, _blurMat);

                var r = tmp1; tmp1 = tmp2; tmp2 = r;
            }

            // copy to published RT
            Graphics.Blit(tmp1, _blurRT);

            Shader.SetGlobalVector(BlurTexelSizeID, new Vector4(
                1f / Mathf.Max(bw, 1),
                1f / Mathf.Max(bh, 1),
                bw, bh
            ));

            RenderTexture.ReleaseTemporary(tmp1);
            RenderTexture.ReleaseTemporary(tmp2);
        }

        bool AddUnique(object region)
        {
            if (!GlassRegionKey.TryCreate(region, out var key))
                return false;

            return _uniqueRegions.Add(key);
        }

        void BuildField(RenderTexture src)
        {
            int ds = Mathf.Max(1, settings.fieldDownsample);
            int fw = Mathf.Max(1, src.width / ds);
            int fh = Mathf.Max(1, src.height / ds);

            if (_fieldRT == null || _lastFieldW != fw || _lastFieldH != fh)
            {
                if (_fieldRT) { _fieldRT.Release(); DestroyImmediate(_fieldRT); }
                _fieldRT = new RenderTexture(fw, fh, 0, PickFieldFormat(), RenderTextureReadWrite.Linear)
                {
                    name = "LGU_LiquidField_BI",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _fieldRT.Create();
                _lastFieldW = fw;
                _lastFieldH = fh;
            }

            // sizes (match URP Configure)
            Shader.SetGlobalVector(_ScreenSize, new Vector4(
                src.width, src.height,
                1f / Mathf.Max(src.width, 1),
                1f / Mathf.Max(src.height, 1)
            ));

            Shader.SetGlobalVector(_FieldSize, new Vector4(
                fw, fh,
                1f / Mathf.Max(fw, 1),
                1f / Mathf.Max(fh, 1)
            ));

            // IMPORTANT: use camera target size, not Screen.* (mobile/editor-safe)
            float screenW = Mathf.Max(src.width, 1);
            float screenH = Mathf.Max(src.height, 1);

            Shader.SetGlobalVector(_ScreenToFieldScale, new Vector4(
                (float)fw / screenW,
                (float)fh / screenH,
                0, 0
            ));

            Shader.SetGlobalVector(_ScreenToRTScale, new Vector4(
                (float)src.width / screenW,
                (float)src.height / screenH,
                0, 0
            ));

            // CanvasScaler-derived scale (from CanvasScalerSync globals)
            Vector4 refResV = Shader.GetGlobalVector("_CanvasRefRes");
            float refW = refResV.x > 0 ? refResV.x : screenW;
            float refH = refResV.y > 0 ? refResV.y : screenH;

            float match = Shader.GetGlobalFloat("_CanvasMatch");
            if (refResV.x <= 0 || refResV.y <= 0) match = 0.5f;

            float logW = Mathf.Log(screenW / Mathf.Max(refW, 1f), 2f);
            float logH = Mathf.Log(screenH / Mathf.Max(refH, 1f), 2f);
            float uiScale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, match));

            float refMin = Mathf.Min(refW, refH);
            float kCanvas = Mathf.Max(settings._SMinK, 1e-6f) * (kRefHeight / Mathf.Max(refMin, 1f));

            float kScreen = kCanvas / Mathf.Max(uiScale, 1e-6f);
            float kField = kCanvas * (screenH / (Mathf.Max(uiScale, 1e-6f) * Mathf.Max(fh, 1)));

            Shader.SetGlobalFloat(_SMinK, kScreen);
            Shader.SetGlobalFloat(_SMinK_Field, kField);

            // Clear field
            var prev = RenderTexture.active;
            RenderTexture.active = _fieldRT;
            GL.Clear(false, true, new Color(4096f, 0f, 0f, 0f));
            RenderTexture.active = prev;

            var ui = GlassRegion.Active;
            var world = GlassRegionWorld.Active;

            int count = (ui?.Count ?? 0) + (world?.Count ?? 0);
            if (count == 0)
            {
                // still publish empty field
                Shader.SetGlobalFloat(_UseShape, 0f);
                return;
            }

            // Draw passes using a command buffer (efficient & matches URP style)
            var cmd = new CommandBuffer { name = "LGU_SDF_BuiltIn" };
            cmd.SetRenderTarget(_fieldRT);

            // PASS 0: crisp SDF -> R (min union)
            _uniqueRegions.Clear();
            if (ui != null)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    var r = ui[i];
                    if (!AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, _sdfMat, 0, MeshTopology.Triangles, 3, 1);
                }
            }

            if (world != null)
            {
                for (int i = 0; i < world.Count; i++)
                {
                    var r = world[i];
                    if (!AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, _sdfMat, 0, MeshTopology.Triangles, 3, 1);
                }
            }

            // PASS 1: influence -> GBA (add)
            _uniqueRegions.Clear();
            if (ui != null)
            {
                for (int i = 0; i < ui.Count; i++)
                {
                    var r = ui[i];
                    if (!AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                    cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, _sdfMat, 1, MeshTopology.Triangles, 3, 1);
                }
            }

            if (world != null)
            {
                for (int i = 0; i < world.Count; i++)
                {
                    var r = world[i];
                    if (!AddUnique(r)) continue;
                    cmd.SetGlobalVector(_ScreenRectPx, r.ScreenRectPx);
                    cmd.SetGlobalFloat(_RadiusPx, r.RadiusPx);
                    cmd.SetGlobalFloat(_IsBlur, r.IsBlur ? 1f : 0f);
                    cmd.SetGlobalFloat(_EffectIntensity, r.EffectIntensity);
                    ApplyShape(cmd, r);
                    cmd.DrawProcedural(Matrix4x4.identity, _sdfMat, 1, MeshTopology.Triangles, 3, 1);
                }
            }

            // reset shape flag for safety
            cmd.SetGlobalFloat(_UseShape, 0f);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        static void ApplyShape(CommandBuffer cmd, object provider)
        {
            if (provider is IGlassShapeProvider sp && sp.UseShape && sp.ShapeSDF)
            {
                cmd.SetGlobalFloat(_UseShape, 1f);
                cmd.SetGlobalTexture(_ShapeTex, sp.ShapeSDF);
                cmd.SetGlobalVector(_ShapeOriginPx, new Vector4(sp.ShapeOriginScreenPx.x, sp.ShapeOriginScreenPx.y, 0, 0));
                cmd.SetGlobalVector(_ShapeInvM, sp.ShapeInvM);

                cmd.SetGlobalFloat(_ShapeMaxDistTexel, Mathf.Max(sp.ShapeMaxDistTexel, 1e-3f));
                cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV,
                    new Vector4(sp.ShapeScreenPxPerTexelUV.x, sp.ShapeScreenPxPerTexelUV.y, 0, 0));
            }
            else
            {
                cmd.SetGlobalFloat(_UseShape, 0f);
                cmd.SetGlobalTexture(_ShapeTex, Texture2D.whiteTexture);
                cmd.SetGlobalVector(_ShapeOriginPx, Vector4.zero);
                cmd.SetGlobalVector(_ShapeInvM, Vector4.zero);
                cmd.SetGlobalFloat(_ShapeMaxDistTexel, 1f);
                cmd.SetGlobalVector(_ShapeScreenPxPerTexelUV, new Vector4(1, 1, 0, 0));
            }
        }
    }
}
#endif
