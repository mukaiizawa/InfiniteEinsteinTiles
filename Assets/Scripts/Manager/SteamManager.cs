using System;

using UnityEngine;

using Steamworks;
using Steamworks.Data;

public class SteamManager : MonoBehaviour
{

    public static SteamManager _instance;

    uint _appId = 3550470;
    bool _connected = false;
    int _achievementCount = GlobalData.TotalLevel + 1;    // Each level and all cleared. 1 to 29.

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        try
        {
            SteamClient.Init(_appId);
            _connected = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SteamManager#Awake: connection failed {e.Message}");
            _connected = false;
        }
    }

    void OnApplicationQuit()
    {
        Close();
    }

    void Update()
    {
        if (_connected) SteamClient.RunCallbacks();
    }

    public void Close()
    {
        if (_connected) SteamClient.Shutdown();
    }

    bool Achievement(int level, out Achievement result)
    {
        if (level - 1 < 0 || level - 1 >= _achievementCount)
        {
            Debug.LogWarning($"SteamManager#Achievement invalid level {level}");
            return false;
        }
        result = new Achievement($"p{level}");
        return true;
    }

    public void UnlockAchievement(int level)
    {
        if (!_connected) return;
        Debug.Log($"SteamManager#UnlockAchievement unlock {level}");
        if (Achievement(level, out Achievement achievement))
        {
            achievement.Trigger();
            if (level == GlobalData.TotalLevel) UnlockAchievement(level + 1);    // congratulations!
        }
    }

    public void ResetAllAchievements()
    {
        if (!_connected) return;
        Debug.Log($"SteamManager#ResetAllAchievements");
        for (int i = 0; i < _achievementCount; i++)
            if (Achievement(i + 1, out Achievement achievement)) achievement.Clear();
    }

}
