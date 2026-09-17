using UnityEngine;

public enum GizmoAxis { X, Y, Z, Uniform }
public enum GizmoHandleType { Move, Scale }

// The move-axis handles (X/Y/Z) and their drag-to-move logic were removed - movement now
// happens via free mouse drag (X/Y) and the W/S depth key (SelectionManager). Only the
// uniform scale handle uses this component now.
public class GizmoHandle : MonoBehaviour
{
    public GizmoAxis axis;
    public GizmoHandleType type;
    public TransformGizmo gizmo;

    static readonly Vector3 UniformDir = new Vector3(1f, 1f, 1f).normalized;

    Plane dragPlane;
    Vector3 startTargetPos;
    float startUniformScale;
    float startProjection;

    // OnMouseDown/OnMouseDrag are built-in Unity messages - they fire automatically
    // for any GameObject with a Collider when the main camera sees it clicked/dragged.
    void OnMouseDown()
    {
        if (gizmo.Target == null) return;

        Vector3 planeOrigin = transform.position;
        Vector3 toCamera = Camera.main.transform.position - planeOrigin;
        Vector3 normal = Vector3.Cross(Vector3.Cross(UniformDir, toCamera), UniformDir).normalized;
        if (normal.sqrMagnitude < 0.0001f) normal = Camera.main.transform.forward;

        dragPlane = new Plane(normal, planeOrigin);
        startTargetPos = gizmo.Target.position;
        Vector3 s = gizmo.Target.localScale;
        startUniformScale = (s.x + s.y + s.z) / 3f;
        startProjection = ProjectOnAxis(RaycastPlane(), startTargetPos, UniformDir);
    }

    void OnMouseDrag()
    {
        if (gizmo.Target == null) return;

        float currentProjection = ProjectOnAxis(RaycastPlane(), startTargetPos, UniformDir);
        float delta = currentProjection - startProjection;
        gizmo.SetUniformScale(startUniformScale + delta);
    }

    Vector3 RaycastPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        dragPlane.Raycast(ray, out float enter);
        return ray.GetPoint(enter);
    }

    static float ProjectOnAxis(Vector3 point, Vector3 axisOrigin, Vector3 axisDir)
    {
        return Vector3.Dot(point - axisOrigin, axisDir);
    }
}
