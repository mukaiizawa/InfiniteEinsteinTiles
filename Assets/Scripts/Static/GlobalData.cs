using UnityEngine;

public static class GlobalData
{

    public static int Slot;    // [1, 3]
    public static int Level;    // [1, TotalLevel]
    public static int TotalLevel = 28;

    public static GameMode GameMode = GameMode.Nil;
    public static Solution Solution;

    /**
     * When restarting with tiling scene.
     * In order to leave the user the option to exit without saving after restarting, the saved data is not deleted and reloaded.
     */
    public static bool IsRestart;

}
