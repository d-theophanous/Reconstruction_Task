using System;
using System.IO;
using UnityEngine;

// Small static holder for the current session's identity - set once the consent
// screen is accepted, read by whatever ends up saving data to disk.
public static class SessionInfo
{
    public static string ParticipantId { get; private set; }
    public static DateTime ConsentTimestampUtc { get; private set; }
    public static bool HasConsented { get; private set; }

    public static string OutputFolder => Path.Combine(Application.persistentDataPath, "ReconstructionData");

    public static void RecordConsent(string participantId)
    {
        ParticipantId = string.IsNullOrWhiteSpace(participantId) ? Guid.NewGuid().ToString("N") : participantId.Trim();
        ConsentTimestampUtc = DateTime.UtcNow;
        HasConsented = true;

        Directory.CreateDirectory(OutputFolder);
        string line = $"{ParticipantId},{ConsentTimestampUtc:O}";
        File.AppendAllText(Path.Combine(OutputFolder, "consent_log.csv"), line + Environment.NewLine);
    }

    public static string SessionFileTag => $"{ConsentTimestampUtc:yyyyMMdd_HHmmss}_{ParticipantId}";
}
