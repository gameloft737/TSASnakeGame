# ForceField Ability Setup Guide

This guide explains how to set up the ForceField ability, which creates a protective aura around the snake that damages enemies who enter it (similar to Garlic in Vampire Survivors).

## Overview

The ForceField ability creates a circular damage zone around the snake's head. Enemies that enter this zone take damage over time. The ability scales with level, increasing radius, damage, and damage frequency.

## Creating the ForceField Ability

### Step 1: Create the Ability Prefab

1. In Unity, create a new empty GameObject
2. Name it `ForceFieldAbility`
3. Add the `ForceFieldAbility` component to it
4. Save it as a prefab in your Abilities folder

### Step 2: Configure Basic Settings

In the Inspector for the ForceFieldAbility component:

#### Force Field Settings (Fallback values if no UpgradeData)
- **Base Radius**: `3` - The starting radius of the force field
- **Base Damage**: `10` - Damage dealt per tick
- **Base Damage Interval**: `0.5` - Seconds between damage ticks

#### Visual Settings
- **Force Field Visual Prefab**: (Optional) Assign a particle system prefab for the visual effect
- **Force Field Color**: Light blue with transparency `(0.3, 0.7, 1.0, 0.4)`
- **Show Debug Gizmos**: `true` - Shows the force field radius in Scene view
- **Visual Y Offset**: `-0.5` - How far below the snake the visual sits
- **Use Particle System**: `true` - Creates a default particle system if no prefab is assigned

#### Audio
- **Damage Sound**: (Optional) Sound to play when dealing damage
- **Audio Source**: (Optional) Will auto-create if not assigned

### Step 3: Create the AbilitySO (ScriptableObject)

1. Right-click in Project window
2. Navigate to **Create > Abilities > Ability SO**
3. Name it `ForceFieldAbilitySO`

Configure the AbilitySO:
- **Ability Name**: `Force Field`
- **Ability Description**: `Creates a protective aura that damages nearby enemies`
- **Ability Icon**: Assign an appropriate icon
- **Ability Type**: `Active`
- **Ability Prefab**: Drag your `ForceFieldAbility` prefab here
- **Max Level**: `5` (or higher if using evolutions)

### Step 4: Create the AbilityUpgradeData

1. Right-click in Project window
2. Navigate to **Create > Abilities > Ability Upgrade Data**
3. Name it `ForceFieldUpgradeData`

Configure the upgrade data:

#### Ability Info
- **Ability Name**: `Force Field`
- **Ability Description**: `Creates a protective aura that damages nearby enemies`
- **Ability Icon**: Same icon as AbilitySO
- **Ability Type**: `Active`

#### Level Stats
Add 5 levels (or more for evolutions):

**Level 1:**
- Level Name: `Force Field I`
- Description: `Creates a small protective aura`
- Damage: `10`
- Cooldown: `0.5` (damage interval)
- Duration: `0` (permanent while active)
- Custom Stats:
  - `radius`: `3`

**Level 2:**
- Level Name: `Force Field II`
- Description: `Increased radius and damage`
- Damage: `15`
- Cooldown: `0.45`
- Custom Stats:
  - `radius`: `4`

**Level 3:**
- Level Name: `Force Field III`
- Description: `Further increased radius and damage`
- Damage: `20`
- Cooldown: `0.4`
- Custom Stats:
  - `radius`: `5`

**Level 4:**
- Level Name: `Force Field IV`
- Description: `Large protective aura`
- Damage: `25`
- Cooldown: `0.35`
- Custom Stats:
  - `radius`: `6`

**Level 5:**
- Level Name: `Force Field V`
- Description: `Maximum protection`
- Damage: `30`
- Cooldown: `0.3`
- Custom Stats:
  - `radius`: `7`

### Step 5: Link the Upgrade Data

1. Select your `ForceFieldAbility` prefab
2. In the Inspector, find the **Upgrade Data** field
3. Drag `ForceFieldUpgradeData` into this field

### Step 6: Add to Ability Pool

1. Find your `AbilityDrop` or ability selection system
2. Add `ForceFieldAbilitySO` to the available abilities pool

## Custom Visual Effects

### Option 1: Use Default Particle System

If you don't assign a visual prefab, the ability will create a default particle system with:
- Circular emission around the player
- Swirling particles that orbit the Y axis
- Fade in/out effect
- Color matching the `forceFieldColor` setting

