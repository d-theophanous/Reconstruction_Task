using System;
using System.IO;
using UnityEngine;

public enum GenderOption { Male, Female, NonBinary, PreferNotToSay }

// Small static holder for the current session's identity and demographics - set by the
// consent and demographics screens, read by whatever ends up saving data to disk.
public static class SessionInfo
{
    // Anonymous, auto-generated when consent is given. Ties the CSV rows to the file name.
    public static string SessionId { get; private set; }
    public static DateTime ConsentTimestampUtc { get; private set; }
    public static bool HasConsented { get; private set; }

    public static int Age { get; private set; }          // 0 = not provided
    public static GenderOption? Gender { get; private set; } // null = not provided

    public static string OutputFolder => Path.Combine(Application.persistentDataPath, "ReconstructionData");

    public static void RecordConsent()
    {
        SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        ConsentTimestampUtc = DateTime.UtcNow;
        HasConsented = true;

        Directory.CreateDirectory(OutputFolder);
        string line = $"{SessionId},{ConsentTimestampUtc:O}";
        File.AppendAllText(Path.Combine(OutputFolder, "consent_log.csv"), line + Environment.NewLine);
    }

    // Clears everything about the finished participant so the next one starts fresh.
    public static void Reset()
    {
        SessionId = null;
        ConsentTimestampUtc = default;
        HasConsented = false;
        Age = 0;
        Gender = null;
    }

    public static void SetDemographics(int age, GenderOption gender)
    {
        Age = age;
        Gender = gender;
    }

    public static string SessionFileTag => $"{ConsentTimestampUtc:yyyyMMdd_HHmmss}_{SessionId}";
}
