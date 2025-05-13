using System.Collections;

using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class TitleSceneManager : MonoBehaviour
{

    static bool Clicked = false;

    public Button ContinueButton;
    public TextMeshProUGUI VersionText;

    AudioManager _audioManager;
    AssetManager _assetManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;

    void Awake()
    {
        Application.targetFrameRate = 60;
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _assetManager = this.gameObject.GetComponent<AssetManager>();
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
    }

    void Start()
    {
        var resolution = _persistentManager.GetResolution();
        Screen.fullScreen = _persistentManager.IsFullScreen();
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.Title)).StartBGM();
        ContinueButton.onClick.AddListener(() => StartCoroutine(OnContinue()));
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        StartCoroutine(SetLocaleAsync(_persistentManager.GetLocale()));
        VersionText.text = $"Version {Application.version}";
    }

    float _fadeDulation = 2f;
    IEnumerator OnContinue()
    {
        if (Clicked) yield break;
        Clicked = true;
        _audioManager.PlaySE(_assetManager.SEOK);
        var light = GameObject.Find("/GlobalLight").GetComponent<Light2D>();
        var canvasGroup = GameObject.Find("/Canvas").GetComponent<CanvasGroup>();
        float startIntensity = light.intensity;
        float t = 0f;
        while (t < _fadeDulation)
        {
            t += Time.deltaTime;
            var ratio = t / _fadeDulation;
            if (light != null)
                light.intensity = Mathf.Lerp(startIntensity, 0f, ratio);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, ratio);
            yield return null;
        }
        yield return _loadingManager.LoadAsync(LoadingManager.Scene.Menu);
    }

    IEnumerator SetLocaleAsync(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code.Equals(localeCode));
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
        yield return null;
    }

}
