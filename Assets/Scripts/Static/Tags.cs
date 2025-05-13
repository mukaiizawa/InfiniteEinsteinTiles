using UnityEngine;

public static class Tags
{

    public static string Tile = "Tile";
    public static string LevelTile = "LevelTile";
    public static string SelectedTile = "SelectedTile";

    public static bool match(GameObject o, string tag)
    {
        if (o == null) return false;
        return o.tag == tag;
    }

}
