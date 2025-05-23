using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;

using UnityEngine;

public class PersistentManager : MonoBehaviour
{

    public static int SlotCount = 3;    // [1,3]

    public string ScreenshotDir;
    public string CreativeModeDir;
    public string PuzzleModeDir;
    public string[] PuzzleModeSlotDirs = new string[SlotCount];

    /*
     * PlayerPrefs
     */

    string _prefKeySE = "SEVolume";
    string _prefKeyBGM = "BGMVolume";
    string _prefKeyResolution = "Resolution";
    string _prefKeyFullScreen = "FullScreen";
    string _prefKeyLocale = "Locale";

    void Start()
    {
        Directory.CreateDirectory(ScreenshotDir = Path.Combine(Application.persistentDataPath, "Screenshots"));
        Directory.CreateDirectory(CreativeModeDir = Path.Combine(Application.persistentDataPath, "Creative"));
        Directory.CreateDirectory(PuzzleModeDir = Path.Combine(Application.persistentDataPath, "Puzzle"));
        for (int i = 0; i < SlotCount; i++)
        {
            Directory.CreateDirectory(PuzzleModeSlotDirs[i] = Path.Combine(PuzzleModeDir, $"Slot{i + 1}"));
            for (int j = 0; j < GlobalData.TotalLevel; j++)
                Directory.CreateDirectory(SolutionDir(GameMode.Puzzle, i + 1, j + 1));
        }
    }

    /*
     * player pref
     */

    public Resolution GetResolution()
    {
        try
        {
            var res = PlayerPrefs.GetString(_prefKeyResolution);
            var vals = res.Split("x");
            return new Resolution { width = int.Parse(vals[0]), height = int.Parse(vals[1]) };
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PersistentManager#GetResolution: failed {e.Message}");
            return Screen.currentResolution;
        }
    }

    public void SetResolution(Resolution resolution)
    {
        PlayerPrefs.SetString(_prefKeyResolution, $"{resolution.width}x{resolution.height}");
        PlayerPrefs.Save();
    }

    public bool IsFullScreen()
    {
        return PlayerPrefs.GetInt(_prefKeyFullScreen, 1) == 1;
    }

    public void SetFullScreen(bool b)
    {
        PlayerPrefs.SetInt(_prefKeyFullScreen, b? 1: 0);
        PlayerPrefs.Save();
    }

    public string GetLocale()
    {
        return PlayerPrefs.GetString(_prefKeyLocale, "en");
    }

    public void SetLocale(string val)
    {
        PlayerPrefs.SetString(_prefKeyLocale, val);
        PlayerPrefs.Save();
    }

    public float GetSEVolume()
    {
        return PlayerPrefs.GetFloat(_prefKeySE, 0.5f);
    }

    public void SetSEVolume(float val)
    {
        PlayerPrefs.SetFloat(_prefKeySE, val);
        PlayerPrefs.Save();
    }

    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat(_prefKeyBGM, 0.5f);
    }

    public void SetBGMVolume(float val)
    {
        PlayerPrefs.SetFloat(_prefKeyBGM, val);
        PlayerPrefs.Save();
    }

    /*
     * FileSystem
     *
     *     Application.persistentDataPath/
     *       - Creative/
     *         - bc9b300b853e4282aa360d29902fd7e5.json
     *         - 0a02677036754a08a464f4af769ebc3a.json.backup
     *         - a1f0ef59b60f400ab6957f1f647a976e.json.backup
     *       - Puzzle/
     *         - Slot1/
     *           - Puzzle1
     *               - adfpoip2134jspfisdal2po9902fd7e5.json
     *               - bc9b30adfpoip2134jspfisdal2po7e5.json
     *               ...
     *           - Puzzle2
     *               ...
     *           ...
     *           - Progress.json
     *         - slot2/
     *         - slot3/
     */

    string SolutionDir(GameMode gameMode, int slot, int level)
    {
        switch (gameMode)
        {
            case GameMode.Creative:
                return CreativeModeDir;
            case GameMode.Puzzle:
                return Path.Combine(PuzzleModeSlotDirs[slot - 1], $"Puzzle{level}");
            default:
                Debug.LogError($"PersistentManager#SolutionDir: invalid GameMode {gameMode}");
                return null;
        }
    }

    string SolutionFile(Solution solution)
    {
        return Path.Combine(SolutionDir(solution.GameMode, solution.Slot, solution.Level), solution.PhysicalName);
    }

    void BackupIfExists(string file)
    {
        if (File.Exists(file))
        {
            try
            {
                File.Copy(file, file + ".backup", true);
            }
            catch (Exception e)
            {
                Debug.LogError($"PersistentManager#BackupIfExists: backup failed {e.Message}");
            }
        }
    }

    public void SaveSolution(Solution solution)
    {
        var file = SolutionFile(solution);
        BackupIfExists(file);
        try
        {
            File.WriteAllText(file, JsonUtility.ToJson(solution));
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#SaveSolution: {e.Message}");
        }
    }

    public List<Solution> LoadSolutions(GameMode gameMode, int slot, int level)
    {
        var result = new List<Solution>();
        foreach (var file in Directory.GetFiles(SolutionDir(gameMode, slot, level)))
        {
            if (!file.EndsWith("json")) continue;
            try
            {
                var solution = JsonUtility.FromJson<Solution>(File.ReadAllText(file));
                solution.GameMode = gameMode;
                solution.Slot = slot;
                solution.Level = level;
                solution.PhysicalName = Path.GetFileName(file);    // basename.
                result.Add(solution);
            }
            catch (Exception e)
            {
                Debug.LogError($"PersistentManager#LoadSolutions: {e.Message}");
            }
        }
        return result.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Name).ToList();
    }

    public void DeleteSolution(Solution solution)
    {
        var file = SolutionFile(solution);
        if (!File.Exists(file)) return;
        try
        {
            BackupIfExists(file);
            File.Delete(file);
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#DeleteSolution: {e.Message}");
        }
    }

    /*
     * Progress
     */

    string PuzzleProgressFile(int slot)
    {
        return Path.Combine(PuzzleModeSlotDirs[slot - 1], $"Progress.json");
    }

    public Progress LoadProgress(int slot)
    {
#if UNITY_EDITOR
        if (slot == 2) return new Progress(0);    // for debug slot.
        if (slot == 3) return new Progress(GlobalData.TotalLevel);    // for debug slot.
#endif
        try
        {
            return JsonUtility.FromJson<Progress>(File.ReadAllText(PuzzleProgressFile(slot)));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PersistentManager#LoadProgress: {e.Message}");
        }
        return new Progress(0);
    }

    public void SaveProgress(int slot, Progress progress)
    {
        try
        {
            File.WriteAllText(PuzzleProgressFile(slot), JsonUtility.ToJson(progress));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PersistentManager#SaveProgress: {e.Message}");
        }
    }

    /*
     * Debug
     */

    public void Save(string file, object o, bool format=false)
    {
        string json = JsonUtility.ToJson(o, format);
        string path = Path.Combine(Application.persistentDataPath, file);
        File.WriteAllText(path, json);
        Debug.Log("PersistentManager#Save: " + path);
    }

}
