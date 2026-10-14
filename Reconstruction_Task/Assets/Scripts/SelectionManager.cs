using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public Camera mainCamera => MainManager.Instance.CurCamera;
    public LayerMask placedObjectLayer;
    public TransformGizmo gizmo;
    public PaletteManager palette;

    [Tooltip("Optional - tints the selected object when assigned.")]
    public ObjectHighlighter highlighter;
    public bool enableHighlightOnSelect = true;

    [Tooltip("Units per second when nudging the selected axis with the keyboard.")]
    public float keyboardMoveSpeed = 2f;

    [Tooltip("Units per second when scaling with the keyboard (D = up, A = down).")]
    public float keyboardScaleSpeed = 1f;

    [Tooltip("Units per second when moving the selected object away from / towards the camera with W/S.")]
    public float keyboardDepthSpeed = 2f;

    public PlaceableObject Selected => selected;

    // Fired whenever the selection changes - passes null on deselect.
    // ActionPanelUI subscribes to this to enable/disable its buttons.
    public event Action<PlaceableObject> OnSelectionChanged;

    private PlaceableObject selected;

    private bool isDragging;
    private Plane dragPlane;
    private Vector3 dragStartOffset;

    void Update()
    {
        // Settings menu is open (or was just toggled) - leave the scene alone.
        if (MainManager.Instance != null && MainManager.Instance.SettingsBlockingInput) return;

        // W = move tool, R = scale tool (same shortcuts as the Unity editor)
        if (Input.GetKeyDown(KeyCode.W)) gizmo.SetMode(GizmoMode.Move);
        if (Input.GetKeyDown(KeyCode.R)) gizmo.SetMode(GizmoMode.Scale);

        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            DeleteSelected();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            TrySelect();

        if (isDragging)
        {
            if (Input.GetMouseButton(0)) ContinueDrag();
            else isDragging = false;
        }

        HandleAxisNudge();
        HandleScaleNudge();
        HandleDepthNudge();
    }

    void HandleAxisNudge()
    {
        // Active once an axis button has been pressed in Move mode - additional to mouse dragging.
        if (gizmo.Mode != GizmoMode.Move) return;
        if (gizmo.Target == null || gizmo.ActiveAxis == null) return;

        GizmoAxis axis = gizmo.ActiveAxis.Value;
        Vector3 axisDir = axis == GizmoAxis.X ? Vector3.right : axis == GizmoAxis.Y ? Vector3.up : Vector3.forward;

        float input = 0f;
        if (axis == GizmoAxis.X)
        {
            // Left/right axis - A/D
            if (Input.GetKey(KeyCode.A)) input -= 1f;
            if (Input.GetKey(KeyCode.D)) input += 1f;
        }
        else
        {
            // The other two axes (Y and Z) - W/S
            if (Input.GetKey(KeyCode.S)) input -= 1f;
            if (Input.GetKey(KeyCode.W)) input += 1f;
        }

        if (Mathf.Abs(input) > 0f)
            gizmo.Target.position += axisDir * input * keyboardMoveSpeed * Time.deltaTime;
    }

    // A/D changes uniform scale whenever something is selected - independent of the
    // gizmo's scale-tool button, mirroring how W/S works for depth. Skipped while an
    // explicit X-axis move nudge (chosen via the action panel) is already driving A/D,
    // so the two systems don't double up. Clamped via TransformGizmo.minScale/maxScale,
    // same limit the mouse-dragged scale handle uses.
    void HandleScaleNudge()
    {
        if (gizmo.Target == null) return;
        if (gizmo.Mode == GizmoMode.Move && gizmo.ActiveAxis.HasValue && gizmo.ActiveAxis.Value == GizmoAxis.X)
            return;

        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f; // scale down
        if (Input.GetKey(KeyCode.D)) input += 1f; // scale up

        if (Mathf.Abs(input) > 0f)
        {
            Vector3 scale = gizmo.Target.localScale;
            float current = (scale.x + scale.y + scale.z) / 3f;
            gizmo.SetUniformScale(current + input * keyboardScaleSpeed * Time.deltaTime);
        }
    }

    // W/S changes depth - away from (W) / towards (S) the camera - whenever something is selected, independent of the
    // gizmo's move-tool axis buttons. Skipped while an explicit Y or Z axis nudge (chosen
    // via the action panel) is already driving W/S, so the two systems don't double up.
    void HandleDepthNudge()
    {
        if (selected == null) return;
        if (gizmo.Mode == GizmoMode.Move && gizmo.ActiveAxis.HasValue && gizmo.ActiveAxis.Value != GizmoAxis.X)
            return;

        float input = 0f;
        if (Input.GetKey(KeyCode.S)) input -= 1f;
        if (Input.GetKey(KeyCode.W)) input += 1f;

        if (Mathf.Abs(input) > 0f)
            selected.transform.position += mainCamera.transform.forward * input * keyboardDepthSpeed * Time.deltaTime;
    }

    void TrySelect()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        LayerMask combined = placedObjectLayer | gizmo.gizmoLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, combined))
        {
            // Clicked a gizmo handle - let GizmoHandle's own OnMouseDown/OnMouseDrag
            // run the drag. Don't touch selection or the click will deselect the
            // object out from under the handle before the drag can start.
            if (hit.collider.GetComponent<GizmoHandle>() != null)
                return;

            var placeable = hit.collider.GetComponentInParent<PlaceableObject>();
            if (placeable != null)
            {
                Select(placeable);
                BeginDrag(placeable.transform);
                return;
            }
        }
        Deselect();
    }

    // Free-drag directly on the placed object - slides it across the screen (camera-facing
    // plane through the object), so its distance from the camera stays exactly what it was
    // when the drag started. Depth is changed with W/S instead.
    void BeginDrag(Transform target)
    {
        dragPlane = new Plane(-mainCamera.transform.forward, target.position);
        dragStartOffset = target.position - RaycastDragPlane();
        isDragging = true;
    }

    void ContinueDrag()
    {
        if (gizmo.Target == null) { isDragging = false; return; }

        gizmo.Target.position = RaycastDragPlane() + dragStartOffset;
    }

    Vector3 RaycastDragPlane()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        dragPlane.Raycast(ray, out float enter);
        return ray.GetPoint(enter);
    }

    // Public so palette icons (PaletteSlotUI) can select an already-placed object too.
    public void Select(PlaceableObject obj)
    {
        selected = obj;
        gizmo.SetTarget(obj.transform);
        if (enableHighlightOnSelect && highlighter != null) highlighter.Highlight(obj);
        OnSelectionChanged?.Invoke(selected);
    }

    // Public so MainManager can clear the selection on confirm (keeps the gizmo out of
    // the reconstruction screenshot) and palette icons/other UI can deselect too.
    public void Deselect()
    {
        if (selected == null) return;
        selected = null;
        isDragging = false;
        gizmo.SetTarget(null);
        if (highlighter != null) highlighter.ClearHighlight();
        OnSelectionChanged?.Invoke(null);
    }

    // Public so the left-side action panel's Reset button can call it directly.
    public void DeleteSelected()
    {
        if (selected == null) return;
        palette.ReturnItem(selected);
        if (highlighter != null) highlighter.ClearHighlight();
        Destroy(selected.gameObject);
        selected = null;
        isDragging = false;
        gizmo.SetTarget(null);
        OnSelectionChanged?.Invoke(null);
    }
}
