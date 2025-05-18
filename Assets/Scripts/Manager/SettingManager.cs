using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class SettingManager : MonoBehaviour
{

    public Slider BGMSlider;
    public Slider SESlider;
    public Toggle FullScreenCheckbox;
    public TMP_Dropdown ResolutionList;

    AudioManager _audioManager;
    PersistentManager _persistentManager;

    Resolution[] _availableResolutions;

    void Start()
    {
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
        BGMSlider.value = _persistentManager.GetBGMVolume();
        SESlider.value = _persistentManager.GetSEVolume();
        BGMSlider.onValueChanged.AddListener((val) => _audioManager.SetBGMVolume(val));
        SESlider.onValueChanged.AddListener((val) => _audioManager.SetSEVolume(val));
        {
            var (w, h) = (Screen.currentResolution.width, Screen.currentResolution.height); 
            _availableResolutions = Screen.resolutions
                .Select(x => new Resolution { width = x.width, height = x.height })    // ignore refreshRate
                .Where(x => (x.width == w && x.height == h) || Mathf.Approximately((float)x.width / x.height, 16f / 9f))
                .Distinct()
                .OrderBy(x => x.width)
                .ThenBy(x => x.height).ToArray();
            var options = _availableResolutions.Select(x => $"{x.width}x{x.height}").ToList();
            ResolutionList.ClearOptions();
            ResolutionList.AddOptions(options);
            ResolutionList.value = options.IndexOf($"{w}x{h}");
            ResolutionList.RefreshShownValue();
        }
        ResolutionList.onValueChanged.AddListener(OnResolutionChange);
        FullScreenCheckbox.isOn = Screen.fullScreen;
        FullScreenCheckbox.onValueChanged.AddListener(OnFullScreenChange);
    }

    void OnResolutionChange(int index)
    {
        if (index < 0 || index >= _availableResolutions.Length) return;
        Resolution resolution = _availableResolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        _persistentManager.SetResolution(resolution);
    }

    void OnFullScreenChange(bool isOn)
    {
        _persistentManager.SetFullScreen(Screen.fullScreen = isOn);
    }

}
