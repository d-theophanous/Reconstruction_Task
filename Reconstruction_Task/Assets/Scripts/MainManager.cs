using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainManager : Singleton<MainManager>
{
    [Header("UI References")]
    public GameObject BlackBackground;
    public GameObject MenuUI;
    [Tooltip("The Consent UI - shown again after each finished session so the next participant starts fresh.")]
    public GameObject ConsentScreen;
    public Button ConfirmButton;
    public GameObject ReconUI;
    public ComparisonUI ComparisonUI;
    [Tooltip("Visible while the original image is shown and while the participant reconstructs - hidden during the black screen and after Confirm. Leave it inactive in the scene; this script shows/hides it.")]
    public GameObject TrialUI;
    public SelectionManager selectionManager;
    public GameObject TutorialScreen;

    public GameObject DropObjectTutorial;
    public GameObject DragObjectTutorial;

    public GameObject ObjectParent;

    [HideInInspector]
    public Camera CurCamera;

    [Tooltip("Default length of the original image. The settings menu's original image seconds value overrides it once set.")]
    public float OriginalSceneWaitTime;
    [Tooltip("Default length of a timed black screen. The settings menu's black screen seconds value overrides it once set.")]
    public float BlackScreenWaitTime;

    [Header("Black Screen")]
    [Tooltip("Pressed together with Shift to end the black screen, for scenes whose 'fixed black screen' setting is off (set per scene in the settings menu).")]
    public KeyCode ContinueBlackScreenKey = KeyCode.Space;
    [Tooltip("Text shown on the black screen only while it waits for the key combination (never during a timed black screen). Give it a LocalizeStringEvent for the translated message. Keep it inactive in the scene.")]
    public GameObject BlackScreenContinueText;

    [Header("Comparison Screen Texts")]
    [Tooltip("Shown after the practice round.")]
    public LocalizedString PracticeCompleteText;
    [Tooltip("Shown after each real reconstruction except the last one.")]
    public LocalizedString BetweenTrialsText;
    [Tooltip("Shown after the last reconstruction, i.e. when the whole experiment is finished.")]
    public LocalizedString TrialCompleteText;

    [Header("Settings Menu")]
    public SettingsUI settingsUI;
    [Tooltip("Opens/closes the settings menu at any time, even with an object selected.")]
    public KeyCode SettingsMenuKey = KeyCode.Tab;

    [Header("Trials")]
    [Tooltip("Every scene that can be used. In the settings menu at most two are ticked 'include' and ordered: the first becomes the practice scene (its data is saved too, tagged IsPractice), the second the data scene.")]
    public List<string> SceneNameList = new();
    public bool SaveTrialScreenshots = true;

    public bool HasAnswered;
    public bool IsTrialRun;

    private Texture2D lastOriginalTex;
    private Texture2D lastReconTex;
    private int trialIndex;
    private int realTrialNumber;

    // Zoom (field of view) for the scene that's about to load - applied in SetCurCamera,
    // which the loaded scene's SceneInfoScript calls from Start, so even the very first
    // frame of the original is already at the right zoom.
    private float? pendingFieldOfView;

    // Zoom the original image started at - recorded when the scene's camera is applied.
    private float originalStartFieldOfView;

    protected override void Start()
    {
        StartCoroutine(ApplySavedLanguage());
    }

    // Localization initialises asynchronously, so wait for it before switching language.
    IEnumerator ApplySavedLanguage()
    {
        yield return LocalizationSettings.InitializationOperation;
        SettingsStore.ApplySavedLanguage();
    }

    private int settingsToggleFrame = -1;

    // True while the settings menu is open (and for the frame it was just toggled), so scene
    // input like SelectionManager's keys doesn't react to the same key press.
    public bool SettingsBlockingInput =>
        (settingsUI != null && settingsUI.IsOpen) || Time.frameCount == settingsToggleFrame;

    protected override void Update()
    {
        if (settingsUI == null || !Input.GetKeyDown(SettingsMenuKey) || IsTypingInInputField()) return;

        settingsToggleFrame = Time.frameCount;
        settingsUI.Toggle();
    }

    static bool IsTypingInInputField()
    {
        var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return current != null && current.TryGetComponent(out TMP_InputField input) && input.isFocused;
    }

    public void ShowTutorialInfoScreen()
    {
        MenuUI.gameObject.SetActive(false);
        TutorialScreen.SetActive(true);
    }

    public void StartReconstructionTask()
    {
        TutorialScreen.SetActive(false);
        StartCoroutine(ReconstructionLoopCoroutine());
        MenuUI.gameObject.SetActive(false);
    }

    private IEnumerator ReconstructionLoopCoroutine()
    {
        trialIndex = 0;
        realTrialNumber = 0;
        string previousScene = null;

        // At most two scenes, in the order set in the settings menu. The first is the practice
        // scene, the second the data scene. The black screen mode (timed or waiting for the
        // key) belongs to the position in this sequence, not to a particular scene.
        List<string> scenesToRun = SettingsStore.GetScenesToRun(SceneNameList);
        float generalBlackScreenSeconds = SettingsStore.GetBlackScreenSeconds(BlackScreenWaitTime);

        for (int i = 0; i < scenesToRun.Count; i++)
        {
            bool isPractice = i == 0;
            IsTrialRun = isPractice;

            yield return RunTrial(scenesToRun[i], previousScene, isPractice,
                fixedBlackScreen: SettingsStore.GetFixedBlackScreen(i),
                blackScreenSeconds: isPractice
                    ? SettingsStore.GetPracticeBlackScreenSeconds(generalBlackScreenSeconds)
                    : generalBlackScreenSeconds,
                isLastTrial: i == scenesToRun.Count - 1);
            previousScene = scenesToRun[i];
        }

        IsTrialRun = false;

        if (!string.IsNullOrEmpty(previousScene))
            yield return SceneManager.UnloadSceneAsync(previousScene);

        HideLoadingBlackScreen(); // the last trial left it on after Continue

        ErrorManager.Instance.CalculateAndSaveError(SessionInfo.ParticipantId);

        // Fresh state for the next participant, who starts again at the consent screen.
        ErrorManager.Instance.ResetData();
        SessionInfo.Reset();
        PaletteManager.Instance.ResetTutorial();

        if (ConsentScreen != null)
            ConsentScreen.SetActive(true);
        else
            MenuUI.SetActive(true);
    }

    // Runs one full trial: load scene -> show original -> black screen -> reconstruct ->
    // confirm -> comparison screen -> clean up. Shared by the practice run and every real trial.
    private IEnumerator RunTrial(string sceneName, string unloadSceneName, bool isPractice, bool fixedBlackScreen, float blackScreenSeconds, bool isLastTrial = false)
    {
        SceneSettings settings = SettingsStore.Get(sceneName);
        pendingFieldOfView = settings.originalFieldOfView;

        // Cover the previous scene while the next one loads, so it's never seen lingering.
        ShowLoadingBlackScreen();

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (!string.IsNullOrEmpty(unloadSceneName))
            yield return SceneManager.UnloadSceneAsync(unloadSceneName);

        // One more frame so the new scene's Start() has run (camera, zoom, palette) before
        // the first image is shown.
        yield return null;
        HideLoadingBlackScreen();

        SetTrialUI(true); // the original image is now visible

        yield return new WaitForSeconds(SettingsStore.GetOriginalSeconds(OriginalSceneWaitTime));

        CleanupTrialTextures();
        lastOriginalTex = CameraCaptureUtility.Capture(CurCamera);
        List<ObjectData> originalData = BuildOriginalData(PaletteManager.Instance.CurObjectList);

        SetTrialUI(false); // black screen
        BlackBackground.SetActive(true);
        PaletteManager.Instance.CurObjectParent.SetActive(false);
        ReconUI.gameObject.SetActive(true);

        // Switch to the reconstruction zoom while the screen is black, so there's no visible jump.
        float reconStartFieldOfView = ApplyFieldOfView(CurCamera, settings.reconstructionFieldOfView);

        if (fixedBlackScreen)
        {
            yield return new WaitForSeconds(blackScreenSeconds);
        }
        else
        {
            // Stay black until the experimenter presses Shift + the continue key.
            if (BlackScreenContinueText != null) BlackScreenContinueText.SetActive(true);
            while (!ContinueComboPressed())
                yield return null;
            if (BlackScreenContinueText != null) BlackScreenContinueText.SetActive(false);
        }

        BlackBackground.SetActive(false);
        SetTrialUI(true); // reconstruction starts

        if (isPractice)
            DropObjectTutorial.SetActive(true);

        // Reconstruction timer: starts the moment the empty scene is revealed, ends on confirm.
        float reconstructionStartTime = Time.time;

        while (!HasAnswered)
            yield return null;
        HasAnswered = false;
        SetTrialUI(false); // confirmed

        float reconstructionSeconds = Time.time - reconstructionStartTime;
        float confirmFieldOfView = CurCamera.fieldOfView; // zoom at the moment Confirm was pressed

        lastReconTex = CameraCaptureUtility.Capture(CurCamera);
        List<ObjectData> reconData = BuildReconData(ObjectParent, CurCamera);

        ErrorManager.Instance.AddTrial(sceneName, new TrialRecord
        {
            Original = originalData,
            Recon = reconData,
            IsPractice = isPractice,
            TrialNumber = isPractice ? 0 : ++realTrialNumber,
            ReconstructionSeconds = reconstructionSeconds,
            FixedBlackScreen = fixedBlackScreen,
            OriginalStartFieldOfView = originalStartFieldOfView,
            ReconStartFieldOfView = reconStartFieldOfView,
            ReconConfirmFieldOfView = confirmFieldOfView
        });

        trialIndex++;
        if (SaveTrialScreenshots)
            SaveScreenshots(sceneName, trialIndex, isPractice);

        ReconUI.gameObject.SetActive(false);

        if (ComparisonUI != null)
        {
            bool acknowledged = false;
            LocalizedString label = TitleFor(isPractice, isLastTrial);
            ComparisonUI.Show(lastOriginalTex, lastReconTex, label, () =>
            {
                acknowledged = true;
                ShowLoadingBlackScreen(); // no glimpse of the old scene before the next one loads
            });
            while (!acknowledged)
                yield return null;
        }

        ClearReconstructedObjects();
    }

    List<ObjectData> BuildOriginalData(List<ObjectScript> objects)
    {
        var list = new List<ObjectData>();
        foreach (var obj in objects)
            list.Add(new ObjectData(obj.ObjectType, obj.transform.position, obj.transform.lossyScale));
        return list;
    }

    List<ObjectData> BuildReconData(GameObject parent, Camera camera)
    {
        var list = new List<ObjectData>();
        foreach (var placeable in parent.GetComponentsInChildren<PlaceableObject>())
        {
            var type = PaletteManager.Instance.GetObjectType(placeable.sourceDefinition);
            var renderer = placeable.GetComponentInChildren<Renderer>();
            Vector2 screenSize = renderer != null ? ScreenSizeUtility.GetScreenSpaceSize(renderer, camera) : Vector2.zero;
            list.Add(new ObjectData(type, placeable.transform.position, placeable.transform.lossyScale, screenSize));
        }
        return list;
    }

    // Shift must be held while the continue key is pressed, so a participant can't skip the
    // black screen by tapping Space alone.
    bool ContinueComboPressed() =>
        Input.GetKeyDown(ContinueBlackScreenKey) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

    LocalizedString TitleFor(bool isPractice, bool isLastTrial)
    {
        if (isPractice) return PracticeCompleteText;
        if (isLastTrial) return TrialCompleteText;

        // Until the in-between title is filled in, fall back to the old behaviour.
        return BetweenTrialsText != null && !BetweenTrialsText.IsEmpty ? BetweenTrialsText : TrialCompleteText;
    }

    // Black Background is a child of the Reconstruction UI canvas, so that canvas has to be
    // active for the black screen to be visible at all. Covers everything while a scene loads.
    void ShowLoadingBlackScreen()
    {
        ReconUI.SetActive(true);
        BlackBackground.SetActive(true);
    }

    void HideLoadingBlackScreen()
    {
        BlackBackground.SetActive(false);
        ReconUI.SetActive(false);
    }

    void SetTrialUI(bool visible)
    {
        if (TrialUI != null) TrialUI.SetActive(visible);
    }

    // Destroys every object the player placed this trial, so the next trial starts empty.
    void ClearReconstructedObjects()
    {
        foreach (Transform child in ObjectParent.transform)
            Destroy(child.gameObject);
    }

    void CleanupTrialTextures()
    {
        if (lastOriginalTex != null) Destroy(lastOriginalTex);
        if (lastReconTex != null) Destroy(lastReconTex);
    }

    void SaveScreenshots(string sceneName, int index, bool isPractice)
    {
        Directory.CreateDirectory(SessionInfo.OutputFolder);
        string prefix = $"{SessionInfo.SessionFileTag}_" + (isPractice ? "practice" : $"trial{index}_{sceneName}");
        File.WriteAllBytes(Path.Combine(SessionInfo.OutputFolder, $"{prefix}_original.png"), lastOriginalTex.EncodeToPNG());
        File.WriteAllBytes(Path.Combine(SessionInfo.OutputFolder, $"{prefix}_recon.png"), lastReconTex.EncodeToPNG());
    }

    public void SetCurCamera(Camera camera)
    {
        CurCamera = camera;

        if (pendingFieldOfView.HasValue)
        {
            originalStartFieldOfView = ApplyFieldOfView(camera, pendingFieldOfView.Value);
            pendingFieldOfView = null;
        }
    }

    // Sets the camera's field of view, kept within the Camera Controller's min/max.
    // Returns the value that was actually applied.
    static float ApplyFieldOfView(Camera camera, float fieldOfView)
    {
        if (camera == null) return fieldOfView;

        var zoom = CameraZoomController.Instance;
        camera.fieldOfView = zoom != null
            ? Mathf.Clamp(fieldOfView, zoom.minFieldOfView, zoom.maxFieldOfView)
            : fieldOfView;
        return camera.fieldOfView;
    }

    public void ConfirmButtonPressed()
    {
        Debug.Log("Confirm pressed");
        // Deselect so the gizmo's colored axis handles aren't visible in the reconstruction screenshot.
        if (selectionManager != null) selectionManager.Deselect();
        HasAnswered = true;
    }
}
