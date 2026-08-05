#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LGU
{
    // [CreateAssetMenu(menuName = "LGU/Prefeb Manager")]
    public class LGUPrefebManager : SingletonScriptableObject<LGUPrefebManager>
    {
        [Header("Canvas")]
        public GameObject mergeEffectCanvas;

        [Header("Glass Objects")]
        public GameObject mergeableGlass;
        public GameObject singleGlass;
    }

#if UNITY_EDITOR
    #region Editor
    [CustomEditor(typeof(LGUPrefebManager))]
    public class PrefabManagerSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.HelpBox("If you move this file somewhere else, also change the path in LGUMenus! ", MessageType.Info);
        }
    }
    #endregion
#endif
}