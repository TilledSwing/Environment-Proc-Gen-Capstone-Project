using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
public class NetworkSoundManager : NetworkBehaviour
{
    public static NetworkSoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<AudioClip> musicClips;
    public List<AudioClip> sfxClips;

    private Dictionary<string, AudioClip> musicDict;
    private Dictionary<string, AudioClip> sfxDict;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionaries()
    {
        musicDict = new Dictionary<string, AudioClip>();
        foreach (var clip in musicClips)
            musicDict[clip.name] = clip;

        sfxDict = new Dictionary<string, AudioClip>();
        foreach (var clip in sfxClips)
            musicDict[clip.name] = clip;
    }

    private void PlayMusicLocal(string name, bool loop)
    {
        if (musicDict.TryGetValue(name, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Unable to locate the music clip with the name: " + name);
        }
    }

    private void PlaySfxLocal(string name, float volumeScale) //Can add auio scale if we want
    {
        if (sfxDict.TryGetValue(name, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning("Unable to locate the sfx clip with the name: " + name);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SRPCPlayMusic(string name, bool loop)
    {
        ORPCPlayMusic(name, loop);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SRPCPlaySfc(string name, float volumeScale = 1.0f)
    {
        ORPCPlaySfx(name, volumeScale);
    }
    
    [ObserversRpc]
    public void ORPCPlayMusic(string name, bool loop)
    {
        PlayMusicLocal(name, loop);
    }
    
    [ObserversRpc]
    public void ORPCPlaySfx(string name, float volumeScale = 1.0f)
    {
        PlaySfxLocal(name, volumeScale);
    }

    public void SetMusicVolume(float volume) //0-1
    {
        musicSource.volume = volume;
    }

    public void SetSfxVolume(float volume) //0-1
    {
        sfxSource.volume = volume;
    }
}