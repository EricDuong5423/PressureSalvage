using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LootProfile", menuName = "Abyssal/Loot Profile")]
public class LootProfile : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public GameObject prefab;
        public int weight; 
        public int minDay;
    }
    
    public Entry[] items;
    public int baseCount = 5;
    public int extraPerDay = 1;
}
