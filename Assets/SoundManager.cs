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
    [SerializeField] private AudioMixerGroup mixerGroup;
    
    private Dictionary<string, Sound> soundDict;
    private Dictionary<int, Dictionary<string, AudioSource>> cache;

    private void Awake()
    {
        Debug.Log("SoundManager Awake called!");
        
        if (Instance != null)
        {
            Debug.Log("SoundManager instance already exists, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        Debug.Log("Initializing SoundManager Instance");
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        soundDict = new Dictionary<string, Sound>();
        cache = new Dictionary<int, Dictionary<string, AudioSource>>();
        
        Debug.Log($"Loading {sounds.Length} sounds into dictionary...");
        foreach (var s in sounds)
        {
            if (!string.IsNullOrEmpty(s.name))
            {
                soundDict[s.name] = s;
                Debug.Log($"  - Loaded sound: '{s.name}' (Clip: {(s.clip != null ? s.clip.name : "NULL")})");
            }
        }
        Debug.Log($"SoundManager ready with {soundDict.Count} sounds!");
    }

    /// <summary>
    /// Play a sound on an object (automatically reuses AudioSource)
    /// </summary>
    public static void Play(string soundName, GameObject obj)
    {
        Debug.Log($"SoundManager.Play called - Sound: '{soundName}', Object: {(obj != null ? obj.name : "NULL")}");
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
    
    private IEnumerator FadeOutCoroutine(AudioSource source, string soundName, float duration)
    {
        if (source == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration && source != null && source.isPlaying)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        
        if (source != null)
        {
            source.Stop();
            source.volume = 1f; // Restore full volume for next play
        }
    }

    private void PlayInternal(string soundName, GameObject obj)
    {
        Debug.Log($"PlayInternal - Sound: '{soundName}', Object: {(obj != null ? obj.name : "NULL")}");
        
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

        Debug.Log($"PlayInternal: Playing sound '{soundName}' on {obj.name}");

        int id = obj.GetInstanceID();
        
        if (!cache.TryGetValue(id, out var objCache))
        {
            objCache = new Dictionary<string, AudioSource>();
            cache[id] = objCache;
            Debug.Log($"Created new cache for object: {obj.name}");
        }

        if (!objCache.TryGetValue(soundName, out AudioSource source) || source == null)
        {
            Debug.Log($"Creating new AudioSource for '{soundName}' on {obj.name}");
            source = obj.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume; // Use per-sound volume setting
            source.pitch = 1f; // Always normal speed
            source.loop = sound.loop;
            source.spatialBlend = 0f; // Always 2D - no distance-based volume changes
            source.playOnAwake = false;
            // No mixer group - play directly without any effects
            
            objCache[soundName] = source;
        }

        if (!source.isPlaying || sound.allowOverlap)
        {
            source.Play();
            Debug.Log($"Sound '{soundName}' is now playing on {obj.name}");
        }
        else
        {
            Debug.Log($"Sound '{soundName}' already playing on {obj.name} (allowOverlap=false)");
        }
    }

    private void PlayAtPointInternal(string soundName, Vector3 position)
    {
        if (!soundDict.TryGetValue(soundName, out Sound sound) || sound.clip == null)
            return;

        AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume); // Use per-sound volume setting
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
}