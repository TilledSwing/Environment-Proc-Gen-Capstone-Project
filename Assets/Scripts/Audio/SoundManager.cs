using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Collections;
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource MusicSource;
    public AudioSource SFXSource;

    [Header("Audio Clips")]
    public List<AudioClip> MusicClips;
    public List<AudioClip> SFXClips;

    private Dictionary<string, AudioClip> MusicDict;
    private Dictionary<string, AudioClip> SFXDict;

    private float musicFadeDuration = 2f;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionaries();
            SetMusicVolume(.05f);

        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionaries()
    {
        MusicDict = new Dictionary<string, AudioClip>();
        foreach (var clip in MusicClips)
            MusicDict[clip.name] = clip;

        SFXDict = new Dictionary<string, AudioClip>();
        foreach (var clip in SFXClips)
            SFXDict[clip.name] = clip;

    }

    public void PlayMusic(string name, bool loop = true)
    {
        if (!MusicDict.TryGetValue(name, out AudioClip clip)) return;

        if (!MusicSource.isPlaying)
        {
            MusicSource.clip = clip;
            MusicSource.loop = loop;
            MusicSource.Play();
            return;
        }
        StartCoroutine(CrossFadeMusic(clip));

    }

    private IEnumerator CrossFadeMusic(AudioClip nextClip)
    {
        float startVolume = MusicSource.volume;
        float time = 0f;

        while (time < musicFadeDuration)
        {
            time += Time.deltaTime;
            MusicSource.volume = Mathf.Lerp(startVolume, 0f, time / musicFadeDuration);
            yield return null;
        }

        MusicSource.clip = nextClip;
        MusicSource.Play();

        time = 0f;
        while (time < musicFadeDuration)
        {
            time += Time.deltaTime;
            MusicSource.volume = Mathf.Lerp(0f, startVolume, time / musicFadeDuration);
            yield return null;
        }
        MusicSource.volume = startVolume;
    }

    public void PlaySFX(string name, float volumeScale) //Can add auio scale if we want
    {
        if (SFXDict.TryGetValue(name, out AudioClip clip))
        {
            SFXSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning("Unable to locate the SFX clip with the name: " + name);
        }
    }

    public void PlaySFXAtPoint(string name, Vector3 position, float volume=1f) //Can add auio scale if we want
    {
        if (SFXDict.TryGetValue(name, out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
        else
        {
            Debug.LogWarning("Unable to locate the SFX clip with the name: " + name);
        }
    }

    public void SetMusicVolume(float volume) //0-1
    {
        MusicSource.volume = volume;
    }

    public void SetSFXVolume(float volume) //0-1
    {
        SFXSource.volume = volume;
    }
}