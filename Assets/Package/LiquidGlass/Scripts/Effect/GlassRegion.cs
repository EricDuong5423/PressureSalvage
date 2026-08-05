using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LGU
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class GlassRegion : MonoBehaviour, IGlassRegionProvider, IGlassShapeProvider
    {
        [Range(0f, 1f)] public float cornerRadius = 0.08f;
        [Range(0f, 5f)] public float effectIntensity = 1f;
        public bool isBlur = false;

        [Header("Optional Sprite Shape (UI Image)\nSprite texture's Read/Write must be enabled")]
        public bool useSpriteShape = false;

        public static readonly List<GlassRegion> Active = new();
        static readonly Vector3[] _corners = new Vector3[4];

        RectTransform rt;
        Image img;
        Canvas canvas;
        Camera uiCam;
        bool added;

        [HideInInspector] public Vector4 screenRectPx;
        [HideInInspector] public float radiusPx;

        // shape data
        Texture shapeSdf;
        float shapeMaxDistTexel;
        Vector2 shapeOriginScreenPx;
        Vector4 shapeInvM;
        Vector2 shapeScreenPxPerTexelUV;
        bool useShapeRuntime;

        public Vector4 ScreenRectPx => screenRectPx;
        public float RadiusPx => radiusPx;
        public float EffectIntensity => effectIntensity * 5f;
        public bool IsBlur => isBlur;

        public bool UseShape => useShapeRuntime;
        public Texture ShapeSDF => shapeSdf;
        public Vector2 ShapeOriginScreenPx => shapeOriginScreenPx;
        public Vector4 ShapeInvM => shapeInvM;
        public float ShapeMaxDistTexel => shapeMaxDistTexel;
        public Vector2 ShapeScreenPxPerTexelUV => shapeScreenPxPerTexelUV;

        CanvasScalerSync canvasScalerSync;

        // Tier-2 dirty
        bool _dirty = true;
        public bool IsDirty => _dirty;

        // lightweight transform/property change detection
        Vector2 _lastSize;
        Vector3 _lastPos;
        Quaternion _lastRot;
        float _lastCornerRadius;
        float _lastEffectIntensity;
        bool _lastUseSpriteShape;
        Sprite _lastSprite;
        bool _lastIsBlur;
        
        private Color _gizmoColor = new Color(0f, 0.8f, 1f, 0.9f);

        void OnEnable()
        {
            rt = GetComponent<RectTransform>();
            img = GetComponent<Image>();
            canvas = GetComponentInParent<Canvas>();
            EnsureScalerSync(canvas);

            TryAdd();
            MarkDirty();
        }

        void OnDisable()
        {
            if (added) Active.Remove(this);
            added = false;
        }

        void Reset() => MarkDirty();
        void OnValidate() => MarkDirty();
        void OnTransformParentChanged() => MarkDirty();
        void OnRectTransformDimensionsChange() => MarkDirty();

        void LateUpdate()
        {
            TryAdd();
            MaybeMarkDirtyFromChanges();
        }

        void TryAdd()
        {
            if (!added)
            {
                Active.Add(this);
                added = true;
            }
        }

        void MaybeMarkDirtyFromChanges()
        {
            if (!rt) return;

            var size = rt.rect.size;
            var pos = rt.position;
            var rot = rt.rotation;
            var sprite = img ? img.sprite : null;

            if (size != _lastSize ||
                pos != _lastPos ||
                rot != _lastRot ||
                !Mathf.Approximately(_lastCornerRadius, cornerRadius) ||
                !Mathf.Approximately(_lastEffectIntensity, effectIntensity) ||
                _lastUseSpriteShape != useSpriteShape ||
                sprite != _lastSprite ||
                _lastIsBlur != isBlur)
            {
                _lastSize = size;
                _lastPos = pos;
                _lastRot = rot;
                _lastCornerRadius = cornerRadius;
                _lastEffectIntensity = effectIntensity;
                _lastUseSpriteShape = useSpriteShape;
                _lastSprite = sprite;
                _lastIsBlur = isBlur;

                MarkDirty();
            }
        }

        public void MarkDirty()
        {
            _dirty = true;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
#endif
        }

        // -------- Tier-2 entry points --------

        public static void UpdateAllForRenderCamera(Camera renderCam, bool updateAll)
        {
            // prune nulls (Edit mode safety)
            for (int i = Active.Count - 1; i >= 0; i--)
                if (!Active[i]) Active.RemoveAt(i);

            for (int i = 0; i < Active.Count; i++)
            {
                var r = Active[i];
                if (!r) continue;

                if (updateAll || r._dirty)
                    r.ForceUpdateForRender(renderCam);
            }
        }

        void ForceUpdateForRender(Camera renderCam)
        {
            // IMPORTANT: consume dirty here, not elsewhere
            _dirty = false;

            if (!rt) rt = GetComponent<RectTransform>();
            if (!rt) return;

            if (!canvas) canvas = GetComponentInParent<Canvas>();
            EnsureScalerSync(canvas);

            // Decide UI camera per canvas mode
            if (canvas)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) uiCam = null;
                else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    uiCam = canvas.worldCamera ? canvas.worldCamera : renderCam;
                else
                    uiCam = renderCam; // WorldSpace canvas fallback
            }
            else uiCam = renderCam;

            if (canvasScalerSync) canvasScalerSync.UpdateGlobals();

            rt.GetWorldCorners(_corners);

            Vector2 bl = RectTransformUtility.WorldToScreenPoint(uiCam, _corners[0]);
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(uiCam, _corners[1]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(uiCam, _corners[2]);
            Vector2 br = RectTransformUtility.WorldToScreenPoint(uiCam, _corners[3]);

            float xMin = Mathf.Min(bl.x, tl.x, tr.x, br.x);
            float yMin = Mathf.Min(bl.y, tl.y, tr.y, br.y);
            float xMax = Mathf.Max(bl.x, tl.x, tr.x, br.x);
            float yMax = Mathf.Max(bl.y, tl.y, tr.y, br.y);

            float widthPx = Mathf.Abs(xMax - xMin);
            float heightPx = Mathf.Abs(yMax - yMin);
            float minAxis = Mathf.Min(widthPx, heightPx);

            screenRectPx = new Vector4(xMin, yMin, xMax, yMax);
            radiusPx = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis;

            UpdateShape(bl, br, tl);
        }

        void UpdateShape(Vector2 bl, Vector2 br, Vector2 tl)
        {
            useShapeRuntime = false;
            shapeSdf = null;
            shapeMaxDistTexel = 0;
            shapeOriginScreenPx = default;
            shapeInvM = default;
            shapeScreenPxPerTexelUV = default;

            if (!useSpriteShape) return;
            if (!img) img = GetComponent<Image>();
            if (!img || !img.sprite) return;
            if (img.type != Image.Type.Simple) return;

            var sprite = img.sprite;
            if (!SpriteSDFCache.TryGet(sprite, out var sdf)) return;

            Vector2 axisU = br - bl;
            Vector2 axisV = tl - bl;

            float det = axisU.x * axisV.y - axisU.y * axisV.x;
            if (Mathf.Abs(det) < 1e-6f) return;

            float invDet = 1f / det;
            float inv00 = axisV.y * invDet;
            float inv01 = -axisV.x * invDet;
            float inv10 = -axisU.y * invDet;
            float inv11 = axisU.x * invDet;

            float pxPerTexU = axisU.magnitude / Mathf.Max(1, sdf.width);
            float pxPerTexV = axisV.magnitude / Mathf.Max(1, sdf.height);

            shapeOriginScreenPx = bl;
            shapeInvM = new Vector4(inv00, inv01, inv10, inv11);
            shapeSdf = sdf.tex;
            shapeMaxDistTexel = sdf.maxDistTexel;
            shapeScreenPxPerTexelUV = new Vector2(pxPerTexU, pxPerTexV);

            useShapeRuntime = true;
        }

        void EnsureScalerSync(Canvas c)
        {
            if (!c || canvasScalerSync) return;

            var root = c.rootCanvas;
            if (!root) return;

            var scaler = root.GetComponent<CanvasScaler>();
            if (!scaler) return;

            var sync = root.GetComponent<CanvasScalerSync>();
            if (!sync) sync = root.gameObject.AddComponent<CanvasScalerSync>();
            canvasScalerSync = sync;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }

        void OnDrawGizmosSelected()
        {
            if (!rt) rt = GetComponent<RectTransform>();
            if (!rt) return;

            rt.GetWorldCorners(_corners);

            // Convert normalized cornerRadius (0..1) to world-space radius
            float width = Vector3.Distance(_corners[0], _corners[3]);
            float height = Vector3.Distance(_corners[0], _corners[1]);
            float minAxis = Mathf.Min(width, height);

            float r = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis;
            if (r <= 0.001f)
            {
                // Fallback to sharp rect
                Handles.color = _gizmoColor;
                Handles.DrawAAPolyLine(
                    2.5f,
                    _corners[0], _corners[1], _corners[2], _corners[3], _corners[0]
                );
                return;
            }

            Handles.color = _gizmoColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            Utils.DrawRoundedRectWorld(_corners, r, 12);
        }
#endif
    }
}
