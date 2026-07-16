using System.Text;
using DG.Tweening;
using UnityEngine;
using TinyGiantStudio.Text;

public class MapSelectText : MonoBehaviour
{
    [SerializeField] private Modular3DText _destinationText;
    private string _last;

    private Tween _decodeTween;

    private void Awake()
    {
        if(_destinationText == null) 
            _destinationText = GetComponent<Modular3DText>();
    }

    private void Start()
    {
        if (GameProgressionManager.Instance != null)
            GameProgressionManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        _decodeTween?.Kill();
        if (GameProgressionManager.Instance != null)
            GameProgressionManager.Instance.OnStateChanged -= Refresh;
    }

    private void Refresh()
    {
        var g = GameProgressionManager.Instance;
        string newText = (g != null && g.SelectedMap != null) ? g.SelectedMap.DisplayName : "WAITING";
        if (newText == _last) return;
        _last = newText;
        _destinationText.UpdateText(newText);
    }
}
