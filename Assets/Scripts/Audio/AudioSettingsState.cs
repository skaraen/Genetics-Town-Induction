using System;
using UnityEngine;

public static class AudioSettingsState
{
    private const string MusicKey = "Audio_Music_On";
    private const string SfxKey = "Audio_Sfx_On";
    private const string EnvironmentKey = "Audio_Environment_On";

    public static event Action OnSettingsChanged;

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static bool EnvironmentOn
    {
        get => PlayerPrefs.GetInt(EnvironmentKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(EnvironmentKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }
}