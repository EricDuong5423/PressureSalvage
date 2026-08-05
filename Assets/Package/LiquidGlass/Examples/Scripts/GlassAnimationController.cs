using System.Collections;
using UnityEngine;

namespace LGU
{
    public class GlassAnimationController : MonoBehaviour
    {
        [Header("Follower")]
        public RectTransform glass;           // The glass object to move (if null, will use own RectTransform)

        [Header("Path Points (in order)")]
        public RectTransform[] waypoints;     // Points the glass will move between

        [Header("Movement Settings")]
        public float segmentDuration = 1.2f;  // Time to go from one point to next
        public float waitAtPoint = 0.2f;      // Optional pause at each point
        public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private void Awake()
        {
            Application.targetFrameRate = 120;
            if (glass == null)
                glass = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (waypoints != null && waypoints.Length > 1)
                StartCoroutine(FollowPathLoop());
        }

        private IEnumerator FollowPathLoop()
        {
            if (glass == null || waypoints == null || waypoints.Length < 2)
                yield break;

            int index = 0;

            // Start at first waypoint
            glass.localPosition = waypoints[0].localPosition;

            while (true)
            {
                RectTransform from = waypoints[index];
                RectTransform to = waypoints[(index + 1) % waypoints.Length];

                Vector3 startPos = from.localPosition;
                Vector3 endPos = to.localPosition;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / Mathf.Max(0.0001f, segmentDuration);
                    float k = ease.Evaluate(Mathf.Clamp01(t));
                    glass.localPosition = Vector3.Lerp(startPos, endPos, k);
                    yield return null;
                }

                glass.localPosition = endPos;

                if (waitAtPoint > 0f)
                    yield return new WaitForSeconds(waitAtPoint);

                index = (index + 1) % waypoints.Length;
            }
        }
    }
}