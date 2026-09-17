using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ErrorManager : Singleton<ErrorManager>
{
    // Keyed by scene name. Practice trials are simply never added here, so they're
    // automatically excluded from the saved data.
    public Dictionary<string, (List<ObjectData> original, List<ObjectData> recon)> ObjectDataDic = new();

    public void FillOriginalDicEntry(string key, List<ObjectData> data)
    {
        EnsureEntry(key);
        ObjectDataDic[key].original.AddRange(data);
    }

    public void FillReconDicEntry(string key, List<ObjectData> data)
    {
        EnsureEntry(key);
        ObjectDataDic[key].recon.AddRange(data);
    }

    void EnsureEntry(string key)
    {
        if (!ObjectDataDic.ContainsKey(key))
            ObjectDataDic[key] = (new List<ObjectData>(), new List<ObjectData>());
    }

    // Matches each original object to the nearest not-yet-used reconstructed object of
    // the same type (rather than assuming list order lines up), then writes one CSV row
    // per matched pair with the position/scale error.
    public void CalculateAndSaveError(string sessionId)
    {
        var rows = new List<string>
        {
            "SessionId,SceneName,ObjectType,OrigPosX,OrigPosY,OrigPosZ,OrigScaleX,OrigScaleY,OrigScaleZ," +
            "ReconPosX,ReconPosY,ReconPosZ,ReconScaleX,ReconScaleY,ReconScaleZ"
            //"PositionError,ScaleError",ReconScreenW,ReconScreenH
        };

        foreach (var entry in ObjectDataDic)
        {
            string sceneName = entry.Key;
            var matches = MatchByNearestWithinType(entry.Value.original, entry.Value.recon);

            foreach (var (orig, recon) in matches)
            {
                float positionError = Vector3.Distance(orig.Position, recon.Position);
                float scaleError = Vector3.Distance(orig.Scale, recon.Scale);

                rows.Add(string.Join(",",
                    sessionId, sceneName, orig.Type,
                    F(orig.Position.x), F(orig.Position.y), F(orig.Position.z),
                    F(orig.Scale.x), F(orig.Scale.y), F(orig.Scale.z),
                    F(recon.Position.x), F(recon.Position.y), F(recon.Position.z),
                    F(recon.Scale.x), F(recon.Scale.y), F(recon.Scale.z)));
                //,F(positionError), F(scaleError),
                //F(recon.ScreenSize.x), F(recon.ScreenSize.y)
            }
        }

        Directory.CreateDirectory(SessionInfo.OutputFolder);
        string path = Path.Combine(SessionInfo.OutputFolder, $"session_{SessionInfo.SessionFileTag}.csv");
        File.WriteAllLines(path, rows);
    }

    static string F(float value) => value.ToString("F4", CultureInfo.InvariantCulture);

    static List<(ObjectData orig, ObjectData recon)> MatchByNearestWithinType(List<ObjectData> original, List<ObjectData> recon)
    {
        var matches = new List<(ObjectData, ObjectData)>();
        var reconRemaining = new List<ObjectData>(recon);

        foreach (var orig in original)
        {
            ObjectData best = null;
            float bestDist = float.PositiveInfinity;

            foreach (var candidate in reconRemaining)
            {
                if (candidate.Type != orig.Type) continue;
                float dist = Vector3.Distance(orig.Position, candidate.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }

            if (best != null)
            {
                matches.Add((orig, best));
                reconRemaining.Remove(best);
            }
        }

        return matches;
    }
}

// Only position and scale are tracked - rotation isn't part of this task.
public class ObjectData
{
    public ObjectType Type;
    public Vector3 Position;
    public Vector3 Scale;
    public Vector2 ScreenSize; // on-screen size in pixels at record time, zero when not measured

    public ObjectData(ObjectType type, Vector3 position, Vector3 scale, Vector2 screenSize = default)
    {
        Type = type;
        Position = position;
        Scale = scale;
        ScreenSize = screenSize;
    }
}
