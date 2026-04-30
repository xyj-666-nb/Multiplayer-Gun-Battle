using System;
using UnityEngine;

public static class PrivacyConsentGate
{
    public const string ConsentVersion = "2026-04-30";
    private const string ConsentPlayerPrefsKey = "PrivacyConsentVersion";

    public static bool HasAccepted()
    {
        return PlayerPrefs.GetString(ConsentPlayerPrefsKey, string.Empty) == ConsentVersion;
    }

    public static void RequireConsent(Action onAccepted)
    {
        if (HasAccepted())
        {
            onAccepted?.Invoke();
            return;
        }

        PrivacyConsentPanel panel = UImanager.Instance.ShowPanel<PrivacyConsentPanel>();
        panel.Init(onAccepted);
    }

    public static void MarkAccepted()
    {
        PlayerPrefs.SetString(ConsentPlayerPrefsKey, ConsentVersion);
        PlayerPrefs.Save();
    }
}
