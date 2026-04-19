using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    //For raycast
    private Camera cam;
    [Header("Raycast")]
    [SerializeField]
    private float distance = 3f;
    [SerializeField]
    private LayerMask mask;
    private MeshRenderer lastRenderer;
    
    //For UI
    private PlayerUI playerUI;
    
    //For handle the input
    private InputManager inputManager;
    private RaycastHit hitInfo;

    //For carrying object
    [Header("Carry item")]
    [SerializeField] 
    private Transform carryParent;
    [SerializeField]
    private Transform twoHandCarryParent;
    private ICarryable currentCarried;

    private MaterialPropertyBlock propBlock;
    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        playerUI = GetComponent<PlayerUI>();
        inputManager = GetComponent<InputManager>();
        propBlock = new MaterialPropertyBlock();
        
        inputManager.OnFoot.Interact.performed += Interact;
        inputManager.OnFoot.Drop.performed += Drop;
    }

    private void Drop(InputAction.CallbackContext obj)
    {
        if (currentCarried == null) return;
        currentCarried.Drop();
        currentCarried = null;
    }

    private void Interact(InputAction.CallbackContext obj)
    {
        if (currentCarried != null && currentCarried.IsTwoHandRequired) return;
        if (hitInfo.collider == null) return;

        if (hitInfo.collider.TryGetComponent<Interactable>(out Interactable interactable)) 
            interactable.BaseInteract();

        if (hitInfo.collider.TryGetComponent<ICarryable>(out ICarryable carryable))
        {
            Transform parent = carryable.IsTwoHandRequired ?  twoHandCarryParent : carryParent;
            carryable.Carry(parent);
            currentCarried = carryable;
        }
    }
    
    void Update()
    {
        playerUI.UpdateText(string.Empty);
        //Create a ray with the origin from the camera's position with direction where the camera is facing
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        
        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            //For outline
            MeshRenderer currentRenderer = hitInfo.collider.gameObject.GetComponent<MeshRenderer>();

            if (lastRenderer != currentRenderer && currentCarried == null)
            {
                if (lastRenderer != null)
                {
                    TurnOffOutline(lastRenderer);
                }
                if (currentRenderer != null)
                {
                    SetOutline(currentRenderer, 1.05f);
                }
                lastRenderer = currentRenderer;
            }
            
            if (hitInfo.collider.TryGetComponent<Interactable>(out Interactable interactable))
            {
                string text = currentCarried != null ? "Hand full" : interactable.promptMessage;
                playerUI.UpdateText(text);
            }
        }
        else
        {
            if (lastRenderer != null)
            {
                TurnOffOutline(lastRenderer);
                lastRenderer = null;
            }
        }
    }

    private void SetOutline(MeshRenderer renderer, float value)
    {
        renderer.GetPropertyBlock(propBlock, 1);
        propBlock.SetFloat("_Scale", value);
        renderer.SetPropertyBlock(propBlock, 1);
    }

    private void TurnOffOutline(MeshRenderer renderer)
    {
        SetOutline(renderer, 0f);
    }
}
