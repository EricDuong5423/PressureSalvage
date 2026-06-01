using UnityEngine;

public class PickupItem : Interactable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void Interact()
    {
        Debug.Log("+1 " + gameObject.name);
        Destroy(gameObject);
    }
}
