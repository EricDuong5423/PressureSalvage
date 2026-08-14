using System;
using UnityEngine;

public class CheatManager : MonoBehaviour
{
    private OxygenSystem _oxygenSystem;
    private GameProgressionManager _gameProgressionManager;
    [SerializeField] private MapData deepWaterMap;

    private void Awake()
    {
        _oxygenSystem = FindAnyObjectByType<OxygenSystem>();
        _gameProgressionManager = GameProgressionManager.Instance;
    }

    public void IncreaseCredits()
    {
        if (_gameProgressionManager == null) return;
        _gameProgressionManager.AddCredits(100);
    }

    public void UnlockDeepWaterMap(MapData mapData)
    {
        _gameProgressionManager.DebugUnlockMap(mapData);
    }
}
