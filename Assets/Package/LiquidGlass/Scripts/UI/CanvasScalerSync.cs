using UnityEngine;
using UnityEngine.UI; // CanvasScaler

namespace LGU
{
    [ExecuteAlways]
    public class CanvasScalerSync : MonoBehaviour
    {
        static readonly int _CanvasRefResID = Shader.PropertyToID("_CanvasRefRes");
        static readonly int _CanvasMatchID = Shader.PropertyToID("_CanvasMatch");

        CanvasScaler cs;

        void OnEnable()
        {
            cs = GetComponent<CanvasScaler>();
            UpdateGlobals();
        }

        public void UpdateGlobals()
        {
            if (!cs) return;
            // Shader.SetGlobalFloat(_CanvasScaleID,  Mathf.Max(cs.scaleFactor, 1e-6f));
            Shader.SetGlobalVector(_CanvasRefResID, new Vector4(cs.referenceResolution.x, cs.referenceResolution.y, 0, 0));
            Shader.SetGlobalFloat(_CanvasMatchID, Mathf.Clamp01(cs.matchWidthOrHeight));
        }
    }
}
