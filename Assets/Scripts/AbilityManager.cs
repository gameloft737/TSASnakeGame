using UnityEngine;
using System.Collections.Generic;

public class AbilityManager : MonoBehaviour
{
    [Header("Ability Container")]
    [SerializeField] private Transform abilityContainer;
    
    [Header("Drop Settings")]
    [SerializeField] private Camera worldSpaceCamera;
    
    [SerializeField] private List<BaseAbility> activeAbilities = new List<BaseAbility>();

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

    public BaseAbility AddAbility(GameObject abilityPrefab)
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
        
        GameObject abilityObject = Instantiate(abilityPrefab, abilityContainer);
        BaseAbility ability = abilityObject.GetComponent<BaseAbility>();
        
        if (ability != null)
        {
            activeAbilities.Add(ability);
        }
        
        return ability;
    }

    public void RemoveAbility(BaseAbility ability)
    {
        if (activeAbilities.Contains(ability))
        {
            activeAbilities.Remove(ability);
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
}