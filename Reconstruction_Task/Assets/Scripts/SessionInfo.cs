using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public enum GenderOption { Male, Female, NonBinary, PreferNotToSay }

// Small static holder for the current session's identity and demographics - set by the
// consent and demographics screens, read by whatever ends up saving data to disk.
public static class SessionInfo
{
    // Typed in by the researcher on the consent screen.
    public static string ParticipantId { get; private set; }
    public static DateTime ConsentTimestampUtc { get; private set; }
    public static bool HasConsented { get; private set; }

    public static int Age { get; private set; }          // 0 = not provided
    public static GenderOption? Gender { get; private set; } // null = not provided

    public static string OutputFolder => Path.Combine(Application.persistentDataPath, "ReconstructionData");

    static string sessionFileTag;

    public static void RecordConsent(string participantId)
    {
        ParticipantId = participantId.Trim();
        ConsentTimestampUtc = DateTime.UtcNow;
        HasConsented = true;
        sessionFileTag = null;

        Directory.CreateDirectory(OutputFolder);
        string line = $"{ParticipantId},{ConsentTimestampUtc:O}";
        File.AppendAllText(Path.Combine(OutputFolder, "consent_log.csv"), line + Environment.NewLine);
    }

    // Clears everything about the finished participant so the next one starts fresh.
    public static void Reset()
    {
        ParticipantId = null;
        ConsentTimestampUtc = default;
        HasConsented = false;
        Age = 0;
        Gender = null;
        sessionFileTag = null;
    }

    public static void SetDemographics(int age, GenderOption gender)
    {
        Age = age;
        Gender = gender;
    }

    // "<participant id>_<yyyy-MM-dd>", e.g. "P012_2026-10-16". Used for the results file and
    // the screenshots. If that name is already taken (same participant, same day) a
    // number is added, so an earlier file is never overwritten.
    public static string SessionFileTag => sessionFileTag ??= BuildFileTag();

    static string BuildFileTag()
    {
        string id = string.IsNullOrWhiteSpace(ParticipantId) ? "unknown" : ParticipantId;
        foreach (char c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');

        DateTime when = HasConsented ? ConsentTimestampUtc : DateTime.UtcNow;
        string baseTag = $"{id}_{when.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        string tag = baseTag;
        for (int n = 2; File.Exists(Path.Combine(OutputFolder, tag + ".csv")); n++)
            tag = $"{baseTag}_{n}";
        return tag;
    }
}
