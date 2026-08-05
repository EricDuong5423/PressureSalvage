using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }
    [SerializeField]
    private TextMeshProUGUI promptText;
    [SerializeField] private PlayerSpawner playerSpawner;

    [Header("Oxygen UI")] 
    [SerializeField] private LiquidBar oxygenFill;
    [SerializeField] private TMP_Text oxygenRemaining;
    private string currentText;
    private OxygenSystem oxygenSystem;

    public void UpdateText(string promptMessage)
    {
        if (promptMessage == currentText) return;
        currentText = promptMessage;
        promptText.text = promptMessage;
    }
    
    private void OnEnable()
    {
        if (playerSpawner == null)
            playerSpawner = FindAnyObjectByType<PlayerSpawner>();

        if (playerSpawner == null)
        {
            Debug.LogError(
                "PlayerUI: Không tìm thấy PlayerSpawner.",
                this);

            return;
        }
        playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
        playerSpawner.PlayerSpawned += HandlePlayerSpawned;
        
        if (playerSpawner.CurrentPlayer != null)
            HandlePlayerSpawned(playerSpawner.CurrentPlayer);
    }
    
    private void OnDisable()
    {
        if (playerSpawner != null)
            playerSpawner.PlayerSpawned -= HandlePlayerSpawned;

        UnbindOxygen();
    }
    
    private void HandlePlayerSpawned(GameObject player)
    {
        if (player == null)
            return;

        OxygenSystem oxygen =
            player.GetComponentInChildren<OxygenSystem>(true);

        if (oxygen == null)
        {
            Debug.LogError(
                "Player vừa spawn không có OxygenSystem.",
                player);

            return;
        }

        BindOxygen(oxygen);
    }

    private void BindOxygen(OxygenSystem newOxygen)
    {
        if (oxygenSystem == newOxygen)
            return;

        UnbindOxygen();

        oxygenSystem = newOxygen;
        oxygenSystem.OnOxygenChanged.AddListener(UpdateOxygen);
        
        UpdateOxygen(oxygenSystem.CurrentPercent);
    }

    private void UnbindOxygen()
    {
        if (oxygenSystem == null)
            return;

        oxygenSystem.OnOxygenChanged.RemoveListener(UpdateOxygen);
        oxygenSystem = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void UpdateOxygen(float amount)
    {
        if (oxygenFill == null) return;

        float percent = amount / 100f;
        
        oxygenFill.targetFillAmount = percent;
        if (oxygenRemaining == null) return;
        oxygenRemaining.text = $"Oxygen: {Mathf.RoundToInt(oxygenSystem.CurrentPercent)}%";
    }
}
