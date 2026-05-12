using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Lightweight 3D sound manager - ultra simple and fast
/// Usage: SoundManager.Play("soundName", gameObject);
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private Sound[] sounds;
    [Tooltip("Default AudioMixerGroup to route sounds through. If assigned, SoundManager audio will be affected by mixer parameters (e.g. 'MusicVolume' from SettingsManager). Leave null to play sounds directly with no mixer routing.")]
    [SerializeField] private AudioMixerGroup mixerGroup;
    [Tooltip("Optional override for music-tagged sounds (use if you want a separate music bus). Falls back to mixerGroup if null.")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    
    private Dictionary<string, Sound> soundDict;
    private Dictionary<int, Dictionary<string, AudioSource>> cache;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        soundDict = new Dictionary<string, Sound>();
        cache = new Dictionary<int, Dictionary<string, AudioSource>>();
        
        foreach (var s in sounds)
        {
            if (!string.IsNullOrEmpty(s.name))
            {
                soundDict[s.name] = s;
            }
        }
    }

    /// <summary>
    /// Play a sound on an object (automatically reuses AudioSource)
    /// </summary>
    public static void Play(string soundName, GameObject obj)
    {
        Instance?.PlayInternal(soundName, obj);
    }

    /// <summary>
    /// Play a sound at a position (one-shot, not cached)
    /// </summary>
    public static void PlayAtPoint(string soundName, Vector3 position)
    {
        Instance?.PlayAtPointInternal(soundName, position);
    }

    /// <summary>
    /// Stop a sound on an object
    /// </summary>
    public static void Stop(string soundName, GameObject obj)
    {
        if (Instance == null || obj == null) return;
        
        int id = obj.GetInstanceID();
        if (Instance.cache.TryGetValue(id, out var objCache))
        {
            if (objCache.TryGetValue(soundName, out var source))
            {
                source.Stop();
            }
        }
    }
    
    /// <summary>
    /// Set the volume of a specific sound on an object (0-1)
    /// </summary>
    public static void SetVolume(string soundName, GameObject obj, float volume)
    {
        if (Instance == null || obj == null) return;
        
        int id = obj.GetInstanceID();
        if (Instance.cache.TryGetValue(id, out var objCache))
        {
            if (objCache.TryGetValue(soundName, out var source))
            {
                if (source != null)
                {
                    source.volume = Mathf.Clamp01(volume);
                }
            }
        }
    }
    
    /// <summary>
    /// Fade out a sound on an object over a duration
    /// </summary>
    public static void FadeOut(string soundName, GameObject obj, float duration = 0.3f)
    {
        if (Instance == null || obj == null) return;
        
        int id = obj.GetInstanceID();
        if (Instance.cache.TryGetValue(id, out var objCache))
        {
            if (objCache.TryGetValue(soundName, out var source))
            {
                if (source != null && source.isPlaying)
                {
                    Instance.StartCoroutine(Instance.FadeOutCoroutine(source, soundName, duration));
                }
            }
        }
    }
    
    /// <summary>
    /// Starts (or keeps playing) a looping sound at volume 0 and fades it up to its
    /// configured target volume over <paramref name="duration"/> seconds. Perfect for
    /// crossfades. The cached AudioSource is reused so subsequent SetVolume/Stop calls
    /// still work by name.
    /// </summary>
    public static void PlayWithFadeIn(string soundName, GameObject obj, float duration = 1f)
    {
        if (Instance == null || obj == null) return;
        Instance.PlayWithFadeInInternal(soundName, obj, duration);
    }
    
    /// <summary>
    /// Smoothly fades an already-playing sound from its current volume to
    /// <paramref name="targetVolume"/> over <paramref name="duration"/> seconds.
    /// Does NOT stop the sound when done (unlike FadeOut).
    /// </summary>
    public static void FadeTo(string soundName, GameObject obj, float targetVolume, float duration = 0.5f)
    {
        if (Instance == null || obj == null) return;
        
        int id = obj.GetInstanceID();
        if (Instance.cache.TryGetValue(id, out var objCache))
        {
            if (objCache.TryGetValue(soundName, out var source) && source != null)
            {
                Instance.StartCoroutine(Instance.FadeToCoroutine(source, Mathf.Clamp01(targetVolume), duration));
            }
        }
    }
    
    private IEnumerator FadeToCoroutine(AudioSource source, float target, float duration)
    {
        if (source == null) yield break;
        if (duration <= 0f) { source.volume = target; yield break; }
        
        float start = source.volume;
        float elapsed = 0f;
        while (elapsed < duration && source != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        if (source != null) source.volume = target;
    }
    
    private void PlayWithFadeInInternal(string soundName, GameObject obj, float duration)
    {
        if (!soundDict.TryGetValue(soundName, out Sound sound) || sound.clip == null)
        {
            Debug.LogWarning($"PlayWithFadeIn: Sound '{soundName}' missing or has no clip");
            return;
        }
        
        int id = obj.GetInstanceID();
        if (!cache.TryGetValue(id, out var objCache))
        {
            objCache = new Dictionary<string, AudioSource>();
            cache[id] = objCache;
        }
        
        if (!objCache.TryGetValue(soundName, out AudioSource source) || source == null)
        {
            source = obj.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.pitch = 1f;
            source.loop = sound.loop;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = GetMixerGroupFor(sound);
            objCache[soundName] = source;
        }
        else
        {
            // Keep existing source routed correctly in case the mixer group was added later
            if (source.outputAudioMixerGroup == null)
                source.outputAudioMixerGroup = GetMixerGroupFor(sound);
        }
        
        // Start muted and play, then fade up to target volume
        source.volume = 0f;
        if (!source.isPlaying) source.Play();
        StartCoroutine(FadeToCoroutine(source, sound.volume, duration));
    }
    
    private IEnumerator FadeOutCoroutine(AudioSource source, string soundName, float duration)
    {
        if (source == null) yield break;
        
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration && source != null && source.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        if (source != null)
        {
            source.Stop();
            // Restore volume to the sound's configured level for next Play()
            if (soundDict.TryGetValue(soundName, out Sound s))
                source.volume = s.volume;
            else
                source.volume = 1f;
        }
    }

    private void PlayInternal(string soundName, GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("PlayInternal: GameObject is null!");
            return;
        }
        
        if (!soundDict.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogError($"PlayInternal: Sound '{soundName}' not found in dictionary! Available sounds: {string.Join(", ", soundDict.Keys)}");
            return;
        }
        
        if (sound.clip == null)
        {
            Debug.LogError($"PlayInternal: Sound '{soundName}' has no AudioClip assigned!");
            return;
        }

        int id = obj.GetInstanceID();
        
        if (!cache.TryGetValue(id, out var objCache))
        {
            objCache = new Dictionary<string, AudioSource>();
            cache[id] = objCache;
        }

        if (!objCache.TryGetValue(soundName, out AudioSource source) || source == null)
        {
            source = obj.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume; // Use per-sound volume setting
            source.pitch = 1f; // Always normal speed
            source.loop = sound.loop;
            source.spatialBlend = 0f; // Always 2D - no distance-based volume changes
            source.playOnAwake = false;
            // Route through mixer group so volume sliders (MusicVolume/SFXVolume) affect this sound
            source.outputAudioMixerGroup = GetMixerGroupFor(sound);
            
            objCache[soundName] = source;
        }
        else if (source.outputAudioMixerGroup == null)
        {
            // Keep existing cached source routed correctly if mixerGroup was assigned later
            source.outputAudioMixerGroup = GetMixerGroupFor(sound);
        }

        if (!source.isPlaying || sound.allowOverlap)
        {
            source.Play();
        }
    }

    private void PlayAtPointInternal(string soundName, Vector3 position)
    {
        if (!soundDict.TryGetValue(soundName, out Sound sound) || sound.clip == null)
            return;

        // Build a temporary one-shot source manually so we can route it through
        // the mixer group (the stock AudioSource.PlayClipAtPoint does not let us).
        GameObject go = new GameObject($"OneShot_{soundName}");
        go.transform.position = position;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = sound.clip;
        src.volume = sound.volume;
        src.spatialBlend = 1f; // 3D at position (matches PlayClipAtPoint behavior)
        src.outputAudioMixerGroup = GetMixerGroupFor(sound);
        src.Play();
        Destroy(go, sound.clip.length + 0.1f);
    }
    
    /// <summary>
    /// Returns the appropriate AudioMixerGroup for a given sound.
    /// Music-tagged sounds go to musicMixerGroup (falling back to mixerGroup).
    /// All other sounds go to mixerGroup.
    /// </summary>
    private AudioMixerGroup GetMixerGroupFor(Sound sound)
    {
        if (sound != null && sound.isMusic)
        {
            return musicMixerGroup != null ? musicMixerGroup : mixerGroup;
        }
        return mixerGroup;
    }
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 3f)]
    public float volume = 1f;
    public bool loop = false;
    public bool allowOverlap = false;
    [Tooltip("If true, this sound is routed through SoundManager.musicMixerGroup (e.g. affected by the Music volume slider). Otherwise it uses the default mixerGroup.")]
    public bool isMusic = false;
}