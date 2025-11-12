using UnityEngine;

public abstract class Attack : MonoBehaviour
{
    [Header("Attack Stats")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float cooldown = 1f;

    protected float lastUseTime = -999f;

    public bool CanUse()
    {
        return Time.time >= lastUseTime + cooldown;
    }

    public void TryUse()
    {
        if (CanUse())
        {
            Use();
            lastUseTime = Time.time;
        }
    }

    protected abstract void Use();

    public float GetDamage() => damage;
    public float GetCooldown() => cooldown;
    public float GetCooldownRemaining() => Mathf.Max(0f, (lastUseTime + cooldown) - Time.time);
}