using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[Serializable]
public class SceneSettings
{
    public string sceneName;
    public bool include = true;
    // Camera field of view: low = zoomed in, high = zoomed out.
    public float originalFieldOfView = 60f;       // while the original scene is shown
    public float reconstructionFieldOfView = 60f; // while the participant reconstructs
}

[Serializable]
public class SettingsData
{
    public List<SceneSettings> scenes = new();
    public List<string> sceneOrder = new();  // scene names in the order the researcher set
    // true: timed black screen, false: waits for the continue key. Indexed by trial POSITION
    // (0 = first scene = practice, 1 = second scene), not by scene.
    public List<bool> blackScreenFixedByPosition = new();
    public string language = "";     // locale code, e.g. "nl"; empty = keep the project's default language
    public float blackScreenSeconds; // 0 = not set yet, MainManager's default is used
    public float practiceBlackScreenSeconds; // 0 = not set yet, the general black screen seconds are used
    public float originalSeconds;    // 0 = not set yet, MainManager's default is used
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

    // At most this many scenes can be ticked "include": the first one is the practice scene,
    // the second the data scene.
    public const int MaxIncludedScenes = 2;

    // All scenes, in the order the researcher set. Scenes without a saved position (new
    // ones) come last in their natural order.
    public static List<string> GetOrderedScenes(List<string> available)
    {
        EnsureLoaded();
        var ordered = new List<string>();
        foreach (string name in data.sceneOrder)
            if (available.Contains(name) && !ordered.Contains(name)) ordered.Add(name);
        foreach (string name in available)
            if (!ordered.Contains(name)) ordered.Add(name);
        return ordered;
    }

    // Moves a scene up (delta -1) or down (+1) in the order.
    public static void MoveScene(List<string> available, string sceneName, int delta)
    {
        var ordered = GetOrderedScenes(available);
        int index = ordered.IndexOf(sceneName);
        int target = index + delta;
        if (index < 0 || target < 0 || target >= ordered.Count) return;

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        data.sceneOrder = ordered;
    }

    // Keeps at most MaxIncludedScenes ticked; the first ones in order win.
    public static void EnforceMaxIncluded(List<string> orderedScenes)
    {
        int ticked = 0;
        foreach (string name in orderedScenes)
        {
            SceneSettings settings = Get(name);
            if (settings.include && ++ticked > MaxIncludedScenes) settings.include = false;
        }
    }

    public static int CountIncluded(List<string> scenes)
    {
        int count = 0;
        foreach (string name in scenes)
            if (Get(name).include) count++;
        return count;
    }

    // The scenes to run, in order. The first is the practice scene.
    public static List<string> GetScenesToRun(List<string> available)
    {
        var ordered = GetOrderedScenes(available);
        EnforceMaxIncluded(ordered);

        var result = new List<string>();
        foreach (string name in ordered)
            if (Get(name).include) result.Add(name);
        return result;
    }

    // Whether the trial at this position (0 = practice) uses a timed black screen (true)
    // or waits for the continue key (false).
    public static bool GetFixedBlackScreen(int position)
    {
        EnsureLoaded();
        return position < 0
            || position >= data.blackScreenFixedByPosition.Count
            || data.blackScreenFixedByPosition[position];
    }

    public static void SetFixedBlackScreen(int position, bool fixedTime)
    {
        EnsureLoaded();
        while (data.blackScreenFixedByPosition.Count <= position)
            data.blackScreenFixedByPosition.Add(true);
        data.blackScreenFixedByPosition[position] = fixedTime;
    }

    // Length of the timed black screen after the data scene's original image.
    public static float GetBlackScreenSeconds(float fallback)
    {
        EnsureLoaded();
        return data.blackScreenSeconds > 0f ? data.blackScreenSeconds : fallback;
    }

    public static void SetBlackScreenSeconds(float seconds)
    {
        EnsureLoaded();
        data.blackScreenSeconds = seconds;
    }

    // Length of the timed black screen in the first (practice) scene. Falls back to the
    // general value until it has been set.
    public static float GetPracticeBlackScreenSeconds(float fallback)
    {
        EnsureLoaded();
        return data.practiceBlackScreenSeconds > 0f ? data.practiceBlackScreenSeconds : fallback;
    }

    public static void SetPracticeBlackScreenSeconds(float seconds)
    {
        EnsureLoaded();
        data.practiceBlackScreenSeconds = seconds;
    }

    // How long the original image is shown, for every scene.
    public static float GetOriginalSeconds(float fallback)
    {
        EnsureLoaded();
        return data.originalSeconds > 0f ? data.originalSeconds : fallback;
    }

    public static void SetOriginalSeconds(float seconds)
    {
        EnsureLoaded();
        data.originalSeconds = seconds;
    }

    // Switches the active language and remembers it for next time.
    public static void SetLanguage(Locale locale)
    {
        EnsureLoaded();
        data.language = locale.Identifier.Code;
        LocalizationSettings.SelectedLocale = locale;
    }

    // Re-applies the remembered language. Call once Localization has finished initialising.
    public static void ApplySavedLanguage()
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(data.language)) return;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == data.language)
            {
                LocalizationSettings.SelectedLocale = locale;
                return;
            }
        }
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
        data.sceneOrder ??= new List<string>();
        data.blackScreenFixedByPosition ??= new List<bool>();
    }
}
