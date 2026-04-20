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
        
        MonoBehaviour item = currentCarried as  MonoBehaviour;
        if (item != null)
            item.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 0.3f;
        
        currentCarried.Drop();
        currentCarried = null;
    }

    private void Interact(InputAction.CallbackContext obj)
    {
        if (currentCarried != null && currentCarried.IsTwoHandRequired) return;
        if (hitInfo.collider == null) return;

        if (hitInfo.collider.TryGetComponent<Interactable>(out Interactable interactable)) 
            interactable.BaseInteract();

        if (hitInfo.collider.TryGetComponent<ICarryable>(out ICarryable carryable) && currentCarried == null)
        {
            Transform parent = carryable.IsTwoHandRequired ?  twoHandCarryParent : carryParent;
            carryable.Carry(parent);
            currentCarried = carryable;
        }
    }

    private void ClearOutline()
    {
        if (lastRenderer != null)
        {
            TurnOffOutline(lastRenderer);
            lastRenderer = null;
        }
    }

    private void HandleOutline(MeshRenderer currentRenderer, bool targetIsCarryable)
    {
        bool handFull = currentCarried != null && (currentCarried.IsTwoHandRequired || targetIsCarryable);
        if (handFull)
        {
            ClearOutline();
            return;
        }
        if (currentRenderer == lastRenderer) return;
        ClearOutline();
        if (currentRenderer != null)
        {
            SetOutline(currentRenderer, 1.05f);
            lastRenderer = currentRenderer;
        }
    }
    
    private void HandleInteractionUI(Collider hitCollider)
    {
        if (!hitCollider.TryGetComponent<Interactable>(out Interactable interactable))
        {
            playerUI.UpdateText(string.Empty);
            return;
        }
        string text = interactable.promptMessage;
        hitCollider.TryGetComponent<ICarryable>(out ICarryable carryable);
        bool twoHandRequired = carryable != null ? carryable.IsTwoHandRequired : false;

        if (currentCarried != null && (currentCarried.IsTwoHandRequired || twoHandRequired || carryable != null))
        {
            text = "Hand full";
        }
        playerUI.UpdateText(text);
    }
    
    void Update()
    {
        //Create a ray with the origin from the camera's position with direction where the camera is facing
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);

        if (!Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            ClearOutline();
            playerUI.UpdateText(string.Empty);
            return;
        }
        
        MeshRenderer currentRenderer = hitInfo.collider.GetComponent<MeshRenderer>();
        bool targetIsCarryable = hitInfo.collider.TryGetComponent<ICarryable>(out ICarryable carryable);
        HandleOutline(currentRenderer, targetIsCarryable);
        
        HandleInteractionUI(hitInfo.collider);
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
