using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LGU
{
    [CustomEditor(typeof(GlassRoundedRect))]
    [CanEditMultipleObjects]
    public class GlassRoundedRectEditor : ImageEditor
    {
        // --- Unity ImageEditor internal method names (we call via reflection) ---
        static System.Reflection.MethodInfo MI_RaycastControlsGUI;
        static System.Reflection.MethodInfo MI_MaterialGUI;

        SerializedProperty m_Sprite; // Unity Image sprite field (Image.m_Sprite)

        SerializedProperty cornerRadius;
        SerializedProperty effectIntensity;
        SerializedProperty overallTint;
        SerializedProperty isBlur;

        SerializedProperty useSpriteShape; // NEW

        // SerializedProperty useSpriteAlphaAsMask;
        // SerializedProperty useMobileFastNormals;

        SerializedProperty refractionPx;
        SerializedProperty dispersionGain;
        SerializedProperty thicknessPx;
        SerializedProperty reflectionFactor;
        SerializedProperty highlightTint;

        SerializedProperty fresnelRange;
        SerializedProperty fresnelHardness;
        SerializedProperty fresnelIntensity;

        SerializedProperty glareRange;
        SerializedProperty glareHardness;
        SerializedProperty glareConvergence;
        SerializedProperty glareOppositeFactor;
        SerializedProperty glareAngle;
        SerializedProperty glareIntensity;

        SerializedProperty showShadow;
        SerializedProperty shadowColor;
        SerializedProperty shadowRangePx;
        SerializedProperty shadowHardness;
        SerializedProperty shadowIntensity;
        SerializedProperty shadowOffset;

        SerializedProperty blurTexture;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Cache reflection methods once
            if (MI_RaycastControlsGUI == null)
            {
                var t = typeof(ImageEditor);
                MI_RaycastControlsGUI = t.GetMethod("RaycastControlsGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                MI_MaterialGUI = t.GetMethod("MaterialGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            // Unity sprite field
            m_Sprite = serializedObject.FindProperty("m_Sprite");

            cornerRadius = serializedObject.FindProperty("cornerRadius");
            effectIntensity = serializedObject.FindProperty("effectIntensity");
            // overallTint           = serializedObject.FindProperty("overallTint");
            isBlur = serializedObject.FindProperty("isBlur");

            useSpriteShape = serializedObject.FindProperty("useSpriteShape"); // NEW

            // useSpriteAlphaAsMask  = serializedObject.FindProperty("useSpriteAlphaAsMask");
            // useMobileFastNormals  = serializedObject.FindProperty("useMobileFastNormals");

            refractionPx = serializedObject.FindProperty("refractionPx");
            dispersionGain = serializedObject.FindProperty("dispersionGain");
            thicknessPx = serializedObject.FindProperty("thicknessPx");
            reflectionFactor = serializedObject.FindProperty("reflectionFactor");
            highlightTint = serializedObject.FindProperty("highlightTint");

            fresnelRange = serializedObject.FindProperty("fresnelRange");
            fresnelHardness = serializedObject.FindProperty("fresnelHardness");
            fresnelIntensity = serializedObject.FindProperty("fresnelIntensity");

            glareRange = serializedObject.FindProperty("glareRange");
            glareHardness = serializedObject.FindProperty("glareHardness");
            glareConvergence = serializedObject.FindProperty("glareConvergence");
            glareOppositeFactor = serializedObject.FindProperty("glareOppositeFactor");
            glareAngle = serializedObject.FindProperty("glareAngle");
            glareIntensity = serializedObject.FindProperty("glareIntensity");

            showShadow = serializedObject.FindProperty("showShadow");
            shadowColor = serializedObject.FindProperty("shadowColor");
            shadowRangePx = serializedObject.FindProperty("shadowRangePx");
            shadowHardness = serializedObject.FindProperty("shadowHardness");
            shadowIntensity = serializedObject.FindProperty("shadowIntensity");
            shadowOffset = serializedObject.FindProperty("shadowOffset");

            blurTexture = serializedObject.FindProperty("blurTexture");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Liquid Glass", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(cornerRadius, new GUIContent("Corner Radius"));
            EditorGUILayout.PropertyField(effectIntensity, new GUIContent("Glass Intensity"));
            // EditorGUILayout.PropertyField(overallTint,     new GUIContent("Tint"));
            EditorGUILayout.PropertyField(isBlur, new GUIContent("Is Blur"));

            // NEW: Sprite SDF shape toggle
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useSpriteShape, new GUIContent("Use Sprite Shape (SDF)"));
            if (useSpriteShape.boolValue)
            {
                EditorGUILayout.PropertyField(m_Sprite, new GUIContent("Sprite"));
                DrawUnityEssentialsNoAppearance();
                
                EditorGUILayout.HelpBox(
                    "Sprite texture's Read/Write must be enabled \n Sprite Shape works best when Image Type is Simple and the .",
                    MessageType.Info
                );
            }

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Masking & Performance", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(useSpriteAlphaAsMask, new GUIContent("Use Sprite Alpha As Mask"));
            // EditorGUILayout.PropertyField(useMobileFastNormals, new GUIContent("Mobile Fast Normals"));

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Glass", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(refractionPx,    new GUIContent("Refraction (px)"));
            // EditorGUILayout.PropertyField(thicknessPx,     new GUIContent("Thickness (px)"));
            // EditorGUILayout.PropertyField(reflectionFactor,new GUIContent("Reflection Factor (IOR)"));
            // EditorGUILayout.PropertyField(dispersionGain,  new GUIContent("Dispersion Gain"));
            // EditorGUILayout.PropertyField(highlightTint,   new GUIContent("Highlight Tint"));

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Fresnel Rim", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(fresnelRange);
            // EditorGUILayout.PropertyField(fresnelHardness);
            // EditorGUILayout.PropertyField(fresnelIntensity);

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Glare Band", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(glareRange);
            // EditorGUILayout.PropertyField(glareHardness);
            // EditorGUILayout.PropertyField(glareConvergence);
            // EditorGUILayout.PropertyField(glareOppositeFactor);
            // EditorGUILayout.PropertyField(glareAngle);
            // EditorGUILayout.PropertyField(glareIntensity);

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Shadow", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(showShadow);
            // EditorGUILayout.PropertyField(shadowColor);
            // EditorGUILayout.PropertyField(shadowRangePx);
            // EditorGUILayout.PropertyField(shadowHardness);
            // EditorGUILayout.PropertyField(shadowIntensity);
            // EditorGUILayout.PropertyField(shadowOffset);

            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Blur Input", EditorStyles.boldLabel);
            // EditorGUILayout.PropertyField(blurTexture);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    var rr = t as GlassRoundedRect;
                    if (!rr) continue;

                    rr.Refresh(); // SetVerticesDirty + ApplyMaterialParams
                    EditorUtility.SetDirty(rr);

                    if (rr.material != null)
                        EditorUtility.SetDirty(rr.material);
                }

                if (!Application.isPlaying)
                {
                    SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        void DrawUnityEssentialsNoAppearance()
        {
            // If reflection fails due to Unity changes, just do nothing (we still have Sprite).
            if (MI_RaycastControlsGUI == null || MI_MaterialGUI == null) return;

            // These two don’t draw the big "Image Type/Fill/Native Size" UI,
            // but keep key Unity wiring working.
            MI_RaycastControlsGUI.Invoke(this, null);
            MI_MaterialGUI.Invoke(this, null);
        }
    }
}
