using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    [Header("Ability Container")]
    [SerializeField] private Transform abilityContainer;
    
    [Header("Drop Settings")]
    [SerializeField] private Camera worldSpaceCamera;
    
    [Header("Ability Limits")]
    [SerializeField] private int maxPassiveAbilities = 4;
    [SerializeField] private int maxActiveAbilities = 4;
    
    [SerializeField] private List<BaseAbility> activeAbilities = new List<BaseAbility>();
    
    // Track ability types using AbilitySO references
    private Dictionary<BaseAbility, AbilitySO> abilitySOMap = new Dictionary<BaseAbility, AbilitySO>();

    private void Start()
    {
        // Create container if not assigned
        if (abilityContainer == null)
        {
            GameObject container = new GameObject("AbilityContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            abilityContainer = container.transform;
        }
        
        // Find main camera if not assigned
        if (worldSpaceCamera == null)
        {
            worldSpaceCamera = Camera.main;
        }
    }

    public T AddAbility<T>() where T : BaseAbility
    {
        GameObject abilityObject = new GameObject(typeof(T).Name);
        abilityObject.transform.SetParent(abilityContainer);
        abilityObject.transform.localPosition = Vector3.zero;
        
        T ability = abilityObject.AddComponent<T>();
        activeAbilities.Add(ability);
        
        return ability;
    }

    public BaseAbility AddAbility(GameObject abilityPrefab, AbilitySO abilitySO = null)
    {
        if (abilityPrefab == null) return null;
        
        // Check if ability already exists
        BaseAbility existingAbility = GetAbility(abilityPrefab);
        if (existingAbility != null)
        {
            // Level up existing ability instead of adding new one
            existingAbility.LevelUp();
            return existingAbility;
        }
        
        // Check ability type limits if we have the SO
        if (abilitySO != null)
        {
            if (abilitySO.abilityType == AbilityType.Passive && GetPassiveAbilityCount() >= maxPassiveAbilities)
            {
                Debug.LogWarning($"Cannot add passive ability {abilitySO.abilityName}: max passive abilities ({maxPassiveAbilities}) reached!");
                return null;
            }
            if (abilitySO.abilityType == AbilityType.Active && GetActiveAbilityCount() >= maxActiveAbilities)
            {
                Debug.LogWarning($"Cannot add active ability {abilitySO.abilityName}: max active abilities ({maxActiveAbilities}) reached!");
                return null;
            }
        }
        
        GameObject abilityObject = Instantiate(abilityPrefab, abilityContainer);
        BaseAbility ability = abilityObject.GetComponent<BaseAbility>();
        
        if (ability != null)
        {
            activeAbilities.Add(ability);
            if (abilitySO != null)
            {
                abilitySOMap[ability] = abilitySO;
            }
        }
        
        return ability;
    }

    public void RemoveAbility(BaseAbility ability)
    {
        if (activeAbilities.Contains(ability))
        {
            activeAbilities.Remove(ability);
            abilitySOMap.Remove(ability);
            Destroy(ability.gameObject);
        }
    }

    public List<BaseAbility> GetActiveAbilities()
    {
        return new List<BaseAbility>(activeAbilities);
    }
    
    public bool HasAbility<T>() where T : BaseAbility
    {
        foreach (var ability in activeAbilities)
        {
            if (ability is T)
            {
                return true;
            }
        }
        return false;
    }
    
    public Camera GetWorldSpaceCamera() => worldSpaceCamera;

    public BaseAbility GetAbility(GameObject abilityPrefab)
    {
        if (abilityPrefab == null) return null;
        
        string prefabName = abilityPrefab.name;
        
        foreach (var ability in activeAbilities)
        {
            if (ability != null && ability.gameObject.name.Contains(prefabName))
            {
                return ability;
            }
        }
        
        return null;
    }

    public T GetAbilityOfType<T>() where T : BaseAbility
    {
        foreach (var ability in activeAbilities)
        {
            if (ability is T typedAbility)
            {
                return typedAbility;
            }
        }
        
        return null;
    }

    public bool HasAbility(GameObject abilityPrefab)
    {
        return GetAbility(abilityPrefab) != null;
    }

    public int GetAbilityLevel(GameObject abilityPrefab)
    {
        BaseAbility ability = GetAbility(abilityPrefab);
        return ability != null ? ability.GetCurrentLevel() : 0;
    }
    
    /// <summary>
    /// Gets the AbilitySO associated with an ability
    /// </summary>
    public AbilitySO GetAbilitySO(BaseAbility ability)
    {
        if (ability != null && abilitySOMap.ContainsKey(ability))
        {
            return abilitySOMap[ability];
        }
        return null;
    }
    
    /// <summary>
    /// Gets the count of passive abilities currently active
    /// </summary>
    public int GetPassiveAbilityCount()
    {
        int count = 0;
        foreach (var kvp in abilitySOMap)
        {
            if (kvp.Value != null && kvp.Value.abilityType == AbilityType.Passive)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Gets the count of active abilities currently active
    /// </summary>
    public int GetActiveAbilityCount()
    {
        int count = 0;
        foreach (var kvp in abilitySOMap)
        {
            if (kvp.Value != null && kvp.Value.abilityType == AbilityType.Active)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Checks if a new passive ability can be added
    /// </summary>
    public bool CanAddPassiveAbility()
    {
        return GetPassiveAbilityCount() < maxPassiveAbilities;
    }
    
    /// <summary>
    /// Checks if a new active ability can be added
    /// </summary>
    public bool CanAddActiveAbility()
    {
        return GetActiveAbilityCount() < maxActiveAbilities;
    }
    
    /// <summary>
    /// Checks if an ability can be added or upgraded
    /// </summary>
    public bool CanAddOrUpgradeAbility(AbilitySO abilitySO)
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return false;
        
        // Check if already have this ability
        BaseAbility existingAbility = GetAbility(abilitySO.abilityPrefab);
        if (existingAbility != null)
        {
            // Can upgrade if not at max level
            return existingAbility.GetCurrentLevel() < abilitySO.maxLevel;
        }
        
        // New ability - check type limits
        if (abilitySO.abilityType == AbilityType.Passive)
        {
            return CanAddPassiveAbility();
        }
        else
        {
            return CanAddActiveAbility();
        }
    }
    
    // Getters for limits
    public int GetMaxPassiveAbilities() => maxPassiveAbilities;
    public int GetMaxActiveAbilities() => maxActiveAbilities;
}