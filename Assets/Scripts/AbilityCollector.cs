using UnityEngine;

[RequireComponent(typeof(AbilityManager))]
public class AbilityCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectionEffectPrefab;
    [SerializeField] private GameObject upgradeEffectPrefab;
    
    private AbilityManager abilityManager;

    private void Awake()
    {
        abilityManager = GetComponent<AbilityManager>();
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect) return;
        
        AbilityDrop drop = other.GetComponent<AbilityDrop>();
        if (drop != null && drop.IsGrounded() && !drop.IsCollected())
        {
            TryCollectDrop(drop);
        }
    }

    public bool TryCollectDrop(AbilityDrop drop)
    {
        if (drop == null || drop.IsCollected() || !drop.IsGrounded())
        {
            return false;
        }
        
        AbilitySO abilitySO = drop.GetAbilitySO();
        if (abilitySO == null || abilitySO.abilityPrefab == null)
        {
            drop.Collect();
            return false;
        }
        
        AbilityDrop.DropType dropType = drop.GetDropType();
        BaseAbility existingAbility = abilityManager.GetAbility(abilitySO.abilityPrefab);
        
        if (dropType == AbilityDrop.DropType.New)
        {
            BaseAbility newAbility = abilityManager.AddAbility(abilitySO.abilityPrefab);
            if (newAbility != null)
            {
                PlayEffect(drop.transform.position, false);
                drop.Collect();
                return true;
            }
        }
        else if (dropType == AbilityDrop.DropType.Upgrade && existingAbility != null)
        {
            existingAbility.LevelUp();
            PlayEffect(drop.transform.position, true);
            drop.Collect();
            return true;
        }
        else if (dropType == AbilityDrop.DropType.Duration && existingAbility != null)
        {
            existingAbility.LevelUp(); // This extends duration even at max level
            PlayEffect(drop.transform.position, false);
            drop.Collect();
            return true;
        }
        
        drop.Collect();
        return false;
    }

    private void PlayEffect(Vector3 position, bool isUpgrade)
    {
        AudioClip sound = isUpgrade ? upgradeSound : collectSound;
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }
        
        GameObject effectPrefab = isUpgrade ? upgradeEffectPrefab : collectionEffectPrefab;
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}