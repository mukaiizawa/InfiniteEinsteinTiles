using System.Collections;
using UnityEngine;

using TMPro;

public class NotificationManager : MonoBehaviour
{

    public GameObject Panel;
    public TextMeshProUGUI Message;

    float _displayTime = 4f;    // [s]
    float _fadeDuration = 1f;    // [s]

    public void Notify(string message)
    {
        StopAllCoroutines();
        StartCoroutine(notify(message));
    }

    private IEnumerator notify(string message)
    {
        Message.text = message;
        Message.canvasRenderer.SetAlpha(0f);
        Panel.gameObject.SetActive(true);
        Message.CrossFadeAlpha(1f, _fadeDuration, false);
        yield return new WaitForSeconds(_fadeDuration + _displayTime);
        Message.CrossFadeAlpha(0f, _fadeDuration, false);
        yield return new WaitForSeconds(_fadeDuration);
        Panel.gameObject.SetActive(false);
    }

}
