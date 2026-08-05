using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LGU{
    public class Utils
    {
#if UNITY_EDITOR

        public static void DrawRoundedRectWorld(Vector3[] c, float radius, int arcSteps)
        {
            // Corner layout:
            // 1----2
            // |    |
            // 0----3

            Vector3 bl = c[0];
            Vector3 tl = c[1];
            Vector3 tr = c[2];
            Vector3 br = c[3];

            // Edge directions
            Vector3 right = (br - bl).normalized;
            Vector3 up = (tl - bl).normalized;

            // Shorten edges by radius
            Vector3 bl_r = bl + right * radius + up * radius;
            Vector3 tl_r = tl + right * radius - up * radius;
            Vector3 tr_r = tr - right * radius - up * radius;
            Vector3 br_r = br - right * radius + up * radius;

            // Draw straight edges
            Handles.DrawLine(bl_r, tl_r);
            Handles.DrawLine(tl_r, tr_r);
            Handles.DrawLine(tr_r, br_r);
            Handles.DrawLine(br_r, bl_r);

            // Draw arcs
            DrawCornerArc(bl_r, -right, -up, radius, arcSteps);
            DrawCornerArc(tl_r, -right,  up, radius, arcSteps);
            DrawCornerArc(tr_r,  right,  up, radius, arcSteps);
            DrawCornerArc(br_r,  right, -up, radius, arcSteps);
        }

        static void DrawCornerArc(
            Vector3 center,
            Vector3 dirA,
            Vector3 dirB,
            float radius,
            int steps
        )
        {
            Vector3 prev = center + dirA * radius;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float ang = t * Mathf.PI * 0.5f;

                Vector3 next =
                    center +
                    (dirA * Mathf.Cos(ang) + dirB * Mathf.Sin(ang)) * radius;

                Handles.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}