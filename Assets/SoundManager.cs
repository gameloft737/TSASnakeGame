using System;
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
            source.volume = sound.volume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;
            source.spatialBlend = sound.is3D ? 1f : 0f;
            source.minDistance = sound.minDistance;
            source.maxDistance = sound.maxDistance;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = mixerGroup;
            
            objCache[soundName] = source;
        }

        if (sound.pitchVariation > 0)
            source.pitch = sound.pitch + UnityEngine.Random.Range(-sound.pitchVariation, sound.pitchVariation);

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

        AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume);
    }
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.05f;
    
    public bool is3D = true;
    public float minDistance = 1f;
    public float maxDistance = 50f;
    
    public bool loop = false;
    public bool allowOverlap = false;

    // Constructor to ensure defaults are set when created in Inspector
    public Sound()
    {
        volume = 1f;
        pitch = 1f;
        pitchVariation = 0.05f;
        is3D = true;
        minDistance = 1f;
        maxDistance = 50f;
        loop = false;
        allowOverlap = false;
    }
}