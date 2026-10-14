using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Tools > Icon Generator
// Renders each selected prefab/model with a temporary camera on a solid background and
// saves it as a PNG sprite, ready to use as a PlaceableItemDefinition icon.
public class IconGeneratorWindow : EditorWindow
{
    // Far from anything in the open scene, so only the object being rendered is in view.
    static readonly Vector3 FarAway = new Vector3(10000f, 10000f, 10000f);

    string outputFolder = "Assets/Icons And Objects/Generated";
    int size = 512;
    Color backgroundColor = Color.white;
    float padding = 0.1f;
    float pitch = 20f;
    float yaw = -35f;

    [MenuItem("Tools/Icon Generator")]
    static void Open() => GetWindow<IconGeneratorWindow>("Icon Generator");

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Select prefabs or models (or a folder of them) in the Project window, then press Generate.",
            MessageType.Info);

        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);
        size = Mathf.Clamp(EditorGUILayout.IntField("Size (pixels)", size), 64, 2048);
        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
        padding = EditorGUILayout.Slider("Padding", padding, 0f, 0.5f);
        pitch = EditorGUILayout.Slider("Camera pitch (up/down)", pitch, -89f, 89f);
        yaw = EditorGUILayout.Slider("Camera yaw (around)", yaw, -180f, 180f);

        var selected = GetSelectedPrefabs();
        EditorGUILayout.LabelField($"{selected.Count} prefab(s)/model(s) selected");

        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button("Generate icons for selection"))
                Generate(selected);
        }
    }

    static List<GameObject> GetSelectedPrefabs()
    {
        var result = new List<GameObject>();
        foreach (var go in Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets))
        {
            // Skip child nodes inside model files - only the main asset gets an icon.
            if (AssetDatabase.IsMainAsset(go)) result.Add(go);
        }
        return result;
    }

    void Generate(List<GameObject> prefabs)
    {
        if (!outputFolder.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("Icon Generator", "The output folder must be inside Assets.", "OK");
            return;
        }
        Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder)));

        // Lighting is made deterministic while rendering (scene lights off, flat ambient)
        // and put back afterwards.
        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        var lightWasEnabled = new bool[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            lightWasEnabled[i] = lights[i].enabled;
            lights[i].enabled = false;
        }
        var oldAmbientMode = RenderSettings.ambientMode;
        var oldAmbientLight = RenderSettings.ambientLight;
        var oldFog = RenderSettings.fog;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
        RenderSettings.fog = false;

        var created = new List<string>();
        try
        {
            foreach (var prefab in prefabs)
            {
                Texture2D icon = RenderIcon(prefab);
                if (icon == null)
                {
                    Debug.LogWarning($"Icon Generator: '{prefab.name}' has nothing to render (no visible meshes) - skipped.");
                    continue;
                }

                string path = $"{outputFolder}/{Sanitize(prefab.name)}_icon.png";
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)), icon.EncodeToPNG());
                DestroyImmediate(icon);
                created.Add(path);
            }
        }
        finally
        {
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].enabled = lightWasEnabled[i];
            RenderSettings.ambientMode = oldAmbientMode;
            RenderSettings.ambientLight = oldAmbientLight;
            RenderSettings.fog = oldFog;
        }

        AssetDatabase.Refresh();
        foreach (string path in created)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        if (created.Count > 0)
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(created[0]));
        Debug.Log($"Icon Generator: created {created.Count} icon(s) in {outputFolder}.");
    }

    Texture2D RenderIcon(GameObject prefab)
    {
        var instance = Instantiate(prefab);
        GameObject cameraObject = null;
        GameObject lightObject = null;
        RenderTexture big = null;
        RenderTexture small = null;

        try
        {
            MarkHidden(instance);
            instance.SetActive(true);
            instance.transform.position = FarAway;
            foreach (var lod in instance.GetComponentsInChildren<LODGroup>())
                lod.ForceLOD(0);

            if (!TryGetBounds(instance, out Bounds bounds)) return null;

            // Fit the object tightly: measure its bounds as seen from the camera direction.
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 local = inverse * (corner - bounds.center);
                min = Vector2.Min(min, new Vector2(local.x, local.y));
                max = Vector2.Max(max, new Vector2(local.x, local.y));
            }
            Vector2 mid = (min + max) * 0.5f;
            Vector2 half = (max - min) * 0.5f;

            float radius = bounds.extents.magnitude;
            float distance = radius * 3f + 1f;
            Vector3 target = bounds.center + rotation * new Vector3(mid.x, mid.y, 0f);

            cameraObject = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cameraObject.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(half.x, half.y, 0.001f) * (1f + padding);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = distance + radius * 2f + 1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);

            // One key light from the camera's upper left, so every icon is lit the same way.
            lightObject = new GameObject("IconLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = rotation * Quaternion.Euler(35f, -30f, 0f);

            // Render at double size and shrink it, for smooth edges.
            big = RenderTexture.GetTemporary(size * 2, size * 2, 24, RenderTextureFormat.ARGB32);
            small = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            cam.targetTexture = big;
            cam.Render();
            Graphics.Blit(big, small);

            var previous = RenderTexture.active;
            RenderTexture.active = small;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            return texture;
        }
        finally
        {
            if (big != null) RenderTexture.ReleaseTemporary(big);
            if (small != null) RenderTexture.ReleaseTemporary(small);
            if (cameraObject != null) DestroyImmediate(cameraObject);
            if (lightObject != null) DestroyImmediate(lightObject);
            DestroyImmediate(instance);
        }
    }

    static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;

            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }

    // Keeps the temporary copy out of the saved scene (and doesn't mark the scene as changed).
    static void MarkHidden(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
