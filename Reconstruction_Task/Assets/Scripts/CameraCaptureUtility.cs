using UnityEngine;

// Renders a camera's current view to an off-screen texture so it can be shown later
// (e.g. side-by-side original-vs-reconstruction comparison) or saved to disk.
public static class CameraCaptureUtility
{
    public static Texture2D Capture(Camera cam, int width = 640)
    {
        if (cam == null) return null;

        float aspect = cam.aspect;
        int height = Mathf.Max(1, Mathf.RoundToInt(width / aspect));

        var renderTexture = new RenderTexture(width, height, 24);
        var previousTarget = cam.targetTexture;
        var previousActive = RenderTexture.active;

        cam.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        cam.Render();

        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        cam.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        Object.Destroy(renderTexture);

        return texture;
    }
}
