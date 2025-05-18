using System;
using System.Linq;

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    public AudioMixer AudioMixer;

    PersistentManager _persistentManager;

    float _time = 0f;
    int _trackNo = 0;
    AudioSource _bgmSource;
    AudioSource _seSource;
    AudioClip[] _playlist;

    float ToDecibel(float volume)
    {
        if (volume <= 0.0001f) return -80f;
        return Mathf.Log10(volume) * 20f;
    }

    void StopBGM()
    {
        if (_bgmSource == null || !_bgmSource.isPlaying) return;
        _bgmSource.Stop();
    }

    void PlayBGM()
    {
        if (_bgmSource == null || _bgmSource.isPlaying) return;
        _bgmSource.time = _time;
        _bgmSource.Play();
    }

    void PlayNextBGM()
    {
        if (_playlist == null || _playlist.Length == 0) return;
        _time = 0f;
        _bgmSource.clip = _playlist[(_trackNo = (_trackNo + 1)) % _playlist.Length];
        PlayBGM();
    }

    public void StartBGM()
    {
        PlayNextBGM();
    }

    public AudioManager SetPlaylist(AudioClip[] playlist)
    {
        _playlist = playlist.OrderBy(x => Guid.NewGuid()).ToArray();    // shuffle
        return this;
    }

    public void SetSEVolume(float val)
    {
        AudioMixer.SetFloat("SEVolume", ToDecibel(val));
    }

    public void SetBGMVolume(float val)
    {
        AudioMixer.SetFloat("BGMVolume", ToDecibel(val));
    }

    public void PlaySE(AudioClip se)
    {
        if (se != null) _seSource.PlayOneShot(se);
    }

    void Awake()
    {
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
    }

    void Start()
    {
        var camera = Camera.main.gameObject;
        _bgmSource = camera.AddComponent<AudioSource>();
        _bgmSource.loop = false;
        _bgmSource.outputAudioMixerGroup = AudioMixer.FindMatchingGroups("BGM")[0];
        _seSource = camera.AddComponent<AudioSource>();
        _seSource.loop = false;
        _seSource.outputAudioMixerGroup = AudioMixer.FindMatchingGroups("SE")[0];
        SetBGMVolume(_persistentManager.GetBGMVolume());
        SetSEVolume(_persistentManager.GetSEVolume());
    }

    void FixedUpdate()
    {
        if (_bgmSource != null && _bgmSource.clip != null)
        {
            _time = _bgmSource.time;
            if (Mathf.Approximately(_bgmSource.clip.length, _time)) PlayNextBGM();
        }
    }

    void OnApplicationFocus(bool onFocus)
    {
        if (onFocus) PlayBGM();
        else StopBGM();
    }

}
