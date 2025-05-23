using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using static MathHex;
using static Colors;

[System.Serializable]
public class TileMemory
{

    public Vector2Int Position;
    public int Rotation;    // 0 to 5.
    public bool IsFlip;
    public Color Color;

    public TileMemory()
    {
        this.Position = Vector2Int.zero;
        this.Rotation = 0;
        this.IsFlip = false;
        this.Color = Colors.Tile;
    }

    public TileMemory(Vector2Int pos, Color color) : this()
    {
        this.Position = pos;
        this.Color = color;
    }

    public TileMemory CopyFrom(TileMemory memory)
    {
        this.Position = new Vector2Int(memory.Position.x, memory.Position.y);
        this.Rotation = memory.Rotation;
        this.IsFlip = memory.IsFlip;
        this.Color = memory.Color;
        return this;
    }

    PartialHex[] gen(
            (Vector2Int, int) t0,
            (Vector2Int, int) t1,
            (Vector2Int, int) t2,
            (Vector2Int, int) t3,
            (Vector2Int, int) t4,
            (Vector2Int, int) t5,
            (Vector2Int, int) t6,
            (Vector2Int, int) t7)
    {
        return new PartialHex[] {
            new (t0.Item1 + this.Position, t0.Item2),
            new (t1.Item1 + this.Position, t1.Item2),
            new (t2.Item1 + this.Position, t2.Item2),
            new (t3.Item1 + this.Position, t3.Item2),
            new (t4.Item1 + this.Position, t4.Item2),
            new (t5.Item1 + this.Position, t5.Item2),
            new (t6.Item1 + this.Position, t6.Item2),
            new (t7.Item1 + this.Position, t7.Item2)
        };
    }

    public PartialHex[] PartialHexes()
    {
        switch ((IsFlip, Rotation))
        {
            case ((false, 0)): return gen((O, 4),(O, 5),(O, 0),(O, 1),( L, 1),( L, 2),(LU, 3),(LU, 4));
            case ((false, 1)): return gen((O, 3),(O, 4),(O, 5),(O, 0),( B, 0),( B, 1),( L, 2),( L, 3));
            case ((false, 2)): return gen((O, 2),(O, 3),(O, 4),(O, 5),(BR, 5),(BR, 0),( B, 1),( B, 2));
            case ((false, 3)): return gen((O, 1),(O, 2),(O, 3),(O, 4),( R, 4),( R, 5),(BR, 0),(BR, 1));
            case ((false, 4)): return gen((O, 0),(O, 1),(O, 2),(O, 3),( U, 3),( U, 4),( R, 5),( R, 0));
            case ((false, 5)): return gen((O, 5),(O, 0),(O, 1),(O, 2),(LU, 2),(LU, 3),( U, 4),( U, 5));
            case ((true, 0)): return gen((O, 5),(O, 0),(O, 1),(O, 2),( U, 2),( U, 3),( R, 4),( R, 5));
            case ((true, 1)): return gen((O, 4),(O, 5),(O, 0),(O, 1),(LU, 1),(LU, 2),( U, 3),( U, 4));
            case ((true, 2)): return gen((O, 3),(O, 4),(O, 5),(O, 0),( L, 0),( L, 1),(LU, 2),(LU, 3));
            case ((true, 3)): return gen((O, 2),(O, 3),(O, 4),(O, 5),( B, 5),( B, 0),( L, 1),( L, 2));
            case ((true, 4)): return gen((O, 1),(O, 2),(O, 3),(O, 4),(BR, 4),(BR, 5),( B, 0),( B, 1));
            case ((true, 5)): return gen((O, 0),(O, 1),(O, 2),(O, 3),( R, 3),( R, 4),(BR, 5),(BR, 0));
            default: Debug.LogError("never reached"); return System.Array.Empty<PartialHex>();
        }
    }

    public override string ToString()
    {
        return Position + "," + Rotation + "," + IsFlip;
    }

}
