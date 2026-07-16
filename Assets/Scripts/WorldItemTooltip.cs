using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class WorldItemTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas tooltipCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text itemInfoText;

    [Header("World Position")] 
    [SerializeField, Min(0f)] private float verticalGap = 0.15f;
    [SerializeField, Min(0f)] private float followSharpness = 20f;
    [Tooltip("Để (0, 0, 0). Nếu Canvas bị ngược thì dùng (0, 180, 0).")]
    [SerializeField] private Vector3 rotationOffset;
    private CarryItem currentItem;
    private Camera viewer;
    private Renderer[] targetRenderers =
        Array.Empty<Renderer>();

    private bool isVisible;
    private Sequence visibilitySequence;
    private void Awake()
    {
        if (tooltipCanvas == null)
            tooltipCanvas = GetComponentInChildren<Canvas>();
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (tooltipCanvas != null)
            tooltipCanvas.renderMode = RenderMode.WorldSpace;

        Hide();
    }

    public void Show(CarryItem item, Camera playerCamera)
    {
        if (item == null ||
            item.data == null ||
            playerCamera == null)
        {
            Hide();
            return;
        }
        
        currentItem = item;
        viewer = playerCamera;

        targetRenderers = item.GetComponentsInChildren<Renderer>(true);
        
        if (tooltipCanvas != null)
            tooltipCanvas.worldCamera = playerCamera;

        UpdateContent(item);
        SnapToTarget();
        SetVisible(true);
    }

    public void Hide()
    {
        currentItem = null;
        viewer = null;
        targetRenderers = Array.Empty<Renderer>();
        
        SetVisible(false);
    }

    private void UpdateContent(CarryItem item)
    {
        ItemData itemData = item.data;
        if (itemInfoText == null) return;

        itemInfoText.text = itemData.itemName.ToUpperInvariant() + "\n" +
                            $"SALVAGE VALUE   {item.Value:N0}₡\n" +
                            $"RANK {itemData.rank}  ·  {itemData.weightKg:0.0} KG";
    }

    private void LateUpdate()
    {
        if (!isVisible) return;
        
        if (currentItem == null || viewer == null)
        {
            Hide();
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        float followAmount = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, followAmount);
        FaceCamera();
    }
    
    private void SnapToTarget()
    {
        transform.position = GetTargetPosition();
        FaceCamera();
    }
    
    private Vector3 GetTargetPosition()
    {
        if (TryGetCombinedBounds(out Bounds bounds))
        {
            return bounds.center +
                   Vector3.up *
                   (bounds.extents.y + verticalGap);
        }

        return currentItem.transform.position +
               Vector3.up * verticalGap;
    }
    
    private bool TryGetCombinedBounds(out Bounds bounds)
    {
        bounds = default;
        bool foundRenderer = false;

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (renderer is not MeshRenderer &&
                renderer is not SkinnedMeshRenderer)
            {
                continue;
            }

            if (!foundRenderer)
            {
                bounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundRenderer;
    }

    private void FaceCamera()
    {
        if (viewer == null)
            return;

        transform.rotation =
            viewer.transform.rotation *
            Quaternion.Euler(rotationOffset);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (canvasGroup == null)
            return;

        visibilitySequence?.Kill();
        visibilitySequence = null;
        
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!visible)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = 0f;
        visibilitySequence = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, 0.07f)
                .SetEase(Ease.OutQuad))
            .AppendInterval(0.025f)
            .Append(
                canvasGroup.DOFade(0.12f, 0.045f)
                    .SetEase(Ease.Linear))
            .AppendInterval(0.02f)
            .Append(
                canvasGroup.DOFade(0.75f, 0.04f)
                    .SetEase(Ease.Linear))
            .Append(
                canvasGroup.DOFade(0.25f, 0.035f)
                    .SetEase(Ease.Linear))
            .Append(
                canvasGroup.DOFade(1f, 0.1f)
                    .SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                if (visible)
                    canvasGroup.alpha = 1f;

                visibilitySequence = null;
            });
    }
    
    private void OnDisable()
    {
        visibilitySequence?.Kill();
        visibilitySequence = null;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        isVisible = false;
    }
}
