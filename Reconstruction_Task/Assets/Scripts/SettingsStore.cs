using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SceneSettings
{
    public string sceneName;
    public bool include = true;
    public float fieldOfView = 60f;         // camera field of view: low = zoomed in, high = zoomed out
    public bool blackScreenFixedTime = true; // true: timed black screen, false: waits for the continue key
}

[Serializable]
public class SettingsData
{
    public List<SceneSettings> scenes = new();
}

// Per-scene experiment settings, saved as JSON in persistentDataPath so they survive
// restarts and work in a built player (nothing here is Editor-only).
public static class SettingsStore
{
    static SettingsData data;

    public static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");

    // Returns the live settings object for a scene (created with defaults if it's new).
    // Edits to it are used immediately; call Save() to persist them to disk.
    public static SceneSettings Get(string sceneName)
    {
        EnsureLoaded();
        var settings = data.scenes.Find(s => s.sceneName == sceneName);
        if (settings == null)
        {
            settings = new SceneSettings { sceneName = sceneName };
            data.scenes.Add(settings);
        }
        return settings;
    }

    public static void Save()
    {
        EnsureLoaded();
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
    }

    static void EnsureLoaded()
    {
        if (data != null) return;

        data = new SettingsData();
        try
        {
            if (File.Exists(FilePath))
                data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SettingsStore: couldn't read {FilePath}, using defaults. {e.Message}");
        }
        data.scenes ??= new List<SceneSettings>();
    }
}
