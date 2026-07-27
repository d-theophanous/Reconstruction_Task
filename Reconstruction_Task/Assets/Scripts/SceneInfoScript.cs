
using System.Collections.Generic;
using UnityEngine;

public class SceneInfoScript : MonoBehaviour
{
    public List<ObjectScript> ObjectList = new();
    public GameObject ObjectParent;
    public Camera Camera;

    private void Start()
    {
        PaletteManager.Instance.SetUpCurrentReconstruction(ObjectList, ObjectParent);
        MainManager.Instance.SetCurCamera(Camera);
    }
}
