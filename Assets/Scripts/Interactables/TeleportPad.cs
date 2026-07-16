using System.Collections.Generic;
using UnityEngine;

public class TeleportPad : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private float materializeDuration = 1.2f;
    
    private readonly List<GameObject> spawnedItems = new();
    private void Start() => RebuildAll();
    public void RebuildAll()
    {
        ClearSpawned();
        var loadout = PlayerLoadout.Instance;
        var inv = Inventory.Instance;
        if (loadout == null) return;
        foreach (var gear in loadout.ownedGear)
        {
            if (gear == null || gear.worldPrefab == null) continue;
            if (inv != null && inv.Contains(gear)) continue;
            SpawnItem(gear, false);
        }
    }
    public void SpawnGear(ItemData gear)
    {
        if (gear == null || gear.worldPrefab == null) return;
        SpawnItem(gear, true);
    }
    private void SpawnItem(ItemData gear, bool withDissolve)
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        var go = Instantiate(gear.worldPrefab, pos, rot);
        if (go.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
        }
        spawnedItems.Add(go);
        if (withDissolve && dissolveMaterial != null)
        {
            var fx = go.AddComponent<MaterializeEffect>();
            fx.Init(dissolveMaterial, materializeDuration);
            fx.Play();
        }
    }
    private void ClearSpawned()
    {
        foreach (var go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();
    }
}
