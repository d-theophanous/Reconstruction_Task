using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainManager : Singleton<MainManager>
{
    [Header("UI References")]
    public GameObject BlackBackground;
    public GameObject MenuUI;
    public Button ConfirmButton;
    public GameObject ReconUI;
    public ComparisonUI ComparisonUI;
    public SelectionManager selectionManager;
    public GameObject TutorialScreen;

    public GameObject DropObjectTutorial;
    public GameObject DragObjectTutorial;

    public GameObject ObjectParent;

    [HideInInspector]
    public Camera CurCamera;

    public float OriginalSceneWaitTime;
    public float BlackScreenWaitTime;

    [Header("Trials")]
    [Tooltip("Optional - run through this scene once first, same mechanics, but its data is not saved.")]
    public string PracticeSceneName;
    public List<string> SceneNameList = new();
    public bool SaveTrialScreenshots = true;

    public bool HasAnswered;
    public bool IsTrialRun;

    private Texture2D lastOriginalTex;
    private Texture2D lastReconTex;
    private int trialIndex;

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
        string previousScene = null;

        if (!string.IsNullOrEmpty(PracticeSceneName))
        {
            IsTrialRun = true;
            yield return RunTrial(PracticeSceneName, null, isPractice: true);
            previousScene = PracticeSceneName;
        }

        IsTrialRun = false;
        for (int i = 0; i < SceneNameList.Count; i++)
        {
            yield return RunTrial(SceneNameList[i], previousScene, isPractice: false);
            previousScene = SceneNameList[i];
        }

        if (!string.IsNullOrEmpty(previousScene))
            yield return SceneManager.UnloadSceneAsync(previousScene);

        ErrorManager.Instance.CalculateAndSaveError(SessionInfo.ParticipantId);

        MenuUI.SetActive(true);
    }

    // Runs one full trial: load scene -> show original -> black screen -> reconstruct ->
    // confirm -> comparison screen -> clean up. Shared by the practice run and every real trial.
    private IEnumerator RunTrial(string sceneName, string unloadSceneName, bool isPractice)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (!string.IsNullOrEmpty(unloadSceneName))
            yield return SceneManager.UnloadSceneAsync(unloadSceneName);

        yield return new WaitForSeconds(OriginalSceneWaitTime);

        CleanupTrialTextures();
        lastOriginalTex = CameraCaptureUtility.Capture(CurCamera);
        List<ObjectData> originalData = isPractice ? null : BuildOriginalData(PaletteManager.Instance.CurObjectList);

        BlackBackground.SetActive(true);
        PaletteManager.Instance.CurObjectParent.SetActive(false);
        ReconUI.gameObject.SetActive(true);
        yield return new WaitForSeconds(BlackScreenWaitTime);
        BlackBackground.SetActive(false);

        if (isPractice)
            DropObjectTutorial.SetActive(true);

        while (!HasAnswered)
            yield return null;
        HasAnswered = false;

        lastReconTex = CameraCaptureUtility.Capture(CurCamera);
        List<ObjectData> reconData = isPractice ? null : BuildReconData(ObjectParent, CurCamera);

        if (!isPractice)
        {
            ErrorManager.Instance.FillOriginalDicEntry(sceneName, originalData);
            ErrorManager.Instance.FillReconDicEntry(sceneName, reconData);
        }

        trialIndex++;
        if (SaveTrialScreenshots)
            SaveScreenshots(sceneName, trialIndex, isPractice);

        ReconUI.gameObject.SetActive(false);

        if (ComparisonUI != null)
        {
            bool acknowledged = false;
            string label = isPractice ? "Great! You completed the practice round" : "Trial complete. Thank you for participating!";
            ComparisonUI.Show(lastOriginalTex, lastReconTex, label, () => acknowledged = true);
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
        string prefix = isPractice ? "practice" : $"trial{index}_{sceneName}";
        File.WriteAllBytes(Path.Combine(SessionInfo.OutputFolder, $"{prefix}_original.png"), lastOriginalTex.EncodeToPNG());
        File.WriteAllBytes(Path.Combine(SessionInfo.OutputFolder, $"{prefix}_recon.png"), lastReconTex.EncodeToPNG());
    }

    public void SetCurCamera(Camera camera)
    {
        CurCamera = camera;
    }

    public void ConfirmButtonPressed()
    {
        Debug.Log("Confirm pressed");
        // Deselect so the gizmo's colored axis handles aren't visible in the reconstruction screenshot.
        if (selectionManager != null) selectionManager.Deselect();
        HasAnswered = true;
    }
}
