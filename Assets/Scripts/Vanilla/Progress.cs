using System;

using UnityEngine;

[System.Serializable]
public class Progress
{

    public int CurrentLevel;

    public Progress()
    {
    }

    public Progress(int level)
    {
        CurrentLevel = level;
    }

}
