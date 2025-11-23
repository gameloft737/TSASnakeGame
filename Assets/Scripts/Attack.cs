using UnityEngine;

[System.Serializable]
public class AttackVariation
{
    public string attackName;
    public Material headMaterial;
    public Material bodyMaterial;
    public GameObject attachmentObject; // Can be null if no attachment
}

public abstract class Attack : MonoBehaviour
{
    [Header("Attack Stats")]
    [SerializeField] protected float damage = 10f;
    public string attackName;

    [Header("Fuel System")]
    [SerializeField] protected float minFuelToActivate = 50f;
    [SerializeField] protected float fuelRechargeRate = 20f;

    [Header("Attack Type")]
    [SerializeField] protected AttackType attackType = AttackType.Burst;
    [SerializeField] protected float burstFuelCost = 15f;
    [SerializeField] protected float continuousDrainRate = 10f;
    
    [Header("Visual Variation")]
    [SerializeField] protected AttackVariation visualVariation;

    public enum AttackType
    {
        Burst,
        Continuous
    }

    protected const float MAX_FUEL = 100f;
    protected static float sharedFuel = MAX_FUEL;
    protected bool isActive = false;

    private static Attack lastUsedAttack = null;
    protected static int activeAttackCount = 0;

    protected virtual void Update()
    {
        // Only recharge if no attacks are active and this was the last attack used
        if (!IsAnyAttackActive() && sharedFuel < MAX_FUEL && lastUsedAttack == this)
        {
            sharedFuel = Mathf.Min(MAX_FUEL, sharedFuel + fuelRechargeRate * Time.deltaTime);
        }
    }

    private static bool IsAnyAttackActive()
    {
        return activeAttackCount > 0;
    }

    public bool CanActivate()
    {
        return sharedFuel >= minFuelToActivate;
    }

    public bool TryActivate()
    {
        if (CanActivate())
        {
            isActive = true;
            activeAttackCount += 1;
            lastUsedAttack = this;
            OnActivate();

            if (attackType == AttackType.Burst)
            {
                sharedFuel = Mathf.Max(0f, sharedFuel - burstFuelCost);
                StopUsing();
            }

            return true;
        }
        return false;
    }

    public void HoldUpdate()
    {
        if (!isActive) return;

        OnHoldUpdate();

        if (attackType == AttackType.Continuous)
        {
            sharedFuel = Mathf.Max(0f, sharedFuel - continuousDrainRate * Time.deltaTime);

            if (sharedFuel <= 0f)
            {
                StopUsing();
            }
        }
    }

    public void StopUsing()
    {
        if (isActive)
        {
            isActive = false;
            activeAttackCount -= 1;
            OnDeactivate();
        }
    }

    // Getters
    public float GetFuelPercentage() => sharedFuel / MAX_FUEL;
    public float GetCurrentFuel() => sharedFuel;
    public float GetMaxFuel() => MAX_FUEL;
    public float GetMinFuelToActivate() => minFuelToActivate;
    public bool IsActive() => isActive;
    public float GetDamage() => damage;
    public AttackType GetAttackType() => attackType;
    public AttackVariation GetVisualVariation() => visualVariation;

    // Override these in child classes
    protected virtual void OnActivate() { }
    protected virtual void OnHoldUpdate() { }
    protected virtual void OnDeactivate() { }
}