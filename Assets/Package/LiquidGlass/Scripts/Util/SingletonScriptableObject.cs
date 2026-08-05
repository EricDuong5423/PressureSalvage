using UnityEngine;

namespace LGU
{
    public class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Resources.LoadAll("", typeof(T));
                    T[] results = Resources.FindObjectsOfTypeAll<T>();
                    if (results.Length == 0)
                    {
                        Debug.Log("SingletonScriptableObject: result length is 0 of " + typeof(T).ToString());
                        return null;
                    }
                    if (results.Length > 1)
                    {
                        Debug.Log("SingletonScriptableObject: result length is greater than 1 of " + typeof(T).ToString());
                        return null;
                    }

                    _instance = results[0];
                    _instance.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
                return _instance;
            }
        }
    }
}