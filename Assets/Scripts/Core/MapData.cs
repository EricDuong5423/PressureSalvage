using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Abyssal/Map Data")]
public class MapData : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string SceneName;
    public Sprite ReviewImage;
    public Vector2 mapPosition;
    [TextArea] public string Description;
    
    [Header("Content")]
    public LootProfile LootProfile;
    public EnemySpawnProfile EnemySpawnProfile;

    [Header("Unlock")] 
    public bool UnlockedByDefault;
    public int RequiredDay;
    public int UnlockCost;
    [TextArea] public string LockHint;
}
