using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LGU
{
    public static class LGUMenus
    {
        private const int MenuPriority = -30;
        private static LGUPrefebManager lguPrefebManager;

        [MenuItem("GameObject/LGU/MergeEffectCanvas", priority = -200)]
        private static void CreateLGUUICanvas()
        {
            LoadManager();
            SafeInstantiate(LGUMenusData => LGUMenusData.mergeEffectCanvas, isUnderCanvas: false);
            CreateEventSystem();
        }

        [MenuItem("GameObject/LGU/MergeableGlass", priority = MenuPriority)]
        private static void CreateLGUCard()
        {
            LoadManager();
            SafeInstantiate(LGUMenusData => LGUMenusData.mergeableGlass, isCenter: true);
        }

        [MenuItem("GameObject/LGU/SingleGlass", priority = MenuPriority)]
        private static void CreateLGUBackground()
        {
            LoadManager();
            SafeInstantiate(LGUMenusData => LGUMenusData.singleGlass, isCenter: true, isOverlay: true);
        }

        // Private Fuction
        private static void LoadManager()
        {
            if (lguPrefebManager == null)
                lguPrefebManager = Resources.Load<LGUPrefebManager>("ULG Prefeb Manager");
        }

        private static GameObject SafeInstantiate(Func<LGUPrefebManager, GameObject> itemSelector, bool isUnderCanvas = true, bool isCenter = false, bool isOverlay = false)
        {
            var prefebManager = LGUPrefebManager.Instance;
            if (!prefebManager)
                return null;

            if (isUnderCanvas)
            {
                Canvas canvas;
                bool isCanvasSelected = Selection.activeGameObject && Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (isCanvasSelected)
                {
                    canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
                }
                else
                {
                    canvas = GameObject.FindObjectOfType<Canvas>();
                    Selection.activeObject = canvas;
                    if (canvas == null)
                    {
                        GameObject canvasGo = CreateCanvasAndSetAsSelected(isOverlay);
                        canvas = canvasGo.GetComponentInChildren<Canvas>();
                    }
                }

                if (!((isOverlay && canvas.renderMode == RenderMode.ScreenSpaceOverlay) || (!isOverlay && canvas.renderMode == RenderMode.ScreenSpaceCamera)))
                {
                    String message = isOverlay ? "SingleGlass can only be created on ScreenSpaceOverlay Canvas" : "MergeableGlass can only be created on ScreenSpaceCamera Canvas";
                    Debug.Log(message);
                    return null;
                }
            }

            Transform container = Selection.activeTransform;
            GameObject item = itemSelector(prefebManager);
            GameObject instance = GameObject.Instantiate(item, isUnderCanvas ? container : null);
            instance.name = instance.name.Replace("(Clone)", "");

            // Set poistion
            SceneView sceneView = isUnderCanvas && !isCenter ? SceneView.lastActiveSceneView : null;
            instance.transform.position = sceneView ? sceneView.pivot : Vector3.zero;

            var localPosition = instance.transform.localPosition;
            localPosition.z = 0;
            instance.transform.localPosition = localPosition;

            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");
            Selection.activeObject = instance;

            return instance;
        }

        private static GameObject CreateCanvasAndSetAsSelected(bool isOverlay)
        {
            // Canvas
            GameObject canvasGO = new GameObject();
            canvasGO.name = "Canvas";
            canvasGO.AddComponent<Canvas>();

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = isOverlay ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            canvasGO.name = canvasGO.name.Replace("(Clone)", "");
            Undo.RegisterCreatedObjectUndo(canvasGO, $"Create {canvasGO.name}");
            Selection.activeObject = canvasGO;

            CreateEventSystem();

            return canvasGO;
        }

        private static void CreateEventSystem()
        {
            //Event System
            if (GameObject.FindObjectOfType<StandaloneInputModule>() == null)
            {
                GameObject eventSystemGO = new GameObject();
                eventSystemGO.name = "EventSystem";
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemGO, $"Create {eventSystemGO.name}");
            }
        }
    }
}