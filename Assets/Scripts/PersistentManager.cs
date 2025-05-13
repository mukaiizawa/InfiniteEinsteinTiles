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

    public static string CreativeDir;
    public static string[] PuzzleDirs = new string[SlotCount];

    /*
     * PlayerPrefs
     */

    string _prefKeySE = "SEVolume";
    string _prefKeyBGM = "BGMVolume";
    string _prefKeyResolution = "Resolution";
    string _prefKeyFullScreen = "FullScreen";
    string _prefKeyLocale = "Locale";
    string _prefKeyActiveSlot = "ActiveSlot";
    string _prefKeyCurrentLevel = "CurrentLevel";

    /*
     * FileSystem
     *
     *     Application.persistentDataPath/
     *       - creative/
     *         - bc9b300b853e4282aa360d29902fd7e5.json
     *         - 0a02677036754a08a464f4af769ebc3a.json.backup
     *         - a1f0ef59b60f400ab6957f1f647a976e.json.backup
     *       - puzzle/
     *         - slot1/
     *           - solution1.json
     *           - solution2.json
     *           ...
     *         - slot2/
     *         - slot3/
     */

    void Start()
    {
        CreativeDir = mkdir(Path.Combine(Application.persistentDataPath, "creative"));
        var puzzleDirRoot = mkdir(Path.Combine(Application.persistentDataPath, "puzzle"));
        for (int i = 0; i < SlotCount; i++)
            PuzzleDirs[i] = mkdir(Path.Combine(puzzleDirRoot, $"slot{i}"));
    }

    /*
     * player pref
     */

    public Resolution GetResolution()
    {
        try {
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

    public int GetActiveSlot()
    {
        var slot = PlayerPrefs.GetInt(_prefKeyActiveSlot, 1);
        Debug.Log($"PersistentManager#GetActiveSlot {slot}");
        return slot;
    }

    public void SetActiveSlot(int val)
    {
        if (!(1 <= val && val <= SlotCount))
        {
            Debug.LogError($"PersistentManager#SetActiveSlot: invalid slot {val}");
            return;
        }
        PlayerPrefs.SetInt(_prefKeyActiveSlot, val);
        PlayerPrefs.Save();
    }

    public void DeleteSlot(int slot)
    {
        PlayerPrefs.DeleteKey(_prefKeyCurrentLevel + slot);
    }

    public int GetActiveSlotCurrentLevel()
    {
        return GetCurrentLevel(GetActiveSlot());
    }

    public int GetCurrentLevel(int slot)
    {
#if UNITY_EDITOR
        if (slot == 3) return GlobalData.TotalLevel;    // for debug slot.
#endif
        return PlayerPrefs.GetInt(_prefKeyCurrentLevel + slot, 0);
    }

    public void SetCurrentLevel(int val)
    {
        PlayerPrefs.SetInt(_prefKeyCurrentLevel + GetActiveSlot(), val);
        PlayerPrefs.Save();
    }

    /*
     * FileSystem
     */

    string mkdir(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    void BackupIfExists(string file)
    {
        if (File.Exists(file))
        {
            try {
                File.Copy(file, file + ".backup", true);
            }
            catch (Exception e)
            {
                Debug.LogError($"PersistentManager#BackupIfExists: backup failed {e.Message}");
            }
        }
    }

    void SaveWithBackup(string file, string contents)
    {
        BackupIfExists(file);
        try {
            mkdir(Path.GetDirectoryName(file));
            File.WriteAllText(file, contents);
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#SaveWithBackup: {e.Message}");
        }
    }

    public void DeleteCreativeSolution(Solution solution)
    {
        string file = Path.Combine(CreativeDir, solution.PhysicalName);
        if (file == null || !File.Exists(file)) return;
        try {
            BackupIfExists(file);
            File.Delete(file);
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#DeleteCreativeSolution: {e.Message}");
        }
    }

    public void SaveCreativeSolution(Solution solution)
    {
        SaveWithBackup(Path.Combine(CreativeDir, solution.PhysicalName), JsonUtility.ToJson(solution));
    }

    public List<Solution> LoadCreativeSolutions()
    {
        var result = new List<Solution>();
        foreach (var file in Directory.GetFiles(CreativeDir))
        {
            if (!file.EndsWith("json")) continue;
            try {
                var solution = JsonUtility.FromJson<Solution>(File.ReadAllText(file));
                solution.PhysicalName = Path.GetFileName(file);
                result.Add(solution);
            }
            catch (Exception e)
            {
                Debug.LogError($"PersistentManager#LoadCreativeSolutions: {e.Message}");
            }
        }
        return result.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Name).ToList();
    }

    string PuzzleSolution(int level)
    {
        return Path.Combine(PuzzleDirs[GetActiveSlot() - 1], $"solution{level}");
    }

    public void DeletePuzzleSolution(int level)
    {
        var file = PuzzleSolution(level);
        try {
            if (File.Exists(file)) File.Delete(file);
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#DeletePuzzleSolution: {e.Message}");
        }
    }

    public void SavePuzzleSolution(int level, Solution solution)
    {
        try {
            File.WriteAllText(PuzzleSolution(level), JsonUtility.ToJson(solution));
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#SavePuzzleSolution: {e.Message}");
        }
    }

    public Solution LoadPuzzleSolution(int level)
    {
        var file = PuzzleSolution(level);
        try {
            if (File.Exists(file))
                return JsonUtility.FromJson<Solution>(File.ReadAllText(file));
        }
        catch (Exception e)
        {
            Debug.LogError($"PersistentManager#LoadPuzzleSolution: {e.Message}");
        }
        return null;
    }

    public void Save(string file, object o, bool format=false)
    {
        string json = JsonUtility.ToJson(o, format);
        string path = Path.Combine(Application.persistentDataPath, file);
        File.WriteAllText(path, json);
        Debug.Log("PersistentManager#Save: " + path);
    }

}
