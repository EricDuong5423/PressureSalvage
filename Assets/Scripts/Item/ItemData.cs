using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Game Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public ItemRank rank;
    public Sprite icon;

    [Header("Economy")] 
    public int minValue;
    public int maxValue;

    [Header("Physics")] 
    public float weightKg = 1f;

    [Header("Properties")] 
    public bool canBreak = false;
    public bool isQuest;

    [Header("Prefab")] 
    public GameObject worldPrefab;
}

public enum ItemRank {F, D, C, B, A, S}