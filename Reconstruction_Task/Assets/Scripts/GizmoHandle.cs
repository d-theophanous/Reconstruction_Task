using UnityEngine;

public enum GizmoAxis { X, Y, Z, Uniform }
public enum GizmoHandleType { Move, Scale }

public class GizmoHandle : MonoBehaviour
{
    public GizmoAxis axis;
    public GizmoHandleType type;
    public TransformGizmo gizmo;

    Vector3 AxisDir
    {
        get
        {
            switch (axis)
            {
                case GizmoAxis.X: return Vector3.right;
                case GizmoAxis.Y: return Vector3.up;
                case GizmoAxis.Z: return Vector3.forward;
                default: return new Vector3(1f, 1f, 1f).normalized; // Uniform scale handle
            }
        }
    }

    Plane dragPlane;
    Vector3 startTargetPos;
    float startUniformScale;
    float startProjection;

    // OnMouseDown/OnMouseDrag are built-in Unity messages - they fire automatically
    // for any GameObject with a Collider when the main camera sees it clicked/dragged.
    void OnMouseDown()
    {
        if (gizmo.Target == null) return;

        Vector3 axisWorld = AxisDir;
        Vector3 planeOrigin = transform.position;
        Vector3 toCamera = Camera.main.transform.position - planeOrigin;
        Vector3 normal = Vector3.Cross(Vector3.Cross(axisWorld, toCamera), axisWorld).normalized;
        if (normal.sqrMagnitude < 0.0001f) normal = Camera.main.transform.forward;

        dragPlane = new Plane(normal, planeOrigin);
        startTargetPos = gizmo.Target.position;
        Vector3 s = gizmo.Target.localScale;
        startUniformScale = (s.x + s.y + s.z) / 3f;
        startProjection = ProjectOnAxis(RaycastPlane(), startTargetPos, axisWorld);
    }

    void OnMouseDrag()
    {
        if (gizmo.Target == null) return;

        Vector3 axisWorld = AxisDir;
        float currentProjection = ProjectOnAxis(RaycastPlane(), startTargetPos, axisWorld);
        float delta = currentProjection - startProjection;

        if (type == GizmoHandleType.Move)
        {
            gizmo.Target.position = startTargetPos + axisWorld * delta;
        }
        else
        {
            // Uniform scale - all three axes move together.
            gizmo.SetUniformScale(startUniformScale + delta);
        }
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
