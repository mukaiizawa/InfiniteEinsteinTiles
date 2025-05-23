using System;

using UnityEngine;

[System.Serializable]
public class PartialHex
{

    public Vector2Int Position;
    public int Nth;    // 0..5

    public PartialHex(Vector2Int pos, int nth)
    {
        this.Position = pos;
        this.Nth = nth;
    }

    public override bool Equals(object o)
    {
        if (o == null || GetType() != o.GetType()) return false;
        PartialHex p = (PartialHex)o;
        return Position == p.Position && Nth == p.Nth;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Nth);
    }

    public override string ToString()
    {
        return Position + "#" + Nth;
    }

}
