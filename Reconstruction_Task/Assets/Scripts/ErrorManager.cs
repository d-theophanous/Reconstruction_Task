using System.Collections.Generic;
using UnityEngine;

public class ErrorManager : Singleton<ErrorManager>
{
    //- brauche position und scale z.B.
    public Dictionary<string, (List<ObjectData> original, List<ObjectData> recon)> ObjectDataDic = new();
    private List<List<float>> ErrorList = new();

    public void CalculateAndSaveError(string path)
    {
        foreach (var item in ObjectDataDic)
        {
            var value = item.Value;
            for (int i = 0; i < value.original.Count; i++)
            {
                ErrorList.Add(value.original[i].CalculateError(value.recon[i]));
            }
        }
    }
    public void FillOriginalDicEntry(string key, List<GameObject> obj_list)
    {
        if (!ObjectDataDic.ContainsKey(key))
        {
            Debug.Log("Dictionary does not contain given key");
            return;
        }
        foreach (var obj in obj_list)
        {
            ObjectDataDic[key].original.Add(new ObjectData(obj));
        }
    }
    public void FillReconDicEntry(string key, List<GameObject> obj_list)
    {

        if (!ObjectDataDic.ContainsKey(key))
        {
            Debug.Log("Dictionary does not contain given key");
            return;
        }
        foreach (var obj in obj_list)
        {
            ObjectDataDic[key].recon.Add(new ObjectData(obj));
        }
    }
}
public class ObjectData
{
    public Vector3 Position;
    public Vector3 Scale;

    public ObjectData(Vector3 position, Vector3 scale)
    {
        Position = position;
        Scale = scale;
    }
    public ObjectData(GameObject obj)
    {
        new ObjectData(obj.transform.position, obj.transform.lossyScale);
    }

    public List<float> CalculateError(ObjectData compare_obj)
    {
        List<float> output = new();

        //- How to calculate?! ToDo

        return output;
    }
}

