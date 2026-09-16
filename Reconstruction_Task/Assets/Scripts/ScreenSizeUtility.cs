using UnityEngine;

// Measures how big an object actually looks on screen. World-space scale alone doesn't
// tell you this once the camera can zoom (field of view changes apparent size too), so
// this is recorded alongside Position/Scale for later analysis.
public static class ScreenSizeUtility
{
    public static Vector2 GetScreenSpaceSize(Renderer renderer, Camera cam)
    {
        if (renderer == null || cam == null) return Vector2.zero;

        Bounds bounds = renderer.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector2 screenMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 screenMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);

            Vector3 screenPoint = cam.WorldToScreenPoint(corner);
            screenMin = Vector2.Min(screenMin, screenPoint);
            screenMax = Vector2.Max(screenMax, screenPoint);
        }

        return screenMax - screenMin;
    }
}
