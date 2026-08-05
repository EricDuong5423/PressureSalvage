using UnityEngine;

namespace LGU
{
    public static class DestroyHelper
    {
        public static void Destroy(Object @object)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(@object);
            }
            else
            {
                Object.DestroyImmediate(@object, false);
            }
#else
			Object.Destroy(@object);
#endif
        }
    }
}