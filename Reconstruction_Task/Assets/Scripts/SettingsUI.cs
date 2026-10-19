using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

// The experimenter's settings menu. Builds one row per scene in MainManager.SceneNameList
// (in the order the researcher set; at most two can be included, the first is the practice
// scene) from a row prefab, offers the global settings (language, durations) and saves on Close.
public class SettingsUI : MonoBehaviour
{
    [Tooltip("The panel to show/hide. Can be this same GameObject.")]
    public GameObject panel;
    [Tooltip("ScrollView > Viewport > Content - rows get created under this.")]
    public Transform rowContainer;
    public SettingsRowUI rowPrefab;

    [Header("Global settings")]
    [Tooltip("Lists every locale registered in the Localization Settings.")]
    public TMP_Dropdown languageDropdown;
    [Tooltip("Seconds the timed black screen stays in the second (data) scene.")]
    public TMP_InputField blackScreenSecondsInput;
    [Tooltip("Seconds the timed black screen stays in the first (practice) scene.")]
    public TMP_InputField practiceBlackScreenSecondsInput;
    [Tooltip("Seconds the original image is shown, for every scene.")]
    public TMP_InputField originalSecondsInput;

    public bool IsOpen => panel.activeSelf;

    // Wire the main menu's Settings button to this.
    public void Open()
    {
        panel.SetActive(true);
        Refresh();
        SetupLanguageDropdown();
        SetupSecondsInputs();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // Wire the settings panel's Quit button to this. Saves the settings first.
    public void QuitApplication()
    {
        SettingsStore.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Wire the settings panel's Close button to this.
    public void Close()
    {
        SettingsStore.Save();
        panel.SetActive(false);
    }

    void SetupLanguageDropdown()
    {
        if (languageDropdown == null) return;

        // The locale list isn't available until Localization has initialised.
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone)
        {
            init.Completed += _ => SetupLanguageDropdown();
            return;
        }

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var names = new List<string>();
        int selected = 0;
        for (int i = 0; i < locales.Count; i++)
        {
            names.Add(locales[i].LocaleName);
            if (locales[i] == LocalizationSettings.SelectedLocale) selected = i;
        }

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(names);
        languageDropdown.SetValueWithoutNotify(selected);
        languageDropdown.onValueChanged.AddListener(index => SettingsStore.SetLanguage(locales[index]));
    }

    void SetupSecondsInputs()
    {
        var main = MainManager.Instance;
        SetupSecondsInput(blackScreenSecondsInput,
            () => SettingsStore.GetBlackScreenSeconds(main.BlackScreenWaitTime), SettingsStore.SetBlackScreenSeconds);
        SetupSecondsInput(practiceBlackScreenSecondsInput,
            () => SettingsStore.GetPracticeBlackScreenSeconds(SettingsStore.GetBlackScreenSeconds(main.BlackScreenWaitTime)),
            SettingsStore.SetPracticeBlackScreenSeconds);
        SetupSecondsInput(originalSecondsInput,
            () => SettingsStore.GetOriginalSeconds(main.OriginalSceneWaitTime), SettingsStore.SetOriginalSeconds);
    }

    // A decimal input for a number of seconds. Accepts "2.5" and "2,5". Anything that isn't
    // a number above 0 is ignored and the field goes back to the value currently in use.
    static void SetupSecondsInput(TMP_InputField input, Func<float> getSeconds, Action<float> setSeconds)
    {
        if (input == null) return;

        void Show() => input.SetTextWithoutNotify(getSeconds().ToString("0.##", CultureInfo.InvariantCulture));

        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(text =>
        {
            if (float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds)
                && seconds > 0f)
            {
                setSeconds(seconds);
            }
            Show();
        });
        Show();
    }

    // Used by the rows.
    public bool CanIncludeAnother() =>
        SettingsStore.CountIncluded(MainManager.Instance.SceneNameList) < SettingsStore.MaxIncludedScenes;

    public void MoveScene(string sceneName, int delta)
    {
        SettingsStore.MoveScene(MainManager.Instance.SceneNameList, sceneName, delta);
        Refresh();
    }

    // Rebuilds the rows, in the order the researcher set. The first included scene is the
    // practice scene and the included scenes' positions decide their black screen mode.
    public void Refresh()
    {
        foreach (Transform child in rowContainer)
        {
            child.gameObject.SetActive(false); // gone from the layout right away, destroyed at frame end
            Destroy(child.gameObject);
        }

        var zoom = CameraZoomController.Instance;
        float minFov = zoom != null ? zoom.minFieldOfView : 20f;
        float maxFov = zoom != null ? zoom.maxFieldOfView : 80f;

        List<string> ordered = SettingsStore.GetOrderedScenes(MainManager.Instance.SceneNameList);
        SettingsStore.EnforceMaxIncluded(ordered);

        int position = 0;
        foreach (string sceneName in ordered)
        {
            SceneSettings settings = SettingsStore.Get(sceneName);
            int trialPosition = settings.include ? position++ : -1;
            string label = trialPosition == 0 ? $"{sceneName} (practice)" : sceneName;
            Instantiate(rowPrefab, rowContainer).Bind(label, settings, trialPosition, minFov, maxFov, this);
        }
    }
}
