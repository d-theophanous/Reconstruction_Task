using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Thin Grass
// Randomly removes a share of the single grass objects from scenes, to make them lighter to
// render. The same seed picks the same objects in scenes with the same layout, so scenes
// that start out alike stay alike.
public class GrassThinningWindow : EditorWindow
{
    public string prefabNameFilter = "UNS_Grass";
    public float removePercent = 50f;
    public int seed = 12345;
    public List<SceneAsset> scenes = new List<SceneAsset>();

    [MenuItem("Tools/Thin Grass")]
    static void Open() => GetWindow<GrassThinningWindow>("Thin Grass");

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Removes the chosen share of grass objects (prefab instances whose prefab name contains the " +
            "text below). Every run removes that share of what is LEFT, so run it once per scene. " +
            "Commit your project in git first - this edits and saves scene files.",
            MessageType.Warning);

        prefabNameFilter = EditorGUILayout.TextField("Prefab name contains", prefabNameFilter);
        removePercent = EditorGUILayout.Slider("Remove (%)", removePercent, 0f, 90f);
        seed = EditorGUILayout.IntField("Random seed", seed);

        EditorGUILayout.Space();
        if (GUILayout.Button("Thin grass in the open scene (not saved)"))
            ThinOpenScene();

        EditorGUILayout.Space();
        var serialized = new SerializedObject(this);
        EditorGUILayout.PropertyField(serialized.FindProperty("scenes"), new GUIContent("Scenes"), true);
        serialized.ApplyModifiedProperties();

        if (GUILayout.Button("Thin grass in the scenes above (opens and saves each)"))
            ThinListedScenes();
    }

    void ThinOpenScene()
    {
        if (!CanRun()) return;

        Scene scene = SceneManager.GetActiveScene();
        int total = FindGrass(scene).Count;
        int toRemove = Mathf.RoundToInt(total * removePercent / 100f);
        if (!EditorUtility.DisplayDialog("Thin grass",
                $"'{scene.name}' has {total} grass objects. Remove {toRemove}?\n\nThe scene is not saved automatically.",
                "Remove", "Cancel"))
            return;

        int removed = Thin(scene);
        Debug.Log($"Thin Grass: removed {removed} of {total} grass objects in '{scene.name}'. Save the scene to keep it.");
    }

    void ThinListedScenes()
    {
        if (!CanRun()) return;
        if (scenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Thin grass", "Add at least one scene to the list first.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Thin grass",
                $"This opens, thins and SAVES {scenes.Count} scene(s), removing {removePercent:0}% of their grass.\n\nHave you committed your project in git?",
                "Go ahead", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string originalScenePath = SceneManager.GetActiveScene().path;
        try
        {
            foreach (SceneAsset sceneAsset in scenes)
            {
                if (sceneAsset == null) continue;

                string path = AssetDatabase.GetAssetPath(sceneAsset);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int total = FindGrass(scene).Count;
                int removed = Thin(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Thin Grass: removed {removed} of {total} grass objects in '{scene.name}' and saved it.");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }

    bool CanRun()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;
        EditorUtility.DisplayDialog("Thin grass", "Stop Play mode first.", "OK");
        return false;
    }

    // The outermost root of every prefab instance whose prefab asset name matches the filter.
    List<GameObject> FindGrass(Scene scene)
    {
        var found = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;

                GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                if (source != null && AssetDatabase.GetAssetPath(source).Contains(prefabNameFilter))
                    found.Add(go);
            }
        }
        return found;
    }

    int Thin(Scene scene)
    {
        List<GameObject> grass = FindGrass(scene);
        int remove = Mathf.RoundToInt(grass.Count * removePercent / 100f);

        // Pick `remove` distinct objects at random (partial shuffle), then delete them.
        var random = new System.Random(seed);
        for (int i = 0; i < remove; i++)
        {
            int j = random.Next(i, grass.Count);
            (grass[i], grass[j]) = (grass[j], grass[i]);
        }
        for (int i = 0; i < remove; i++)
            DestroyImmediate(grass[i]);

        if (remove > 0) EditorSceneManager.MarkSceneDirty(scene);
        return remove;
    }
}
