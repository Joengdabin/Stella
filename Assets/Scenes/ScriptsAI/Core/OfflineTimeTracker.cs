using UnityEngine;
using System;

public class OfflineTimeTracker : MonoBehaviour
{
    const string LAST_TIME_KEY = "LastExitTime";

    void OnApplicationQuit()
    {
        PlayerPrefs.SetString(LAST_TIME_KEY, DateTime.UtcNow.ToString());
    }

    public static TimeSpan GetOfflineDuration()
    {
        if (!PlayerPrefs.HasKey(LAST_TIME_KEY))
            return TimeSpan.Zero;

        DateTime lastTime = DateTime.Parse(PlayerPrefs.GetString(LAST_TIME_KEY));
        return DateTime.UtcNow - lastTime;
    }
}