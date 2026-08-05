using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LGU
{
    public class DefinesUtils
    {
        /// <summary> Add a custom define </summary>
        /// <param name="define"></param>
        public static void AddDefine(string define)
        {
            var definesList = GetDefines();
            if (!definesList.Contains(define))
            {
                definesList.Add(define);
                SetDefines(definesList);
            }
        }

        /// <summary> Remove a custom define </summary>
        /// <param name="_define"></param>
        public static void RemoveDefine(string define)
        {
            var definesList = GetDefines();
            if (definesList.Contains(define))
            {
                definesList.Remove(define);
                SetDefines(definesList);
            }
        }

        /// <summary> Returns the list custom define added into the system </summary>
        public static List<string> GetDefines()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Split(';').ToList();
        }

        /// <summary> Sets the custom defeins </summary>
        /// <param name="definesList"></param>
        public static void SetDefines(List<string> definesList)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines = string.Join(";", definesList.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        }
    }
}