### Option 2: Create Custom Particle System

1. Create a new Particle System
2. Configure it as a ring/circle effect:
   - **Shape**: Circle
   - **Radius**: Will be controlled by the ability
   - **Emission**: Continuous
   - **Render Mode**: Billboard or Mesh
3. Save as prefab
4. Assign to `Force Field Visual Prefab` field

### Option 3: Use Cylinder Visual (Fallback)

Set `Use Particle System` to `false` to use a simple transparent cylinder as the visual.

## How the Ability Works

1. **Activation**: When the ability is acquired, it immediately activates
2. **Visual Creation**: Creates the force field visual around the player
3. **Enemy Detection**: Every frame, finds all enemies within the radius
4. **Damage Ticks**: At regular intervals (based on cooldown), deals damage to all enemies in range
5. **Scaling**: Radius and damage scale with level and PlayerStats multipliers

## Integration with PlayerStats

The ForceField ability automatically integrates with PlayerStats:

- **Damage Multiplier**: `GetFieldDamage()` multiplies base damage by `PlayerStats.GetDamageMultiplier()`
- **Range Multiplier**: `GetRadius()` multiplies base radius by `PlayerStats.GetRangeMultiplier()`

This means passive abilities that boost damage or range will also affect the force field!

## Code Reference

### Key Methods

```csharp
// Get current radius (includes level scaling and PlayerStats multiplier)
private float GetRadius()

// Get current damage (includes level scaling and PlayerStats multiplier)
private float GetFieldDamage()

// Get damage interval (decreases at higher levels)
private float GetDamageInterval()

// Find all enemies in the force field
private void FindEnemiesInField()

// Deal damage to all enemies currently in the field
private void DealDamageToEnemiesInField()
```

### Custom Stats Used

The ability reads these custom stats from the upgrade data:
- `radius` - The base radius of the force field

## Adding Evolutions (Optional)

To add evolutions to the ForceField ability:

### Step 1: Create EvolutionData

1. Right-click in Project window
2. Navigate to **Create > Upgrades > Evolution Data**
3. Name it `ForceFieldEvolutionData`

### Step 2: Configure Evolutions

Example evolution with Frost Aura passive:

- **Required Passive Prefab**: `FrostAuraAbility` prefab
- **Required Passive Level**: `1`
- **Unlocked Level**: `6`
- **Evolution Name**: `Frozen Barrier`
- **Evolution Description**: `Force Field now slows enemies and deals frost damage`

### Step 3: Add Evolution Level Stats

Add Level 6 to your `ForceFieldUpgradeData`:

**Level 6 (Frozen Barrier):**
- Level Name: `Frozen Barrier`
- Description: `Force Field slows enemies and deals frost damage`
- Damage: `35`
- Cooldown: `0.25`
- Custom Stats:
  - `radius`: `8`
  - `slowPercent`: `0.3` (30% slow)
  - `frostDamage`: `5` (additional frost damage)

### Step 4: Link Evolution Data

1. Select `ForceFieldUpgradeData`
2. Find the **Evolution System** section
3. Drag `ForceFieldEvolutionData` into the **Evolution Data** field

## Troubleshooting

### Force Field Not Appearing
- Check that the ability is being activated (check console for activation message)
- Verify the player transform is being found
- Check if `Use Particle System` is enabled

### Enemies Not Taking Damage
- Verify enemies have the `AppleEnemy` component
- Check that enemies are within the radius (use debug gizmos)
- Ensure the damage interval is reasonable (not too long)

### Visual Not Following Player
- The visual should automatically follow the player
- Check `UpdateVisualPosition()` is being called in Update

### Performance Issues
- Reduce particle count in custom particle systems
- Increase damage interval to reduce damage calculations
- Consider using a simpler visual (cylinder instead of particles)

## Example Scene Setup

1. Player with `PlayerMovement` component
2. Enemies with `AppleEnemy` component
3. `AbilityManager` in scene
4. `ForceFieldAbility` prefab added to ability pool
5. (Optional) `PlayerStats` for damage/range multipliers

## Summary

The ForceField ability provides a passive damage aura that:
- Damages all enemies within its radius
- Scales with level (radius, damage, speed)
- Integrates with PlayerStats for additional scaling
- Supports custom visual effects
- Can be evolved with passive ability combinations

This creates a powerful defensive/offensive ability that rewards players for staying close to enemies while providing consistent area damage.