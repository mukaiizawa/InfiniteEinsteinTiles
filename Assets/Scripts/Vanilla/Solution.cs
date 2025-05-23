using System;

using UnityEngine;

[System.Serializable]
public class Solution
{

    [System.NonSerialized]
    public GameMode GameMode;

    [System.NonSerialized]
    public int Slot;

    [System.NonSerialized]
    public int Level;

    public string Name;

    [System.NonSerialized]
    public string PhysicalName;

    public double CreatedAt;

    public double UpdatedAt;

    public Board Board;

    public Solution()
    {
    }

    public Solution(GameMode gameMode, int slot, int level, string name)
    {
        GameMode = gameMode;
        Slot = slot;
        Level = level;
        Name = name;
        var now = DateTime.UnixTimeNow();
        CreatedAt = now;
        UpdatedAt = now;
        Board = new Board();
        PhysicalName = Guid.NewGuid().ToString("N") + ".json";
    }

    public override string ToString()
    {
        return $"GameMode: {GameMode}, Slot: {Slot}, Level: {Level}, Name: {Name}, PhysicalName: {PhysicalName}";
    }

}
