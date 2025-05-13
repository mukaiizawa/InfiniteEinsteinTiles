using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

public static class XGameObject
{

    public static GameObject AtWorldPoint(Vector2 p)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return null;
        RaycastHit2D hit = Physics2D.Raycast(p, Vector2.zero);
        if (hit.collider != null) return hit.collider.gameObject;
        return null;
    }

    public static GameObject Parent(this GameObject o)
    {
        var t = o.transform.parent;
        if (t == null) return null;
        return t.gameObject;
    }

    public static GameObject[] Children(this GameObject o)
    {
        var objects = new List<GameObject>();
        foreach (Transform t in o.transform) objects.Add(t.gameObject);
        return objects.ToArray();
    }

}
