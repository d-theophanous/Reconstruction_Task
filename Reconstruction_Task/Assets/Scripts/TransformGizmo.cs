using System.Collections.Generic;
using UnityEngine;

public enum GizmoMode { Move, Scale }

public class TransformGizmo : MonoBehaviour
{
    public LayerMask gizmoLayer; // dedicated layer, e.g. "Gizmo" - exclude this from your selection raycast
    public float handleDistance = 1.2f;
    public float handleSize = 0.2f;

    [Header("Scale Limits")]
    [Tooltip("Smallest uniform scale a placed object can be shrunk to.")]
    public float minScale = 0.05f;
    [Tooltip("Largest uniform scale a placed object can be grown to.")]
    public float maxScale = 5f;

    public Transform Target { get; private set; }
    public GizmoMode Mode { get; private set; } = GizmoMode.Move;
    public GizmoAxis? ActiveAxis { get; private set; } // currently "selected" move axis, used for keyboard nudging

    GameObject moveRoot;
    GameObject scaleRoot;

    readonly Dictionary<GizmoAxis, Renderer> moveHandleRenderers = new Dictionary<GizmoAxis, Renderer>();
    readonly Dictionary<GizmoAxis, Color> moveHandleBaseColors = new Dictionary<GizmoAxis, Color>();
    static readonly Color ActiveHighlightColor = Color.white;

    void Awake()
    {
        moveRoot = BuildMoveHandles();
        scaleRoot = BuildScaleHandle();
        gameObject.SetActive(false);
        Refresh();
    }

    GameObject BuildMoveHandles()
    {
        var root = new GameObject("MoveHandles");
        root.transform.SetParent(transform, false);

        CreateHandle(root.transform, GizmoAxis.X, Vector3.right, Color.red, GizmoHandleType.Move);
        CreateHandle(root.transform, GizmoAxis.Y, Vector3.up, Color.green, GizmoHandleType.Move);
        CreateHandle(root.transform, GizmoAxis.Z, Vector3.forward, Color.blue, GizmoHandleType.Move);
        return root;
    }

    GameObject BuildScaleHandle()
    {
        var root = new GameObject("ScaleHandle");
        root.transform.SetParent(transform, false);

        // Single handle, positioned diagonally - dragging it scales all three axes together.
        CreateHandle(root.transform, GizmoAxis.Uniform, new Vector3(1f, 1f, 1f).normalized, Color.yellow, GizmoHandleType.Scale);
        return root;
    }

    void CreateHandle(Transform parent, GizmoAxis axis, Vector3 dir, Color color, GizmoHandleType type)
    {
        GameObject handle = GameObject.CreatePrimitive(
            type == GizmoHandleType.Move ? PrimitiveType.Cylinder : PrimitiveType.Cube);
        handle.name = $"{type}_{axis}";
        Destroy(handle.GetComponent<Collider>());

        handle.transform.SetParent(parent, false);
        handle.transform.localPosition = dir * handleDistance;

        if (type == GizmoHandleType.Move)
        {
            handle.transform.localScale = new Vector3(handleSize * 0.4f, handleDistance * 0.5f, handleSize * 0.4f);
            handle.transform.up = dir; // orient the cylinder shaft along its axis
        }
        else
        {
            handle.transform.localScale = Vector3.one * handleSize * 1.4f; // slightly bigger - it's the only scale handle
        }

        handle.layer = LayerMaskToLayer(gizmoLayer);
        var rend = handle.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };

        var collider = handle.AddComponent<BoxCollider>();
        collider.size = Vector3.one * 1.6f; // a bit larger than the mesh - easier to grab with the mouse

        var gh = handle.AddComponent<GizmoHandle>();
        gh.axis = axis;
        gh.type = type;
        gh.gizmo = this;

        if (type == GizmoHandleType.Move)
        {
            moveHandleRenderers[axis] = rend;
            moveHandleBaseColors[axis] = color;
        }
    }

    public void SetTarget(Transform target)
    {
        Target = target;
        gameObject.SetActive(target != null);
        if (target != null) transform.position = target.position;
        SetActiveAxis(null);
        Refresh();
    }

    public void SetMode(GizmoMode mode)
    {
        Mode = mode;
        if (mode == GizmoMode.Scale) SetActiveAxis(null); // keyboard nudging only applies to move
        Refresh();
    }

    // Applies a uniform scale to the current target, clamped to [minScale, maxScale].
    // Shared by the keyboard scale nudge and the mouse-dragged scale handle so the
    // limit can't be bypassed by either input method.
    public void SetUniformScale(float value)
    {
        if (Target == null) return;
        Target.localScale = Vector3.one * Mathf.Clamp(value, minScale, maxScale);
    }

    public void SetActiveAxis(GizmoAxis? axis)
    {
        ActiveAxis = axis;
        foreach (var kvp in moveHandleRenderers)
        {
            kvp.Value.material.color = (axis.HasValue && kvp.Key == axis.Value)
                ? ActiveHighlightColor
                : moveHandleBaseColors[kvp.Key];
        }
    }

    void Refresh()
    {
        moveRoot.SetActive(Mode == GizmoMode.Move);
        scaleRoot.SetActive(Mode == GizmoMode.Scale);
    }

    void LateUpdate()
    {
        if (Target != null)
            transform.position = Target.position;
    }

    static int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int layerValue = mask.value;
        while (layerValue > 1) { layerValue >>= 1; layerNumber++; }
        return layerNumber;
    }
}
