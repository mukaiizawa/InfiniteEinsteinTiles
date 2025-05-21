using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    public AudioMixer AudioMixer;

    PersistentManager _persistentManager;

    AudioSource _bgmSource;
    AudioSource _seSource;
    AudioClip[] _playlist;

    float ToDecibel(float volume)
    {
        if (volume <= 0.0001f) return -80f;
        return Mathf.Log10(volume) * 20f;
    }

    IEnumerator StartBGMAsync()
    {
        while (true)
        {
            var clip = _playlist[UnityEngine.Random.Range(0, _playlist.Length)];
            _bgmSource.clip = clip;
            _bgmSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
    }

    public void StartBGM()
    {
        StartCoroutine(StartBGMAsync());
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

}
