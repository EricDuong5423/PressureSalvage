using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapTooltip : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text nameText, descriptionText, poolText, statusText;
    [SerializeField] private Button selectButton, unlockButton, closeButton;

    private MapSelectUI owner;
    private MapData map;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
    }

    private Camera EnsureCamera()
    {
        if (screenCanvas != null)
        {
            if (screenCanvas.worldCamera == null && Camera.main != null)
                screenCanvas.worldCamera = Camera.main;
            if (screenCanvas.worldCamera != null) return screenCanvas.worldCamera;
        }
        return Camera.main;
    }

    public void Open(MapData m, bool available, MapSelectUI ui, Vector3 markerWorldPos)
    {
        owner = ui; map = m;
        if (root) root.SetActive(true);

        PositionAt(markerWorldPos);

        if (nameText) nameText.text = m.DisplayName;
        if (descriptionText) descriptionText.text = m.Description;
        if (poolText) poolText.text = BuildPool(m);
        if (statusText)
        {
            statusText.text = available ? "AVAILABLE"
                : (m.UnlockCost > 0 ? $"Unlock: {m.UnlockCost}₡" : m.LockHint);
            statusText.color = available ? Color.green : Color.red;
        }

        if (selectButton)
        {
            selectButton.gameObject.SetActive(available);
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => owner.ChooseMap(map));
        }
        if (unlockButton)
        {
            bool canBuy = !available && m.UnlockCost > 0;
            unlockButton.gameObject.SetActive(canBuy);
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() => owner.TryUnlock(map, markerWorldPos));
        }
    }

    private void PositionAt(Vector3 worldPos)
    {
        if (panelRect == null || canvasRect == null) return;
        Camera cam = EnsureCamera();
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z < 0f) return;
        // if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out Vector2 local))
        //     panelRect.localPosition = local + screenOffset;
    }

    public void Hide() { if (root) root.SetActive(false); }

    private string BuildPool(MapData m)
    {
        var lp = m.LootProfile;
        if (lp == null || lp.items == null || lp.items.Length == 0) return "Loot: ?";
        var sb = new StringBuilder("Loot: "); bool first = true;
        foreach (var e in lp.items)
        {
            if (e.prefab == null) continue;
            if (!first) sb.Append(", ");
            sb.Append(e.prefab.name); first = false;
        }
        return sb.ToString();
    }
}