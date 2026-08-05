using System.Collections;
using UnityEngine;

namespace LGU
{
    public class SmoothFollowMouse : MonoBehaviour
    {
        [Header("General")]
        private Canvas canvas;
        public float followSpeed = 10f; // 0 = instant, >0 = smooth

        [Header("target")]
        public RectTransform target; // The single object that follows mouse on PC

        private Vector2 targetPos;

        void Awake()
        {
            if (target != null)
            {
                canvas = target.GetComponentInParent<Canvas>();
                targetPos = target.localPosition;   // use localPosition consistently
            }
        }

        public void ChangeTarget(RectTransform target)
        {
            if (Application.isMobilePlatform)
                return;

            StopAllCoroutines();
            StartCoroutine(ChangeFollowerRoutine(target));
        }

        private IEnumerator ChangeFollowerRoutine(RectTransform newTarget)
        {
            if (target != null)
            {
                Vector2 startPos = target.localPosition;
                Vector2 endPos = targetPos;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * followSpeed;
                    target.localPosition = Vector2.Lerp(startPos, endPos, t);
                    yield return null;
                }
                target.localPosition = endPos;
            }

            target = newTarget;
            if (target != null)
                targetPos = target.localPosition;
        }

        void FixedUpdate()
        {
            if (Application.isMobilePlatform)
                HandleMobileMultiTouch();
            else
                HandlePCMouseFollow();
        }

        // ---------- PC: FOLLOW MOUSE, CENTERED ----------
        private void HandlePCMouseFollow()
        {
            if (target == null || canvas == null) return;

            Vector2 localPoint;
            RectTransform parentRect = target.parent as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );
            // localPoint.y = Mathf.Max(localPoint.y, -parentRect.rect.height / 2 + target.rect.height * 0.5f + 260f);

            if (followSpeed <= 0f)
                target.localPosition = localPoint;
            else
                target.localPosition = Vector2.Lerp(
                    target.localPosition,
                    localPoint,
                    Time.deltaTime * followSpeed
                );
        }

        // ---------- MOBILE: MULTI-TOUCH ----------
        private void HandleMobileMultiTouch()
        {
            if (canvas == null)
                return;
            Touch touch = Input.touches[0];
            Vector2 localPoint;
            RectTransform parentRect = target.parent as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                touch.position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );
            // localPoint.y = Mathf.Max(localPoint.y, -parentRect.rect.height / 2 + target.rect.height * 0.5f + 260f);


            if (followSpeed <= 0f)
                target.localPosition = localPoint;
            else
                target.localPosition = Vector2.Lerp(
                    target.localPosition,
                    localPoint,
                    Time.deltaTime * followSpeed
                );
        }
    }
}