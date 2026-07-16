using UnityEngine;

public class UnderwaterEnvironment : MonoBehaviour
{
    public static UnderwaterEnvironment Instance { get; private set; }
    [SerializeField] private WaterSettings settings;
    public WaterSettings Settings => settings;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
