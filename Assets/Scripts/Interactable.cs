using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string promptMessage;
    public bool useEvents;
    public void BaseInteract()
    {
        if (useEvents)
            GetComponent<InteractionEvent>().OnInteract.Invoke();
        Interact();
    }
    
    protected virtual void Interact()
    {
        
    }
}
