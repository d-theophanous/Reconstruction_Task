using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public Camera mainCamera => MainManager.Instance.CurCamera;
    public LayerMask placedObjectLayer;
    public TransformGizmo gizmo;
    public PaletteManager palette;

    [Tooltip("Units per second when nudging the selected axis with the keyboard.")]
    public float keyboardMoveSpeed = 2f;

    [Tooltip("Units per second when scaling with the keyboard (D = up, A = down).")]
    public float keyboardScaleSpeed = 1f;

    public PlaceableObject Selected => selected;

    // Fired whenever the selection changes - passes null on deselect.
    // ActionPanelUI subscribes to this to enable/disable its buttons.
    public event Action<PlaceableObject> OnSelectionChanged;

    private PlaceableObject selected;

    void Update()
    {
        // W = move tool, R = scale tool (same shortcuts as the Unity editor)
        if (Input.GetKeyDown(KeyCode.W)) gizmo.SetMode(GizmoMode.Move);
        if (Input.GetKeyDown(KeyCode.R)) gizmo.SetMode(GizmoMode.Scale);

        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            DeleteSelected();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            TrySelect();

        HandleAxisNudge();
        HandleScaleNudge();
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

    void HandleScaleNudge()
    {
        // Additional to dragging the yellow scale handle with the mouse.
        if (gizmo.Mode != GizmoMode.Scale) return;
        if (gizmo.Target == null) return;

        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f; // scale down
        if (Input.GetKey(KeyCode.D)) input += 1f; // scale up

        if (Mathf.Abs(input) > 0f)
        {
            Vector3 scale = gizmo.Target.localScale;
            float current = (scale.x + scale.y + scale.z) / 3f;
            float newValue = Mathf.Max(0.05f, current + input * keyboardScaleSpeed * Time.deltaTime);
            gizmo.Target.localScale = Vector3.one * newValue;
        }
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
                return;
            }
        }
        Deselect();
    }

    // Public so palette icons (PaletteSlotUI) can select an already-placed object too.
    public void Select(PlaceableObject obj)
    {
        selected = obj;
        gizmo.SetTarget(obj.transform);
        OnSelectionChanged?.Invoke(selected);
    }

    void Deselect()
    {
        if (selected == null) return;
        selected = null;
        gizmo.SetTarget(null);
        OnSelectionChanged?.Invoke(null);
    }

    // Public so the left-side action panel's Reset button can call it directly.
    public void DeleteSelected()
    {
        if (selected == null) return;
        palette.ReturnItem(selected);
        Destroy(selected.gameObject);
        selected = null;
        gizmo.SetTarget(null);
        OnSelectionChanged?.Invoke(null);
    }
}
