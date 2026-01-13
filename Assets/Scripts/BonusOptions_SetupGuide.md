# Bonus Options Setup Guide

This guide explains how to configure the bonus options that appear when all abilities are maxed out.

## Overview

When a player picks up an ability drop but all abilities are already at max level, the game will display bonus options instead of ability upgrades. By default, two options are available:
- **Heal** - Restores health to the player
- **Speed Boost** - Temporarily increases movement speed

The bonus options use the **same AbilityButton prefab** as regular abilities, so the option name, description, and icon will display in the same TextMeshPro fields used for ability names, descriptions, and icons.

## Setup Instructions

### 1. Configure Bonus Options on AbilityCollector

1. Select the player GameObject that has the `AbilityCollector` component
2. In the Inspector, find the **"Bonus Options (When All Abilities Maxed)"** section
3. Add bonus options to the list:

#### Adding a Heal Option:
- Click the **+** button to add a new element
- Set **Option Name**: "Heal" (displays in the ability name text field)
- Set **Description**: "Restore 25% of your max health" (displays in the description text field)
- Set **Icon**: Assign your heal icon sprite (displays in the ability icon image)
- Set **Bonus Type**: `Heal`
- Set **Heal Amount**: 25 (this is a percentage if `Heal Is Percentage` is checked)
- Check **Heal Is Percentage**: ✓ (to heal based on max health percentage)

#### Adding a Speed Boost Option:
- Click the **+** button to add a new element
- Set **Option Name**: "Speed Boost" (displays in the ability name text field)
- Set **Description**: "Move 50% faster for 5 seconds" (displays in the description text field)
- Set **Icon**: Assign your speed boost icon sprite (displays in the ability icon image)
- Set **Bonus Type**: `SpeedBoost`
- Set **Speed Multiplier**: 1.5 (1.5 = 50% faster)
- Set **Speed Boost Duration**: 5 (seconds)

### 2. UI Field Mapping

The bonus options use the same UI elements as abilities in the `AbilityButton` prefab:

| BonusOption Field | AbilityButton UI Element |
|-------------------|--------------------------|
| `optionName` | `abilityNameText` (TextMeshProUGUI) |
| `description` | `descriptionText` (TextMeshProUGUI) |
| `icon` | `abilityIcon` (Image) |

The following UI elements are hidden for bonus options:
- `levelText` - Set to empty string (bonus options don't have levels)
- `newIndicator` - Hidden
- `upgradeIndicator` - Hidden
- `evolutionPairingText` - Hidden

## BonusOption Properties

| Property | Type | Description |
|----------|------|-------------|
| `optionName` | string | Display name for the option (shown in ability name field) |
| `description` | string | Description text (shown in description field) |
| `icon` | Sprite | Icon displayed (shown in ability icon image) |
| `bonusType` | BonusType | Either `Heal` or `SpeedBoost` |
| `healAmount` | float | Amount to heal (or percentage if `healIsPercentage` is true) |
| `healIsPercentage` | bool | If true, `healAmount` is treated as a percentage of max health |
| `speedMultiplier` | float | Speed multiplier (1.5 = 50% faster) |
| `speedBoostDuration` | float | How long the speed boost lasts in seconds |

## Background Colors

The `AbilityButton` component has configurable colors for bonus options:
- **Heal Color**: Green (`0.4, 1, 0.4`) - Applied when bonus type is Heal
- **Speed Boost Color**: Yellow/Gold (`1, 0.8, 0.2`) - Applied when bonus type is SpeedBoost

You can customize these colors in the AbilityButton prefab's Inspector under the "Colors" section.

## How It Works

1. When the player collects an ability drop, `AbilityCollector.PopulateAbilityButtons()` is called
2. The system calls `GetRandomAbilities()` to find valid abilities to upgrade
3. If no valid abilities are found (all maxed), `PopulateBonusOptionButtons()` is called instead
4. The same `abilityButtonPrefab` is instantiated for each bonus option
5. `AbilityButton.InitializeBonusOption()` is called to set up the button with bonus option data
6. When clicked, `AbilityCollector.SelectBonusOption()` applies the effect and closes the menu

## Speed Boost Behavior

- Speed boosts only count time while the player is not frozen (paused)
- If a new speed boost is selected while one is active, the old one is cancelled and the new one starts fresh
- Original speeds are restored when the boost ends

## Files

- `Assets/Scripts/BonusOption.cs` - Data class for bonus options
- `Assets/Drops/AbilityButton.cs` - Modified to support both abilities and bonus options via `InitializeBonusOption()`
- `Assets/Scripts/AbilityCollector.cs` - Added bonus option support