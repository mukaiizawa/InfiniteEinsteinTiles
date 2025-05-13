using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using UnityEngine;

public static class Colors
{

    public static Color OK = Parse("#4ed964");
    public static Color NG = Parse("#ff3a31");

    public static Color Tile = Color.white;
    public static Color SelectedTile = Parse("#ffa500");

    public static int PaletteSize = 30;
    public static Color[] DefaultColorPalette =
        new string[] {
            "#FFFFFF",
            "#E3E3E3",
            "#C6C6C6",
            "#AAAAAA",
            "#8E8E8E",
            "#717171",
            "#555555",
            "#393939",
            "#1C1C1C",
            "#000000",
            "#FF0000",
            "#FF9900",
            "#CCFF00",
            "#33FF00",
            "#00FF99",
            "#00FFFF",
            "#0066FF",
            "#3300FF",
            "#CC00FF",
            "#FF0099",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
            "#FFFFFF",
        }.Select((x) => Parse(x)).ToArray();

    public static Color Parse(string s)
    {
        ColorUtility.TryParseHtmlString(s, out Color color);
        return color;
    }

    public static string Format(Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static Color ChangeAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    public static Color ChangeSaturation(Color c, float multiplier)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * multiplier), v);
    }

}
