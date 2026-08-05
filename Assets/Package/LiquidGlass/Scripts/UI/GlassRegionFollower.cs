using UnityEngine;
using UnityEngine.UI; // CanvasScaler
using System.Collections.Generic;

namespace LGU
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class GlassRegionFollower : MonoBehaviour
    {
        [SerializeField] public RectTransform target;
        private RectTransform rectTransform;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

#if UNITY_EDITOR
        void Update()
#else
        void FixedUpdate()
#endif
        {
            if(!target || target == null)
                return;
                
            rectTransform.pivot = target.pivot;
            rectTransform.anchorMax = target.anchorMax;
            rectTransform.anchorMin = target.anchorMin;
            rectTransform.sizeDelta = target.sizeDelta;
            rectTransform.anchoredPosition = target.anchoredPosition;
        }
    }
}