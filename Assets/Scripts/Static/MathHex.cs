using UnityEngine;

public static class MathHex
{

    public static Vector2Int O = Vector2Int.zero;
    public static Vector2Int R = Vector2Int.right;
    public static Vector2Int L = Vector2Int.left;
    public static Vector2Int B = Vector2Int.down;
    public static Vector2Int U = Vector2Int.up;
    public static Vector2Int LU = L + U;
    public static Vector2Int BR = B + R;

    public static float A = 0.64f;    // 64px@100 px per unit
    public static float Sqrt3 = Mathf.Sqrt(3);

    /*
     * convert oblique coordinates to worldPoint.
     */
    public static Vector2 ObliqueToWorldPoint(Vector2Int p)
    {
        return new Vector2(2 * A * p.x + A * p.y,  Sqrt3 * A * p.y);
    }

    /*
     * convert worldPoint to nearest oblique coordinates.
     */
    public static Vector2Int WorldPointToNearestOblique(Vector2 worldPoint)
    {
        int j = Mathf.RoundToInt(worldPoint.y / (Sqrt3 * A));
        int i = Mathf.RoundToInt((worldPoint.x - j * A) / (2 * A));
        return new Vector2Int(i, j);
    }

    /*
     * convert worldPoint to bottom left oblique coordinates.
     */
    public static Vector2Int WorldPointToLeftBottomOblique(Vector2 worldPoint)
    {
        int j = Mathf.FloorToInt(worldPoint.y / (Sqrt3 * A));
        int i = Mathf.FloorToInt((worldPoint.x - j * A) / (2 * A));
        return new Vector2Int(i, j);
    }

    public static Vector2 LeftBottomObliqueWorldPoint(Vector2 worldPoint)
    {
        return ObliqueToWorldPoint(WorldPointToLeftBottomOblique(worldPoint));
    }

    public static Vector2 NearestObliqueWorldPoint(Vector2 worldPoint)
    {
        return ObliqueToWorldPoint(WorldPointToNearestOblique(worldPoint));
    }

}
