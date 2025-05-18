using System;
using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{

    public static readonly Action OnLoadNone = () => {};

    public enum Scene
    {
        Title,
        Menu,
        PuzzleMenu,
        Tiling,
    }

    public CanvasGroup Mask;

    public IEnumerator LoadAsync(Scene next, float minLoadingTime=0f)
    {
        yield return StartCoroutine(LoadAsync(next, minLoadingTime, OnLoadNone));
    }

    public IEnumerator LoadAsync(Scene next, float minLoadingTime, Action action)
    {
        if (minLoadingTime > 1f || action != OnLoadNone) Mask.gameObject.SetActive(true);
        if (action != OnLoadNone)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            action.Invoke();
            sw.Stop();
            minLoadingTime -= sw.ElapsedMilliseconds / 1000f;
        }
        while ((minLoadingTime -= Time.deltaTime) > 0)
            yield return null;
        AsyncOperation async = SceneManager.LoadSceneAsync(next.ToString());
        while (!async.isDone)
            yield return null;
    }

}
