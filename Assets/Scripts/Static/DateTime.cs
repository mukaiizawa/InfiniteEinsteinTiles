using System;

using UnityEngine;

public static class DateTime
{

    public static double UnixTimeNow()
    {
        return System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static System.DateTime FromUnixTime(double utime)
    {
        return new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(utime).ToLocalTime();
    }

}
