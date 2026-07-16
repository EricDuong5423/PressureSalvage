using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Serializable]
    public class Slot
    {
        public ItemData data;
        public int Value;
        public bool Empty => data == null;

        public void Clear()
        {
            data = null;
            Value = 0;
        }
    }

    [SerializeField] private int capacity = 1;
    public Slot[] slots { get; private set; }
    public int activeIndex { get; private set; }

    public event Action OnChanged;
    
    public int Capacity => slots != null ? slots.Length : 0;
    public Slot Active => (slots != null && slots.Length > 0) ? slots[activeIndex] : null;

    public float TotalWeight
    {
        get
        {
            float w = 0f;
            if (slots != null)
                foreach (var s in slots)
                    if (!s.Empty)
                        w += s.data.weightKg;
            return w;
        }
    }

    public void ResetNewRun(int initialCapacity)
    {
        activeIndex = 0;
        slots = null;
        EnsureSize(initialCapacity);
        OnChanged?.Invoke();
    }

    public void ClearAll()
    {
        if (slots == null) return;

        foreach (var s in slots)
        {
            s?.Clear();
        }
        
        activeIndex = 0;
        OnChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureSize(capacity);
    }

    private void EnsureSize(int n)
    {
        n = Mathf.Max(1, n);
        var old = slots;
        slots = new Slot[n];
        for (int i = 0; i < n; i++)
            slots[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new Slot();
        if (activeIndex >= n)
            activeIndex = n - 1;
    }

    public void SetCapacity(int n)
    {
        if (Capacity == n) return;
        EnsureSize(n);
        OnChanged?.Invoke();
    }

    public bool TryAdd(ItemData data, int value)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].Empty)
            {
                slots[i].data = data;
                slots[i].Value = value;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void SetActive(int i)
    {
        if (i < 0 || i >= slots.Length || i == activeIndex) return;
        activeIndex = i;
        OnChanged?.Invoke();
    }

    public void ClearActive()
    {
        if (Active == null) return;
        Active.Clear();
        OnChanged?.Invoke();
    }

    public bool Contains(ItemData data)
    {
        foreach (var s in slots) if (!s.Empty && s.data == data) return true;
        return false;
    }
    
    public bool TryWouldBeFull()
    {
        foreach (var s in slots) if (s.Empty) return false;
        return true;
    }
}
