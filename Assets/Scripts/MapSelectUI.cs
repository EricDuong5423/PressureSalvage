using UnityEngine;

public class MapSelectUI : MonoBehaviour
{
    [Header("Markers")]
    [SerializeField] private MapData[] maps;
    [SerializeField] private RectTransform markerContainer;
    [SerializeField] private MapMarker markerPrefab;     // prefab UI

    [Header("Refs")]
    [SerializeField] private MapSelectComputer computer;
    [SerializeField] private MapTooltip infoPanel;

    public void Build()
    {
        foreach (Transform c in markerContainer) Destroy(c.gameObject);
        var g = GameProgressionManager.Instance;
        foreach (MapData map in maps)
        {
            MapMarker marker = Instantiate(markerPrefab, markerContainer);
            ((RectTransform)marker.transform).anchoredPosition = map.mapPosition;
            marker.Setup(map, g != null && g.IsMapAvailable(map), this);
        }
    }

    public void OpenInfo(MapData map, bool available, Vector3 worldPos)
        => infoPanel?.Open(map, available, this, worldPos);

    public void ChooseMap(MapData map)
    {
        GameProgressionManager.Instance?.SetSelectedMap(map);
        infoPanel?.Hide();
        if (computer) computer.Close();
    }

    public void TryUnlock(MapData map, Vector3 worldPos)
    {
        var g = GameProgressionManager.Instance;
        if (g != null && g.TryPurchaseUnlock(map))
        {
            Build();                                      // refresh màu marker
            infoPanel?.Open(map, true, this, worldPos);   // mở lại tại marker đó
        }
    }
}