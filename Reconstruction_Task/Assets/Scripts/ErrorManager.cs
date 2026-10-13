using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ErrorManager : Singleton<ErrorManager>
{
    // Keyed by scene name. Practice trials are included too now, tagged via IsPractice.
    public Dictionary<string, TrialRecord> ObjectDataDic = new();

    public void AddTrial(string key, TrialRecord record)
    {
        ObjectDataDic[key] = record;
    }

    // Drops all recorded trials - call after saving so the next participant starts empty.
    public void ResetData()
    {
        ObjectDataDic.Clear();
    }

    // Matches each original object to the nearest not-yet-used reconstructed object of
    // the same type (rather than assuming list order lines up), then writes one CSV row
    // per matched pair with the position/scale error.
    public void CalculateAndSaveError(string sessionId)
    {
        var rows = new List<string>
        {
            "SessionId,Age,Gender,SceneName,IsPractice,ReconstructionSeconds,FixedBlackScreen,OriginalStartFOV,ReconStartFOV,ReconConfirmFOV,ObjectType,OrigPosX,OrigPosY,OrigPosZ,OrigScaleX,OrigScaleY,OrigScaleZ," +
            "ReconPosX,ReconPosY,ReconPosZ,ReconScaleX,ReconScaleY,ReconScaleZ"
            //"PositionError,ScaleError",ReconScreenW,ReconScreenH
        };

        string age = SessionInfo.Age > 0 ? SessionInfo.Age.ToString() : "";
        string gender = SessionInfo.Gender?.ToString() ?? "";

        foreach (var entry in ObjectDataDic)
        {
            string sceneName = entry.Key;
            TrialRecord record = entry.Value;
            var matches = MatchByNearestWithinType(record.Original, record.Recon);

            foreach (var (orig, recon) in matches)
            {
                float positionError = Vector3.Distance(orig.Position, recon.Position);
                float scaleError = Vector3.Distance(orig.Scale, recon.Scale);

                rows.Add(string.Join(",",
                    sessionId, age, gender, sceneName, record.IsPractice, F(record.ReconstructionSeconds),
                    record.FixedBlackScreen, F(record.OriginalStartFieldOfView), F(record.ReconStartFieldOfView),
                    F(record.ReconConfirmFieldOfView), orig.Type,
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

// One reconstruction task (practice or real): the original/reconstructed object lists plus
// how it was run - practice or not, how long the participant took, and the camera zoom
// (field of view) at each stage.
public class TrialRecord
{
    public List<ObjectData> Original = new();
    public List<ObjectData> Recon = new();
    public bool IsPractice;
    public float ReconstructionSeconds;
    public bool FixedBlackScreen;            // true: timed black screen, false: waited for the key press
    public float OriginalStartFieldOfView;   // zoom when the original image first appeared
    public float ReconStartFieldOfView;      // zoom when the empty scene was revealed
    public float ReconConfirmFieldOfView;    // zoom at the moment Confirm was pressed
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
