using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    //For raycast
    private Camera cam;
    [Header("Raycast")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask;
    [Header("Target Feedback")]
    [SerializeField] private Material hologramMaterial;
    [SerializeField] private WorldItemTooltip worldItemTooltip;
    private MeshRenderer lastRenderer;
    
    private readonly Dictionary<Renderer, Material[]>
        originalMaterials = new();
    
    //For UI
    private PlayerUI playerUI;
    //For handle the input
    private InputManager inputManager;
    private PlayerHotbar hotbar;
    private RaycastHit hitInfo;
    private Interactable currentTarget;

    private MaterialPropertyBlock propBlock;
    
    private bool initialized;
    private bool inputSubscribed;

    public float CarriedWeightKg => Inventory.Instance != null ? Inventory.Instance.TotalWeight : 0f;
    private void Start()
    {
        inputManager = GetComponent<InputManager>();
        hotbar = GetComponent<PlayerHotbar>();

        PlayerLook playerLook =
            GetComponent<PlayerLook>();

        cam = playerLook != null
            ? playerLook.cam
            : null;

        if (cam == null)
            cam = GetComponentInChildren<Camera>(true);

        playerUI = PlayerUI.Instance;

        if (playerUI == null)
            playerUI = FindAnyObjectByType<PlayerUI>();

        if (worldItemTooltip == null)
        {
            worldItemTooltip =
                GetComponentInChildren<WorldItemTooltip>(true);
        }

        if (cam == null)
        {
            Debug.LogError(
                "PlayerInteract không tìm thấy camera.",
                this);
        }

        if (worldItemTooltip == null)
        {
            Debug.LogError(
                "PlayerInteract chưa được gán WorldItemTooltip.",
                this);
        }

        initialized = true;
        SubscribeInput();
    }

    private void OnEnable()
    {
        if (initialized)
            SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        ClearCurrentTarget();
        ClearPrompt();
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
        RestoreOriginalMaterials();
    }

    private void Update()
    {
        if (cam == null)
            return;

        Ray ray = new Ray(
            cam.transform.position,
            cam.transform.forward);

        if (!Physics.Raycast(
                ray,
                out hitInfo,
                distance,
                mask))
        {
            hitInfo = default;
            SetCurrentTarget(null);
            ClearPrompt();
            return;
        }

        Interactable target =
            hitInfo.collider
                .GetComponentInParent<Interactable>();

        SetCurrentTarget(target);
        UpdateInteractionUI(target);
    }

    private void SetCurrentTarget(Interactable newTarget)
    {
        if (newTarget == currentTarget)
            return;

        ClearCurrentTarget();

        currentTarget = newTarget;

        if (currentTarget == null)
            return;

        ApplyHologram(currentTarget);

        if (currentTarget is CarryItem item &&
            item.data != null &&
            worldItemTooltip != null)
        {
            worldItemTooltip.Show(
                item,
                cam);
        }
    }

    private void ClearCurrentTarget()
    {
        RestoreOriginalMaterials();

        if (worldItemTooltip != null)
            worldItemTooltip.Hide();

        currentTarget = null;
    }

    private void ApplyHologram(Interactable target)
    {
        if (target == null || hologramMaterial == null)
            return;

        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer is not MeshRenderer &&
                renderer is not SkinnedMeshRenderer)
            {
                continue;
            }

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
                continue;
            
            originalMaterials[renderer] = materials;
            Material[] hologramMaterials =
                new Material[materials.Length];

            for (int i = 0;
                 i < hologramMaterials.Length;
                 i++)
            {
                hologramMaterials[i] =
                    hologramMaterial;
            }

            renderer.sharedMaterials =
                hologramMaterials;
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry
                 in originalMaterials)
        {
            Renderer renderer = entry.Key;

            if (renderer != null)
                renderer.sharedMaterials = entry.Value;
        }

        originalMaterials.Clear();
    }

    private void UpdateInteractionUI(
        Interactable target)
    {
        if (target == null)
        {
            ClearPrompt();
            return;
        }

        if (target is CarryItem item &&
            item.data != null)
        {
            bool inventoryFull = IsInventoryFull();

            if (playerUI != null)
            {
                playerUI.UpdateText(
                    inventoryFull
                        ? "Inventory full"
                        : $"Pick up {item.data.itemName}");
            }

            return;
        }

        if (worldItemTooltip != null)
            worldItemTooltip.Hide();

        if (playerUI != null)
            playerUI.UpdateText(target.promptMessage);
    }

    private bool IsInventoryFull()
    {
        return Inventory.Instance != null &&
               Inventory.Instance.TryWouldBeFull();
    }

    private void Interact(
        InputAction.CallbackContext context)
    {
        if (currentTarget == null)
            return;

        if (currentTarget is CarryItem item)
        {
            bool pickedUp =
                hotbar != null &&
                hotbar.TryPickup(item);

            if (!pickedUp)
            {
                UpdateInteractionUI(currentTarget);
                return;
            }
            ClearCurrentTarget();
            ClearPrompt();
            hitInfo = default;
            return;
        }

        Interactable target = currentTarget;
        ClearCurrentTarget();
        ClearPrompt();
        hitInfo = default;

        target.BaseInteract();
    }

    private void Drop(
        InputAction.CallbackContext context)
    {
        hotbar?.DropActive();
    }

    private void ClearPrompt()
    {
        if (playerUI != null)
            playerUI.UpdateText(string.Empty);
    }

    private void SubscribeInput()
    {
        if (inputSubscribed || inputManager == null)
            return;

        inputManager.OnFoot.Interact.performed +=
            Interact;

        inputManager.OnFoot.Drop.performed +=
            Drop;

        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!inputSubscribed || inputManager == null)
            return;

        inputManager.OnFoot.Interact.performed -=
            Interact;

        inputManager.OnFoot.Drop.performed -=
            Drop;

        inputSubscribed = false;
    }
}
