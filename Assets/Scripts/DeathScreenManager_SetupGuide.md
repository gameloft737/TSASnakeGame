# Death Screen Manager Setup Guide

This guide explains how to set up the Death Screen with Restart and Quit buttons in your Snake Game.

## Overview

The `DeathScreenManager` replaces the old automatic resume behavior. When the player dies:
1. The death screen appears with "YOU'RE DEAD!" text
2. Two buttons are shown: **Restart** and **Quit**
3. The player must click one of these buttons to proceed

## Setup Steps

### 1. Create the Death Screen UI

In your Canvas (or create a new one), create the following UI hierarchy:

```
Canvas
└── DeathScreenPanel (Panel)
    ├── DeathText (TextMeshPro - Text)
    ├── RestartButton (Button)
    │   └── Text (TextMeshPro - Text) - "RESTART"
    └── QuitButton (Button)
        └── Text (TextMeshPro - Text) - "QUIT"
```

### 2. Configure the DeathScreenPanel

- Set the Panel to cover the full screen (stretch anchors)
- Add a semi-transparent dark background (e.g., black with 0.7 alpha)
- Make sure it's **disabled by default** (unchecked in Inspector)

### 3. Configure the DeathText

- Position it at the top-center of the panel
- Set the text to "YOU'RE DEAD!" (or leave empty - it will be set by script)
- Use a large, bold font
- Color: Red or white

### 4. Configure the Buttons

**Restart Button:**
- Position below the death text
- Style as desired (e.g., green background)
- Text: "RESTART"

**Quit Button:**
- Position below the restart button
- Style as desired (e.g., red background)
- Text: "QUIT"

### 5. Add the DeathScreenManager Component

1. Create an empty GameObject called "DeathScreenManager" (or add to an existing manager object)
2. Add the `DeathScreenManager` script component
3. Assign the references in the Inspector:

| Field | Description |
|-------|-------------|
| Death Screen Panel | The DeathScreenPanel GameObject |
| Death Text | The TextMeshPro text component |
| Restart Button | The Restart Button component |
| Quit Button | The Quit Button component |
| Death Animator | (Optional) Animator for death screen animations |
| Non UI | (Optional) GameObject to hide when death screen is shown |
| Main Menu Scene Name | Name of your main menu scene (default: "MainMenu") |
| Reload Current Scene | If true, restart reloads the scene (recommended) |

### 6. Update SnakeHealth Reference

The `SnakeHealth` component will automatically find the `DeathScreenManager` at runtime, but you can also assign it manually in the Inspector for better performance.

## How It Works

### Restart Button
When clicked, the game reloads the current scene, which:
- Resets all game state
- Clears all enemies
- Resets XP and level
- Clears all attacks and abilities
- Resets player stats
- Starts fresh from wave 1

### Quit Button
When clicked:
1. First tries to load the main menu scene (if it exists in build settings)
2. If no main menu scene is found, quits the application

## Customization

### Using Manual Reset Instead of Scene Reload

If you prefer not to reload the scene, set `Reload Current Scene` to `false`. The script will then manually reset:
- XP Manager
- Player Stats
- Attack Manager
- Ability Manager
- Wave Manager
- Snake Health

Note: Scene reload is recommended as it ensures a complete reset of all game state.

### Custom Main Menu Scene

Set the `Main Menu Scene Name` field to match your main menu scene name. Make sure the scene is added to your Build Settings.

## Animator Setup (Optional)

If you want animations for the death screen:

1. Create an Animator Controller
2. Add a trigger parameter called "ShowDeath"
3. Create states for showing/hiding the death screen
4. Assign the Animator to the `Death Animator` field

## Troubleshooting

### Death screen doesn't appear
- Check that `DeathScreenManager` is in the scene
- Verify all references are assigned
- Check the Console for error messages

### Buttons don't work
- Ensure the buttons have the `Button` component
- Check that the Canvas has a `GraphicRaycaster` component
- Verify there's an `EventSystem` in the scene

### Cursor doesn't appear
- The script automatically shows the cursor when the death screen appears
- If it doesn't work, check for other scripts that might be hiding the cursor

## Code Reference

The death flow is:
1. `SnakeHealth.Die()` is called when health reaches 0
2. `SnakeHealth` calls `DeathScreenManager.ShowDeathScreen()`
3. Player clicks Restart or Quit
4. `DeathScreenManager.OnRestartClicked()` or `OnQuitClicked()` is called
5. Scene reloads or returns to main menu