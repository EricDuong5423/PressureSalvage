using UnityEngine;

public class DiveDescent : Interactable
{
    private void Start() => promptMessage = "Dive down";

    protected override void Interact()
    {
        var g = GameProgressionManager.Instance;
        if (g == null || g.SelectedMap == null)
        {
            promptMessage = "Please select a Map"; 
            return; 
        }
        CameraFade.Instance?.TransitionTo(g.SelectedMap.SceneName);
        g.SetSelectedMap(null);
    }
}
