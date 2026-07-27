using UnityEngine;

public class CameraZoomController : Singleton<CameraZoomController>
{
    public Camera targetCamera => MainManager.Instance.CurCamera;
    public HoldButton zoomInButton;
    public HoldButton zoomOutButton;

    [Tooltip("Degrees of field of view change per second while a button is held.")]
    public float zoomSpeed = 20f;

    [Tooltip("Smallest field of view allowed (most zoomed in). Zooming in stops here.")]
    public float minFieldOfView = 20f;

    [Tooltip("Largest field of view allowed (most zoomed out). Zooming out stops here.")]
    public float maxFieldOfView = 80f;

    protected override void Update()
    {

        if (targetCamera == null) return;

        float fov = targetCamera.fieldOfView;

        // Zooming in = smaller FOV, zooming out = larger FOV.
        if (zoomInButton != null && zoomInButton.IsHeld)
            fov -= zoomSpeed * Time.deltaTime;

        if (zoomOutButton != null && zoomOutButton.IsHeld)
            fov += zoomSpeed * Time.deltaTime;

        targetCamera.fieldOfView = Mathf.Clamp(fov, minFieldOfView, maxFieldOfView);
    }
    
}
