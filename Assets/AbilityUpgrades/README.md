# Ability Upgrades

This folder contains AbilityUpgradeData ScriptableObjects that define level-based stats for abilities.

## Creating a new Ability Upgrade Data

1. Right-click in this folder
2. Select **Create > Abilities > Ability Upgrade Data**
3. Name it appropriately (e.g., "DamageBoost", "SpeedBoost", "Turret", etc.)
4. Configure the levels array with stats for each level

## AbilityLevelStats Fields

Each level in the `levels` array contains **3 core stats**:

| Stat | Description | Example Use |
|------|-------------|-------------|
| **damage** | Damage dealt OR effect value | Turret projectile damage, passive multipliers |
| **cooldown** | Time between uses (seconds) | Turret shoot interval, bomb cooldown |
| **duration** | How long effect lasts (seconds) | Goop puddle duration, buff duration |

Plus additional fields:
- **levelName**: Display name for this level (e.g., "Level 1", "Novice", etc.)
- **description**: Description shown in the UI for this level
- **customStats**: List of custom stat name/value pairs for ability-specific stats

### Which stat to use for which ability:

| Ability Type | Use `damage` for | Use `cooldown` for | Use `duration` for |
|--------------|------------------|--------------------|--------------------|
| **Turret** | Projectile damage | Shoot interval | - |
| **Bomb** | Explosion damage | Time between bombs | - |
| **Goop** | Damage per tick | - | Puddle lifetime |
| **Passive Buffs** | Effect value (multiplier/bonus) | - | - |

## Using Custom Stats

Custom stats allow you to define ability-specific values that aren't covered by the standard fields. Each custom stat has:
- **statName**: A string identifier (e.g., "projectileCount", "shootInterval")
- **value**: A float value

### How to read custom stats in code:

In your ability class, use `GetCustomStat()`:

```csharp
// Get a custom stat with a default fallback value
int projectileCount = Mathf.RoundToInt(GetCustomStat("projectileCount", 1f));
float shootInterval = GetCustomStat("shootInterval", 1.5f);
```

---

## Example: Damage Boost Ability (Passive)

Uses `damage` field for the damage multiplier:

```
Level 1:
  - damage: 0.15 (15% damage boost)
  - description: "Increase all damage by 15%"

Level 2:
  - damage: 0.30 (30% damage boost)
  - description: "Increase all damage by 30%"

Level 3:
  - damage: 0.50 (50% damage boost)
  - description: "Increase all damage by 50%"
```

---

## Example: Turret Ability (Active - uses damage + cooldown)

Uses:
- `damage` - Projectile damage
- `cooldown` - Shoot interval (time between shots)
- `projectileCount` custom stat - Number of darts per shot

### Level 1: Basic Turret
```
damage: 25
cooldown: 1.5
customStats:
  - statName: "projectileCount", value: 1
description: "Summon a turret that shoots enemies"
```

### Level 2: Faster Turret
```
damage: 30
cooldown: 1.2
customStats:
  - statName: "projectileCount", value: 1
description: "Turret shoots faster and deals more damage"
```

### Level 3: Double Shot
```
damage: 35
cooldown: 1.0
customStats:
  - statName: "projectileCount", value: 2
description: "Turret shoots 2 darts at once!"
```

### Level 4: Triple Shot
```
damage: 40
cooldown: 0.8
customStats:
  - statName: "projectileCount", value: 3
description: "Turret shoots 3 darts in a spread pattern!"
```

### Level 5: Rapid Fire Triple
```
damage: 50
cooldown: 0.5
customStats:
  - statName: "projectileCount", value: 3
description: "Maximum firepower! Rapid triple shots!"
```

---

## Example: Goop Ability (uses damage + duration)

Uses:
- `damage` - Damage per tick while enemies stand in goop
- `duration` - How long the goop puddle stays on the ground

### Level 1: Basic Goop
```
damage: 5
duration: 5.0
description: "Leave a damaging puddle that hurts enemies"
```

### Level 2: Stronger Goop
```
damage: 8
duration: 6.0
description: "Goop deals more damage and lasts longer"
```

### Level 3: Toxic Goop
```
damage: 12
duration: 8.0
description: "Highly toxic goop that melts enemies!"
```

---

## Example: Bomb Ability (uses damage + cooldown)

Uses:
- `damage` - Explosion damage
- `cooldown` - Time between bomb throws

### Level 1: Basic Bomb
```
damage: 50
cooldown: 5.0
description: "Throw an explosive bomb at enemies"
```

### Level 2: Bigger Boom
```
damage: 75
cooldown: 4.5
customStats:
  - statName: "explosionRadius", value: 5.0
description: "Larger explosion with more damage"
```

---

## Adding Custom Stats to Your Own Abilities

1. **Define what stats your ability needs** (e.g., "explosionRadius", "bounceCount", "chainLength")

2. **Read them in your ability class**:
```csharp
private void UpdateStatsForLevel()
{
    if (upgradeData != null)
    {
        // Read core stats
        float damage = GetDamage();
        float cooldown = GetCooldown();
        float duration = GetDuration();
        
        // Read custom stats
        float explosionRadius = GetCustomStat("explosionRadius", 5f);
        int bounceCount = Mathf.RoundToInt(GetCustomStat("bounceCount", 0f));
    }
}
```

3. **Configure in the AbilityUpgradeData asset**:
   - Set the core stats (damage, cooldown, duration) as needed
   - Add entries to the `customStats` list for ability-specific values
   - Use the exact same `statName` as in your code

---

## Linking to Abilities

### Option 1: Link to Ability Prefab
1. Open the ability prefab (e.g., TurretAbility.prefab)
2. Find the ability script component (e.g., TurretAbility)
3. Assign the AbilityUpgradeData asset to the "Upgrade Data" field

### Option 2: Link through AbilitySO
1. Open the AbilitySO asset (e.g., Turret.asset in Abilities/SO/)
2. Assign the AbilityUpgradeData to the "Upgrade Data" field

---

## Available Custom Stats by Ability

| Ability | Custom Stat | Description |
|---------|-------------|-------------|
| TurretAbility | `projectileCount` | Number of projectiles per shot |
| BombAbility | `explosionRadius` | Radius of explosion |
| GoopAbility | `slowAmount` | How much to slow enemies |
| (Add your own) | ... | ... |

---

## Quick Reference: Core Stats

```
damage   → How much damage (or effect value for passives)
cooldown → Time between uses (shoot interval, bomb cooldown)
duration → How long effect lasts (goop puddle, buff duration)
```