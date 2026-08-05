using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Serializable]
    public class Cell
    {
        public GameObject root;
        public Image icon;
        public Image highlight;
        public TMP_Text label;
    }
    
    [SerializeField] private Cell[] cells;
    [SerializeField] private TMP_Text weightText;

    private void OnEnable()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null) Inventory.Instance.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        var inv = Inventory.Instance;
        float totalWeight = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            bool used = inv != null && i < inv.Capacity;
            if (cells[i].root) cells[i].root.SetActive(used);
            if (!used) continue;

            var slot = inv.slots[i];
            if (slot.data != null)
            {
                totalWeight += slot.data.weightKg;
            }
            if (cells[i].icon)
            {
                cells[i].icon.enabled = !slot.Empty;
                cells[i].icon.sprite = slot.Empty ? null : slot.data.icon;
            }
            if (cells[i].highlight) cells[i].highlight.enabled = (i == inv.activeIndex);
            if (cells[i].label) cells[i].label.text = (i + 1).ToString();
        }

        if (weightText == null) return;
        weightText.text = $"Weight: {totalWeight} kg";
    }
}
