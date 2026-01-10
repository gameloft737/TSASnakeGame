# Screen Fade Manager Setup Guide

This guide explains how to set up the screen fade system for fade-to-white effects at game start, when reaching certain levels, and for scene transitions.

## Overview

The system consists of two components:
1. **ScreenFadeManager** - Handles the actual fade effects using a full-screen UI Image
2. **LevelUIManager** - Triggers fade effects and scene loads at specific XP levels or wave numbers

## Setup Instructions

### Step 1: Create the Fade UI

1. In your scene, create a new **Canvas** (or use an existing UI Canvas)
   - Set **Render Mode** to "Screen Space - Overlay"
   - Set **Sort Order** to a high value (e.g., 100) so it renders on top of everything

2. Create an **Image** as a child of the Canvas
   - Name it "FadeImage"
   - Set **Anchor** to stretch in all directions (hold Alt and click the bottom-right anchor preset)
   - Set **Left, Right, Top, Bottom** all to 0
   - Set **Color** to white (255, 255, 255) with **Alpha = 255** (fully opaque)
   - The image should cover the entire screen

### Step 2: Add the ScreenFadeManager

1. Create an empty GameObject in your scene
   - Name it "ScreenFadeManager"

2. Add the **ScreenFadeManager** script to this GameObject

3. Configure the settings:
   - **Fade Image**: Drag your FadeImage from Step 1
   - **Default Fade Duration**: How long fades take (default: 1 second)
   - **Fade In On Start**: Enable this for the initial fade from white when the game starts
   - **Initial Fade Delay**: How long to wait before starting the initial fade (default: 0.5s)
   - **Show Subtitle After Initial Fade**: Enable to show "Level 1" subtitle after the initial fade
   - **Initial Subtitle Text**: The text to show (e.g., "Level 1")
   - **Initial Subtitle Duration**: How long to show the subtitle

### Step 3: Configure Level-Based Fade Triggers

To trigger a fade when reaching a specific level:

1. Find or create a **LevelUIManager** in your scene

2. In the **Triggers** list, add a new trigger:
   - **Trigger Type**: XPLevel (or WaveNumber for wave-based triggers)
   - **Trigger Value**: The level number that triggers the fade (e.g., 5)
   - **Trigger Once**: Usually enabled
   - **Action Type**: Choose one of:
     - `FadeToWhite` - Fades to white and stays white
     - `FadeFromWhite` - Fades from white to clear
     - `FadeToWhiteAndBack` - Fades to white, holds, then fades back

3. Configure the fade settings:
   - **Fade Duration**: How long the fade takes
   - **Fade Hold Duration**: (For FadeToWhiteAndBack) How long to stay at full white
   - **Show Subtitle After Fade**: Enable to show a subtitle when the fade completes
   - **Fade Subtitle Text**: The subtitle text to show
   - **Fade Subtitle Duration**: How long to show the subtitle

## Example Configurations

### Initial Game Start (Fade From White + Level 1 Subtitle)

On the **ScreenFadeManager**:
- Fade In On Start: ✓ Enabled
- Initial Fade Delay: 0.5
- Show Subtitle After Initial Fade: ✓ Enabled
- Initial Subtitle Text: "Level 1"
- Initial Subtitle Duration: 3

**Note:** Level 1 doesn't need "Wait For Attack UI" since no attack menu appears at level 1.

### Level 5 Transition (Fade To White And Back + Subtitle)

On the **LevelUIManager**, add a trigger:
- Trigger Type: XPLevel
- Trigger Value: 5
- Trigger Once: ✓ Enabled
- **Wait For Attack UI: ✓ Enabled** (waits for attack selection menu to close first)
- Action Type: FadeToWhiteAndBack
- Fade Duration: 1
- Fade Hold Duration: 0.5
- Show Subtitle After Fade: ✓ Enabled
- Fade Subtitle Text: "Level 5 - The Challenge Begins"
- Fade Subtitle Duration: 3

### Wave 10 Boss Introduction

