using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace LGU
{
    /// <summary>
    /// Tier-2 safe optimization:
    /// - If render camera changes (transform/projection/viewport/resolution), update ALL regions.
    /// - Else update only DIRTY regions.
    /// Fixes "snap back" and avoids per-frame work when camera is stable.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-32000)]
    public class GlassRegionRenderSync : MonoBehaviour
    {
        [Header("Filter")]
        public bool updateGameCameras = true;
        public bool updateSceneViewCameras = false;
        public bool updatePreviewCameras = false;

        [Tooltip("If set, only updates when this camera renders.")]
        public Camera onlyThisCamera;

        // Per-camera state
        struct CamState
        {
            public Matrix4x4 worldToCamera;
            public Matrix4x4 projection;
            public Rect viewportRect;
            public int pixelW;
            public int pixelH;
            public float orthoSize;
            public float fov;
            public float nearClip;
            public float farClip;
            public bool orthographic;
        }

        readonly Dictionary<int, CamState> _states = new();

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

#if UNITY_EDITOR
            // In edit mode, if you drag stuff without rendering, this nudges updates.
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _states.Clear();
        }

        void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (!cam) return;
            if (onlyThisCamera && cam != onlyThisCamera) return;
            if (!IsAllowed(cam)) return;

            bool cameraChanged = HasCameraChanged(cam);

            // cameraChanged => update all
            // else => update only dirty
            GlassRegionWorld.UpdateAllForCamera(cam, cameraChanged);
            GlassRegion.UpdateAllForRenderCamera(cam, cameraChanged);
        }

        bool IsAllowed(Camera cam)
        {
#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView) return updateSceneViewCameras;
            if (cam.cameraType == CameraType.Preview) return updatePreviewCameras;
#endif
            return updateGameCameras;
        }

        bool HasCameraChanged(Camera cam)
        {
            int id = cam.GetInstanceID();

            CamState now = new CamState
            {
                worldToCamera = cam.worldToCameraMatrix,
                projection = cam.projectionMatrix,
                viewportRect = cam.rect,
                pixelW = cam.pixelWidth,
                pixelH = cam.pixelHeight,
                orthoSize = cam.orthographicSize,
                fov = cam.fieldOfView,
                nearClip = cam.nearClipPlane,
                farClip = cam.farClipPlane,
                orthographic = cam.orthographic
            };

            if (!_states.TryGetValue(id, out var prev))
            {
                _states[id] = now;
                return true; // first time for this camera, treat as changed
            }

            bool changed =
                !MatrixApproximately(prev.worldToCamera, now.worldToCamera) ||
                !MatrixApproximately(prev.projection, now.projection) ||
                prev.viewportRect != now.viewportRect ||
                prev.pixelW != now.pixelW ||
                prev.pixelH != now.pixelH ||
                !Mathf.Approximately(prev.orthoSize, now.orthoSize) ||
                !Mathf.Approximately(prev.fov, now.fov) ||
                !Mathf.Approximately(prev.nearClip, now.nearClip) ||
                !Mathf.Approximately(prev.farClip, now.farClip) ||
                prev.orthographic != now.orthographic;

            if (changed)
                _states[id] = now;

            return changed;
        }

        static bool MatrixApproximately(Matrix4x4 a, Matrix4x4 b)
        {
            // Very small tolerance to avoid floating spam.
            // We compare all 16 elements.
            const float eps = 1e-6f;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    float da = a[r, c];
                    float db = b[r, c];
                    if (Mathf.Abs(da - db) > eps)
                        return false;
                }
            }
            return true;
        }
    }
}
