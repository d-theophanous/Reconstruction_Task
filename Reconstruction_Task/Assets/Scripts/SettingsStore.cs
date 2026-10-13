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
    public bool blackScreenFixedTime = true; // true: timed black screen, false: waits for the continue key
}

[Serializable]
public class SettingsData
{
    public List<SceneSettings> scenes = new();
    public string language = "";     // locale code, e.g. "nl"; empty = keep the project's default language
    public float blackScreenSeconds; // 0 = not set yet, MainManager's default is used
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

    // Length of the timed black screen, shared by every scene that uses a fixed wait time.
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
    }
}
