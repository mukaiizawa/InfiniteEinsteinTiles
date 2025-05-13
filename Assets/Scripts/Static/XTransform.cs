using UnityEngine;

public static class XTransform
{

    public static void DestroyAllChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
    }

}
