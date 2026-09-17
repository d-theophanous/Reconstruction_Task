using UnityEngine;

public enum GizmoMode { Move, Scale }

// The move-axis handles (X/Y/Z, drag-to-move) were removed - movement now happens via
// free mouse drag on the object (X/Y) and the W/S depth key (see SelectionManager).
// Only the uniform scale handle is built here now.
public class TransformGizmo : MonoBehaviour
{
    public LayerMask gizmoLayer; // dedicated layer, e.g. "Gizmo" - exclude this from your selection raycast
    public float handleDistance = 1.2f;
    public float handleSize = 0.2f;

    [Tooltip("Shader used for the handle's material. Assign it here (e.g. the built-in 'Unlit/Color' shader) - a shader only looked up via Shader.Find at runtime isn't guaranteed to be included in a build, which can make the handle render as a plain white block instead of its real color once built. Falls back to Shader.Find if left empty, which is fine in the Editor but risky in a build.")]
    public Shader handleShader;

    [Header("Scale Limits")]
    [Tooltip("Smallest uniform scale a placed object can be shrunk to.")]
    public float minScale = 0.05f;
    [Tooltip("Largest uniform scale a placed object can be grown to.")]
    public float maxScale = 5f;

    public Transform Target { get; private set; }
    public GizmoMode Mode { get; private set; } = GizmoMode.Move;

    // Kept for SelectionManager's keyboard axis-nudge system - no longer has any visual
    // representation now that the move handles are gone, but still tracked.
    public GizmoAxis? ActiveAxis { get; private set; }

    GameObject scaleRoot;

    void Awake()
    {
        scaleRoot = BuildScaleHandle();
        gameObject.SetActive(false);
        Refresh();
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
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = $"{type}_{axis}";
        Destroy(handle.GetComponent<Collider>());

        handle.transform.SetParent(parent, false);
        handle.transform.localPosition = dir * handleDistance;
        handle.transform.localScale = Vector3.one * handleSize * 1.4f;

        handle.layer = LayerMaskToLayer(gizmoLayer);
        var rend = handle.GetComponent<Renderer>();
        Shader shader = handleShader != null ? handleShader : Shader.Find("Unlit/Color");
        rend.material = new Material(shader) { color = color };

        var collider = handle.AddComponent<BoxCollider>();
        collider.size = Vector3.one * 1.6f; // a bit larger than the mesh - easier to grab with the mouse

        var gh = handle.AddComponent<GizmoHandle>();
        gh.axis = axis;
        gh.type = type;
        gh.gizmo = this;
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

    public void SetActiveAxis(GizmoAxis? axis)
    {
        ActiveAxis = axis;
    }

    // Applies a uniform scale to the current target, clamped to [minScale, maxScale].
    // Shared by the keyboard scale nudge and the mouse-dragged scale handle so the
    // limit can't be bypassed by either input method.
    public void SetUniformScale(float value)
    {
        if (Target == null) return;
        Target.localScale = Vector3.one * Mathf.Clamp(value, minScale, maxScale);
    }

    void Refresh()
    {
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
