using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

    public GameObject ObjectParent;

    [HideInInspector]
    public Camera CurCamera;

    public float OriginalSceneWaitTime;
    public float BlackScreenWaitTime;

    public List<string> SceneNameList = new();

    public bool HasAnswered;

    public void StartReconstructionTask()
    {
        StartCoroutine(ReconstructionLoopCoroutine());
        MenuUI.gameObject.SetActive(false);
    }

    private IEnumerator ReconstructionLoopCoroutine()
    {
        yield return SceneManager.LoadSceneAsync(SceneNameList[0], LoadSceneMode.Additive);

        ErrorManager.Instance.ObjectDataDic.Add(SceneNameList[0], (new(), new()));
        ErrorManager.Instance.FillOriginalDicEntry(SceneNameList[0], 
            GetGameObjectList(PaletteManager.Instance.CurObjectList));

        yield return new WaitForSeconds(OriginalSceneWaitTime);

        BlackBackground.SetActive(true);

        PaletteManager.Instance.CurObjectParent.SetActive(false);
        ReconUI.gameObject.SetActive(true);
        yield return new WaitForSeconds(BlackScreenWaitTime);

        BlackBackground.SetActive(false);

        while (!HasAnswered)
            yield return null;

        List<GameObject> obj_list = new();
        var transform_list = ObjectParent.GetComponentsInChildren<GameObject>().ToList();
        foreach (var obj in transform_list)
            obj_list.Add(obj.gameObject);

        ErrorManager.Instance.FillReconDicEntry(SceneNameList[0], );

        HasAnswered = false;

        for (int i = 1; i < SceneNameList.Count; i++)
        {
            BlackBackground.SetActive(true);

            ReconUI.gameObject.SetActive(false);
            SceneManager.LoadSceneAsync(SceneNameList[i], LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync(SceneNameList[i-1]);
            yield return new WaitForSeconds(BlackScreenWaitTime);

            BlackBackground.SetActive(false);

            yield return new WaitForSeconds(OriginalSceneWaitTime);

            BlackBackground.SetActive(true);

            PaletteManager.Instance.CurObjectParent.SetActive(false);
            ReconUI.gameObject.SetActive(true);
            yield return new WaitForSeconds(BlackScreenWaitTime);

            BlackBackground.SetActive(false);

            while (!HasAnswered)
                yield return null;

            HasAnswered = false;
        }

        ReconUI.gameObject.SetActive(false);
        SceneManager.UnloadSceneAsync(SceneNameList[SceneNameList.Count - 1]);
        MenuUI.SetActive(true);
    }
    private List<GameObject> GetGameObjectList<T>(List<T> obj_list) where T : MonoBehaviour
    {
        List<GameObject> output = new();
        foreach (T obj in obj_list)
        {
            output.Add(obj.gameObject);
        }
        return output;
    }
    public void SetCurCamera(Camera camera)
    {
        CurCamera = camera;
    }
    public void ConfirmButtonPressed()
    {
        Debug.Log("Confirm pressed");
        HasAnswered = true;
    }
}