On the **LevelUIManager**, add a trigger:
- Trigger Type: WaveNumber
- Trigger Value: 10
- Trigger Once: ✓ Enabled
- Wait For Attack UI: ✗ Disabled (wave triggers don't overlap with attack UI)
- Action Type: FadeToWhiteAndBack
- Fade Duration: 1.5
- Fade Hold Duration: 1
- Show Subtitle After Fade: ✓ Enabled
- Fade Subtitle Text: "BOSS WAVE"
- Fade Subtitle Duration: 4

### Level 10 - Load New Scene (with Fade)

On the **LevelUIManager**, add a trigger:
- Trigger Type: XPLevel
- Trigger Value: 10
- Trigger Once: ✓ Enabled
- **Wait For Attack UI: ✓ Enabled** (waits for player to finish selecting their upgrade)
- Action Type: FadeAndLoadScene
- Fade Duration: 1.5
- Scene To Load: "Level2Scene" (the name of your scene in Build Settings)
- Additive Scene Load: ✗ Disabled (replaces current scene)

### Load Scene Immediately (No Fade)

On the **LevelUIManager**, add a trigger:
- Trigger Type: XPLevel
- Trigger Value: 15
- Trigger Once: ✓ Enabled
- Wait For Attack UI: ✓ Enabled
- Action Type: LoadScene
- Scene To Load: "BonusLevel"
- Additive Scene Load: ✗ Disabled

## Wait For Attack UI Feature

The **Wait For Attack UI** option is crucial for preventing overlapping UI elements. When enabled:

1. The trigger waits for the Attack Selection UI to open (if it's going to)
2. Then waits for the player to make their selection and close the menu
3. Only then does it execute the trigger action (subtitle, fade, scene load, etc.)

**When to use:**
- ✓ Enable for XP Level triggers at level 2 and above (attack menu appears on level up)
- ✓ Enable for any trigger where you want to ensure the player has finished their selection
- ✗ Disable for Level 1 triggers (no attack menu at level 1)
- ✗ Disable for Wave triggers (wave changes don't trigger the attack menu)

## API Reference

### ScreenFadeManager Methods

```csharp
// Fade to white
ScreenFadeManager.Instance.FadeToWhite(duration, onComplete);

// Fade from white to clear
ScreenFadeManager.Instance.FadeFromWhite(duration, onComplete);

// Fade to white, hold, then fade back
ScreenFadeManager.Instance.FadeToWhiteAndBack(fadeInDuration, holdDuration, fadeOutDuration, onFadeToWhiteComplete, onComplete);

// Instant white
ScreenFadeManager.Instance.SetWhite();

// Instant clear
ScreenFadeManager.Instance.SetClear();

// Check if fading
bool isFading = ScreenFadeManager.Instance.IsFading;
```

### LevelUIManager Scene Loading Methods

```csharp
// Load a scene with fade to white
LevelUIManager.Instance.LoadSceneWithFade("SceneName", fadeDuration, additive);

// Load a scene immediately (no fade)
LevelUIManager.Instance.LoadSceneImmediate("SceneName", additive);
```

### Events

```csharp
// Subscribe to fade completion events
ScreenFadeManager.Instance.OnFadeToWhiteComplete += () => { /* code */ };
ScreenFadeManager.Instance.OnFadeFromWhiteComplete += () => { /* code */ };
```

## Troubleshooting

### Fade not visible
- Make sure the Canvas Sort Order is high enough to render on top
- Check that the FadeImage covers the entire screen
- Verify the FadeImage color is white with full alpha

### Subtitle not showing after fade
- Ensure SubtitleUI exists in the scene
- Check that "Show Subtitle After Fade" is enabled
- Verify the subtitle text is not empty

### Fade happens at wrong time
- Double-check the Trigger Value matches the expected level/wave
- Ensure Trigger Type is correct (XPLevel vs WaveNumber)
- Check if "Trigger Once" is preventing re-triggers

### Scene not loading
- Make sure the scene is added to Build Settings (File > Build Settings > Add Open Scenes)
- Verify the scene name matches exactly (case-sensitive)
- Check the console for error messages

### Scene loads but no fade in new scene
- The ScreenFadeManager in the old scene is destroyed when loading a new scene
- Add a ScreenFadeManager to the new scene with "Fade In On Start" enabled
- This will create a smooth fade-from-white effect when the new scene loads

## Dependencies

- **SubtitleUI**: Required for showing subtitles after fades
- **XPManager**: Required for XP level-based triggers
- **WaveManager**: Required for wave-based triggers