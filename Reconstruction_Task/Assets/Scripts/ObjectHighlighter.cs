using System.Collections.Generic;
using UnityEngine;

// Tints an object's renderers toward highlightColor and can restore them again.
// Uses material.color (not emission) so it works regardless of which shader the
// placed-object prefabs use.
public class ObjectHighlighter : MonoBehaviour
{
    public Color highlightColor = Color.yellow;
    [Range(0f, 1f)] public float highlightStrength = 0.6f;

    private readonly Dictionary<Renderer, Color> originalColors = new();
    private PlaceableObject current;

    public void Highlight(PlaceableObject obj)
    {
        if (current == obj) return;
        ClearHighlight();
        if (obj == null) return;

        current = obj;
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            if (!rend.material.HasProperty("_Color")) continue;
            originalColors[rend] = rend.material.color;
            rend.material.color = Color.Lerp(rend.material.color, highlightColor, highlightStrength);
        }
    }

    public void ClearHighlight()
    {
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null) kvp.Key.material.color = kvp.Value;
        }
        originalColors.Clear();
        current = null;
    }
}
