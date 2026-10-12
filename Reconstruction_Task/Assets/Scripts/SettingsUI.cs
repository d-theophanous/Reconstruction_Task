using UnityEngine;

// The experimenter's settings menu. Builds one row per trial scene (practice scene first,
// then MainManager.SceneNameList) from a row prefab and saves on Close.
public class SettingsUI : MonoBehaviour
{
    [Tooltip("The panel to show/hide. Can be this same GameObject.")]
    public GameObject panel;
    [Tooltip("ScrollView > Viewport > Content - rows get created under this.")]
    public Transform rowContainer;
    public SettingsRowUI rowPrefab;

    // Wire the main menu's Settings button to this.
    public void Open()
    {
        panel.SetActive(true);
        Rebuild();
    }

    // Wire the settings panel's Close button to this.
    public void Close()
    {
        SettingsStore.Save();
        panel.SetActive(false);
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
