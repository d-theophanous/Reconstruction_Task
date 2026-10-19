using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PaletteSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Image icon;
    public TMP_Text countLabel; // swap for a TMP_Text field if your project uses TextMeshPro

    private PlaceableItemDefinition item;
    private PaletteManager palette;
    private SelectionManager selectionManager;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    private GameObject dragGhost;

    public void Setup(PlaceableItemDefinition itemDef, PaletteManager manager, SelectionManager selection)
    {
        item = itemDef;
        palette = manager;
        selectionManager = selection;
        rootCanvas = GetComponentInParent<Canvas>(true).rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();

        if (icon != null) icon.sprite = item.icon;
    }

    public void Refresh(int remainingCount)
    {
        bool outOfStock = remainingCount == 0;
        canvasGroup.alpha = outOfStock ? 0.4f : 1f;
        // Raycasts always stay enabled (even out of stock) so the icon can still
        // be clicked to select the already-placed instance. Dragging while out
        // of stock is harmless - PaletteManager.TryConsume blocks the actual spawn.

        if (countLabel != null)
            countLabel.text = remainingCount < 0 ? "" : remainingCount.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var instance = palette.GetSpawnedInstance(item);
        if (instance != null && selectionManager != null)
            selectionManager.Select(instance);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhost.transform.SetParent(rootCanvas.transform, false);

        var img = dragGhost.GetComponent<Image>();
        img.sprite = item.icon;
        img.raycastTarget = false;
        dragGhost.GetComponent<RectTransform>().sizeDelta = ((RectTransform)icon.transform).sizeDelta;
        dragGhost.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null) Destroy(dragGhost);

        if (palette.TryConsume(item))
            palette.SpawnAtScreenPosition(item, eventData.position);
    }
}
