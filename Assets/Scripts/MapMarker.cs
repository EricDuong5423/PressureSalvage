using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MapMarker : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Color availableColor = Color.chartreuse;
    [SerializeField] private Color lockedColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float hoverScale = 1.2f;

    private MapData map;
    private bool available;
    private MapSelectUI owner;
    private RectTransform _rt;
    private Vector3 _baseScale;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
    }

    public void Setup(MapData m, bool avail, MapSelectUI ui)
    {
        map = m; available = avail; owner = ui;
        if (icon) icon.color = avail ? availableColor : lockedColor;
        _rt.localScale = _baseScale;
    }

    public void OnPointerEnter() => _rt.localScale = _baseScale * hoverScale;
    public void OnPointerExit()  => _rt.localScale = _baseScale;
    public void OnPointerClick() => owner.OpenInfo(map, available, _rt.position);
}