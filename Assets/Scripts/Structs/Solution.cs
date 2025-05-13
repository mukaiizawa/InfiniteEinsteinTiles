using System;

using UnityEngine;

[System.Serializable]
public class Solution
{

    public string Name;
    [System.NonSerialized]
    public string PhysicalName;
    public double CreatedAt;
    public double UpdatedAt;
    public Board Board;

    public Solution()
    {
    }

    public Solution(string name)
    {
        Name = name;
        var now = DateTime.UnixTimeNow();
        CreatedAt = now;
        UpdatedAt = now;
        Board = new Board();
        PhysicalName = Guid.NewGuid().ToString("N") + ".json";
    }

}
