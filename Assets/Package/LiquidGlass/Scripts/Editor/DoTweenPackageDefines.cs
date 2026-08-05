using System;
using System.Reflection;
using UnityEditor;

namespace LGU
{
    [InitializeOnLoad]
    public class DoTweenPackageDefines
    {
        // DO_TWEEN_INSTALLED
        // DO_TWEEN_NOT_INSTALLED
        static DoTweenPackageDefines()
        {
            UpdateDefines();
        }

        /// <summary>
        /// Update the defines for DoTween is installed or not
        /// </summary>
        static void UpdateDefines()
        {
            var isDoTweenInsntalled = IsDoTweenInstalled();
            if (isDoTweenInsntalled)
            {
                DefinesUtils.AddDefine("DO_TWEEN_INSTALLED");
                DefinesUtils.RemoveDefine("DO_TWEEN_NOT_INSTALLED");
            }
            else
            {
                DefinesUtils.AddDefine("DO_TWEEN_NOT_INSTALLED");
                DefinesUtils.RemoveDefine("DO_TWEEN_INSTALLED");
            }
        }

        public static bool IsDoTweenInstalled()
        {
            return NamespaceExists("DG.Tweening");
            // return File.Exists("Assets/Plugins/Demigiant/DOTween/DOTween.dll");
        }

        public static bool NamespaceExists(string desiredNamespace)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Namespace == desiredNamespace)
                        return true;
                }
            }
            return false;
        }
    }
}