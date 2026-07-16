using System;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnStart = true;

    private bool _autoSpawnSuppressed;
    public event Action<GameObject> PlayerSpawned;
    
    public GameObject CurrentPlayer { get; private set; }

    private void Start()
    {
        if (spawnOnStart && !_autoSpawnSuppressed) SpawnPlayer();
    }
    
    public void SuppressAutoSpawn()
    {
        _autoSpawnSuppressed = true;
    }

    public GameObject SpawnPlayer()
    {
        if (CurrentPlayer != null)
            return CurrentPlayer;

        if (playerPrefab == null)
        {
            Debug.LogError(
                "PlayerSpawner: Player Prefab chưa được gán.",
                this);

            return null;
        }

        Transform point = spawnPoint != null
            ? spawnPoint
            : transform;

        CurrentPlayer = Instantiate(
            playerPrefab,
            point.position,
            point.rotation);
        
        PlayerSpawned?.Invoke(CurrentPlayer);

        return CurrentPlayer;
    }
}
