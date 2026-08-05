using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LGU
{
    [ExecuteAlways]
    public class GlassRegionWorld : MonoBehaviour, IGlassRegionProvider, IGlassShapeProvider
    {
        [Range(0f, 1f)] public float cornerRadius = 0.08f;
        [Range(0f, 5f)] public float effectIntensity = 1f;
        public bool isBlur = true;

        [Header("Optional Sprite Shape (SpriteRenderer)\nSprite texture's Read/Write must be enabled")]
        public bool useSpriteShape = false;

        public static readonly List<GlassRegionWorld> Active = new();

        [HideInInspector] public Vector4 screenRectPx;
        [HideInInspector] public float radiusPx;

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

        bool added;

        Collider2D col;
        Renderer rend;
        SpriteRenderer sr;

        bool _dirty = true;
        public bool IsDirty => _dirty;

        Vector3 _lastPos;
        Quaternion _lastRot;
        Vector3 _lastScale;
        float _lastCornerRadius;
        float _lastEffectIntensity;
        bool _lastUseSpriteShape;
        bool _lastIsBlur;
        Sprite _lastSprite;
        
        private Color _gizmoColor = new Color(0.2f, 1f, 0.6f, 0.9f);
        static readonly Vector3[] _gizmoCorners = new Vector3[4];

        void OnEnable()
        {
            col = GetComponent<Collider2D>();
            rend = GetComponent<Renderer>();
            sr = GetComponent<SpriteRenderer>();

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
        void OnTransformChildrenChanged() => MarkDirty();

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
            var t = transform;
            var sprite = sr ? sr.sprite : null;

            if (t.position != _lastPos ||
                t.rotation != _lastRot ||
                t.lossyScale != _lastScale ||
                !Mathf.Approximately(_lastCornerRadius, cornerRadius) ||
                !Mathf.Approximately(_lastEffectIntensity, effectIntensity) ||
                _lastUseSpriteShape != useSpriteShape ||
                _lastIsBlur != isBlur ||
                sprite != _lastSprite)
            {
                _lastPos = t.position;
                _lastRot = t.rotation;
                _lastScale = t.lossyScale;
                _lastCornerRadius = cornerRadius;
                _lastEffectIntensity = effectIntensity;
                _lastUseSpriteShape = useSpriteShape;
                _lastIsBlur = isBlur;
                _lastSprite = sprite;

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

        // Tier-2 entry point
        public static void UpdateAllForCamera(Camera cam, bool updateAll)
        {
            if (!cam) return;

            for (int i = Active.Count - 1; i >= 0; i--)
                if (!Active[i]) Active.RemoveAt(i);

            for (int i = 0; i < Active.Count; i++)
            {
                var r = Active[i];
                if (!r) continue;

                if (updateAll || r._dirty)
                    r.ForceUpdateWithCamera(cam);
            }
        }

        void ForceUpdateWithCamera(Camera camToUse)
        {
            _dirty = false;

            if (!camToUse) return;

            useShapeRuntime = false;
            shapeSdf = null;
            shapeMaxDistTexel = 0;
            shapeOriginScreenPx = default;
            shapeInvM = default;
            shapeScreenPxPerTexelUV = default;

            // sprite-shape path
            if (useSpriteShape && sr && sr.sprite)
            {
                if (SpriteSDFCache.TryGet(sr.sprite, out var sdf) &&
                    ComputeSpriteScreenMappingWithCamera(camToUse, sr.sprite, sdf.width, sdf.height,
                        out var origin, out var invM, out var rect, out var pxPerTexUV))
                {
                    shapeSdf = sdf.tex;
                    shapeMaxDistTexel = sdf.maxDistTexel;
                    shapeOriginScreenPx = origin;
                    shapeInvM = invM;
                    shapeScreenPxPerTexelUV = pxPerTexUV;
                    useShapeRuntime = true;

                    screenRectPx = rect;
                    float minAxis = Mathf.Min(rect.z - rect.x, rect.w - rect.y);
                    radiusPx = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis;
                    return;
                }
            }

            // fallback bounds
            Bounds b;
            if (col) b = col.bounds;
            else if (rend) b = rend.bounds;
            else return;

            Vector3 min = b.min;
            Vector3 max = b.max;
            float z = transform.position.z;

            Vector3 p0 = camToUse.WorldToScreenPoint(new Vector3(min.x, min.y, z));
            Vector3 p1 = camToUse.WorldToScreenPoint(new Vector3(max.x, min.y, z));
            Vector3 p2 = camToUse.WorldToScreenPoint(new Vector3(max.x, max.y, z));
            Vector3 p3 = camToUse.WorldToScreenPoint(new Vector3(min.x, max.y, z));

            float xMin = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
            float yMin = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
            float xMax = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
            float yMax = Mathf.Max(p0.y, p1.y, p2.y, p3.y);

            float widthPx = Mathf.Abs(xMax - xMin);
            float heightPx = Mathf.Abs(yMax - yMin);
            float minAxis2 = Mathf.Min(widthPx, heightPx);

            screenRectPx = new Vector4(xMin, yMin, xMax, yMax);
            radiusPx = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis2;
        }

        bool ComputeSpriteScreenMappingWithCamera(
            Camera camToUse,
            Sprite sprite,
            int sdfW,
            int sdfH,
            out Vector2 originPx,
            out Vector4 invM,
            out Vector4 rect,
            out Vector2 pxPerTexUV)
        {
            originPx = default;
            invM = default;
            rect = default;
            pxPerTexUV = default;

            float ppu = sprite.pixelsPerUnit;
            Rect spriteRect = sprite.rect;

            Vector2 sizeUnits = new Vector2(spriteRect.width / ppu, spriteRect.height / ppu);
            Vector2 pivotUnits = sprite.pivot / ppu;

            Vector3 blL = new Vector3(-pivotUnits.x, -pivotUnits.y, 0);
            Vector3 brL = new Vector3(sizeUnits.x - pivotUnits.x, -pivotUnits.y, 0);
            Vector3 tlL = new Vector3(-pivotUnits.x, sizeUnits.y - pivotUnits.y, 0);
            Vector3 trL = new Vector3(sizeUnits.x - pivotUnits.x, sizeUnits.y - pivotUnits.y, 0);

            Vector2 bl = camToUse.WorldToScreenPoint(transform.TransformPoint(blL));
            Vector2 br = camToUse.WorldToScreenPoint(transform.TransformPoint(brL));
            Vector2 tl = camToUse.WorldToScreenPoint(transform.TransformPoint(tlL));
            Vector2 tr = camToUse.WorldToScreenPoint(transform.TransformPoint(trL));

            float xMin = Mathf.Min(bl.x, br.x, tl.x, tr.x);
            float yMin = Mathf.Min(bl.y, br.y, tl.y, tr.y);
            float xMax = Mathf.Max(bl.x, br.x, tl.x, tr.x);
            float yMax = Mathf.Max(bl.y, br.y, tl.y, tr.y);
            rect = new Vector4(xMin, yMin, xMax, yMax);

            Vector2 axisU = br - bl;
            Vector2 axisV = tl - bl;

            float det = axisU.x * axisV.y - axisU.y * axisV.x;
            if (Mathf.Abs(det) < 1e-6f) return false;

            float invDet = 1f / det;
            float inv00 = axisV.y * invDet;
            float inv01 = -axisV.x * invDet;
            float inv10 = -axisU.y * invDet;
            float inv11 = axisU.x * invDet;

            float pxPerTexU = axisU.magnitude / Mathf.Max(1, sdfW);
            float pxPerTexV = axisV.magnitude / Mathf.Max(1, sdfH);
            pxPerTexUV = new Vector2(pxPerTexU, pxPerTexV);

            originPx = bl;
            invM = new Vector4(inv00, inv01, inv10, inv11);
            return true;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
        }

        void OnDrawGizmosSelected()
        {
            Bounds b;
            if (col) b = col.bounds;
            else if (rend) b = rend.bounds;
            else
            {
                col = GetComponent<Collider2D>();
                rend = GetComponent<Renderer>();
                if (col) b = col.bounds;
                else if (rend) b = rend.bounds;
                else return;
            }

            Vector3 min = b.min;
            Vector3 max = b.max;
            float z = transform.position.z;

            _gizmoCorners[0] = new Vector3(min.x, min.y, z);
            _gizmoCorners[1] = new Vector3(min.x, max.y, z);
            _gizmoCorners[2] = new Vector3(max.x, max.y, z);
            _gizmoCorners[3] = new Vector3(max.x, min.y, z);

            float width = Vector3.Distance(_gizmoCorners[0], _gizmoCorners[3]);
            float height = Vector3.Distance(_gizmoCorners[0], _gizmoCorners[1]);
            float minAxis = Mathf.Min(width, height);

            float r = Mathf.Clamp01(cornerRadius) * 0.5f * minAxis;
            if (r <= 0.001f)
            {
                Handles.color = _gizmoColor;
                Handles.DrawAAPolyLine(
                    2.5f,
                    _gizmoCorners[0], _gizmoCorners[1], _gizmoCorners[2], _gizmoCorners[3], _gizmoCorners[0]
                );
                return;
            }

            Handles.color = _gizmoColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Utils.DrawRoundedRectWorld(_gizmoCorners, r, 12);
        }
#endif
    }
}
