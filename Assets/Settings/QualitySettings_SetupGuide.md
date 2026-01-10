# Quality Settings Setup Guide

This guide explains how to configure the graphics quality levels in Unity to work with the SettingsManager.

## Overview

The SettingsManager uses Unity's built-in Quality Settings system. The quality dropdown in the settings menu is populated from `QualitySettings.names`, which are defined in Unity's Project Settings.

## Setting Up Quality Levels

### Step 1: Open Quality Settings
1. In Unity, go to **Edit > Project Settings**
2. Select **Quality** from the left panel

### Step 2: Add Quality Levels
By default, Unity may only have a few quality levels. To add the full range (Low, Medium, High, Ultra):

1. Click the **Add Quality Level** button (+ icon) to add new levels
2. Name them appropriately:
   - **Low** - For low-end hardware
   - **Medium** - For mid-range hardware  
   - **High** - For high-end hardware
   - **Ultra** - For maximum quality

### Step 3: Assign Render Pipeline Assets
For each quality level, you need to assign the corresponding URP asset:

1. Select a quality level by clicking on it
2. In the **Rendering** section, find **Render Pipeline Asset**
3. Assign the appropriate asset:
   - Low → `Assets/Settings/Low_RPAsset.asset`
   - Medium → `Assets/Settings/Medium_RPAsset.asset`
   - High → `Assets/Settings/High_RPAsset.asset`
   - Ultra → `Assets/Settings/Ultra_RPAsset.asset`

### Step 4: Configure Each Quality Level
For each quality level, configure these settings:

#### Low Quality
- Pixel Light Count: 1
- Texture Quality: Half Res
- Anisotropic Textures: Disabled
- Anti Aliasing: Disabled
- Soft Particles: Disabled
- Realtime Reflection Probes: Disabled
- Shadows: Hard Shadows Only
- Shadow Resolution: Low
- Shadow Distance: 30

#### Medium Quality
- Pixel Light Count: 2
- Texture Quality: Full Res
- Anisotropic Textures: Per Texture
- Anti Aliasing: Disabled
- Soft Particles: Enabled
- Realtime Reflection Probes: Disabled
- Shadows: Hard and Soft Shadows
- Shadow Resolution: Medium
- Shadow Distance: 40

#### High Quality
- Pixel Light Count: 4
- Texture Quality: Full Res
- Anisotropic Textures: Forced On
- Anti Aliasing: 2x
- Soft Particles: Enabled
- Realtime Reflection Probes: Enabled
- Shadows: Hard and Soft Shadows
- Shadow Resolution: High
- Shadow Distance: 50

#### Ultra Quality
- Pixel Light Count: 8
- Texture Quality: Full Res
- Anisotropic Textures: Forced On
- Anti Aliasing: 4x
- Soft Particles: Enabled
- Realtime Reflection Probes: Enabled
- Shadows: Hard and Soft Shadows
- Shadow Resolution: Very High
- Shadow Distance: 100

### Step 5: Set Default Quality Level
1. In the Quality Settings, you'll see a grid showing platforms
2. Click on the green checkmark under each platform to set the default quality level
3. Recommended: Set **High** as default for Standalone builds

## URP Asset Differences

The URP assets created have these key differences:

| Setting | Low | Medium | High | Ultra |
|---------|-----|--------|------|-------|
| Render Scale | 0.7 | 0.85 | 1.0 | 1.0 |
| MSAA | Off | Off | 2x | 4x |
| Shadow Resolution | 512 | 1024 | 2048 | 4096 |
| Shadow Distance | 30 | 40 | 50 | 100 |
| Shadow Cascades | 1 | 2 | 2 | 4 |
| Additional Lights | Off | 2 | 4 | 8 |
| Soft Shadows | Off | Off | On | On |
| HDR | Off | On | On | On |
| Reflection Probes | Off | Off | On | On |

## Verifying Setup

1. Run the game
2. Open the Settings menu
3. The Graphics dropdown should show all quality levels
4. Changing the quality level should:
   - Log a message to the console: `[SettingsManager] Setting quality to level X (LevelName)`
   - Apply the new quality settings immediately
   - Save the preference for next session

## Troubleshooting

### Dropdown only shows "PC" or limited options
- Make sure you've added all quality levels in Project Settings > Quality
- Each quality level needs a unique name

### Quality changes don't seem to apply
- Check the Console for the debug log message
- Verify the Render Pipeline Asset is assigned for each quality level
- Some changes (like shadow resolution) may require scene reload

### Resolution changes don't work
- Check Console for: `[SettingsManager] Setting resolution to WxH`
- Resolution changes may not be visible in windowed mode
- Try toggling fullscreen to see the change