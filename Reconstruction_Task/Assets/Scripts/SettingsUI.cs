using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

// The experimenter's settings menu. Builds one row per trial scene (practice scene first,
// then MainManager.SceneNameList) from a row prefab, offers the global settings (language,
// timed black screen length) and saves on Close.
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
    [Tooltip("Seconds the black screen stays for every scene that uses a fixed wait time.")]
    public TMP_InputField blackScreenSecondsInput;

    public bool IsOpen => panel.activeSelf;

    // Wire the main menu's Settings button to this.
    public void Open()
    {
        panel.SetActive(true);
        Rebuild();
        SetupLanguageDropdown();
        SetupBlackScreenInput();
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

    void SetupBlackScreenInput()
    {
        if (blackScreenSecondsInput == null) return;

        blackScreenSecondsInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        blackScreenSecondsInput.onEndEdit.RemoveAllListeners();
        ShowBlackScreenSeconds();
        blackScreenSecondsInput.onEndEdit.AddListener(OnBlackScreenSecondsEdited);
    }

    void ShowBlackScreenSeconds()
    {
        float seconds = SettingsStore.GetBlackScreenSeconds(MainManager.Instance.BlackScreenWaitTime);
        blackScreenSecondsInput.SetTextWithoutNotify(seconds.ToString("0.##", CultureInfo.InvariantCulture));
    }

    // Accepts "2.5" and "2,5". Anything that isn't a number above 0 is ignored and the
    // field goes back to the value currently in use.
    void OnBlackScreenSecondsEdited(string text)
    {
        if (float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds)
            && seconds > 0f)
        {
            SettingsStore.SetBlackScreenSeconds(seconds);
        }
        ShowBlackScreenSeconds();
    }

    void Rebuild()
    {
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);

        var zoom = CameraZoomController.Instance;
        float minFov = zoom != null ? zoom.minFieldOfView : 20f;
        float maxFov = zoom != null ? zoom.maxFieldOfView : 80f;

        var main = MainManager.Instance;
        foreach (string sceneName in main.GetTrialSceneNames())
        {
            string label = sceneName == main.PracticeSceneName ? $"{sceneName} (practice)" : sceneName;
            Instantiate(rowPrefab, rowContainer).Bind(label, SettingsStore.Get(sceneName), minFov, maxFov);
        }
    }
}
