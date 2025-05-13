using System;

using UnityEngine;

[System.Serializable]
public class Board
{

    public TileMemory[] GrabingTiles;    // for only debug.
    public TileMemory[] PlacedTiles;
    public PartialHex[] PartialHexes;    // for only puzzle creation.
    public Color[] ColorPalette;

    public Board()
    {
        this.ColorPalette = Colors.DefaultColorPalette;
    }

    public Board(TileMemory[] tiles, Color[] colors)
    {
        this.PlacedTiles = tiles;
        this.ColorPalette = colors;
    }

    public int PlacedTileCount()
    {
        if (PlacedTiles == null) return 0;
        return PlacedTiles.Length;
    }

}
