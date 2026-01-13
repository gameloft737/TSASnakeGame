# EndlessModeManager Setup Guide

## Overview
The `EndlessModeManager` allows you to add an "Endless Mode" button to your Arcade scene that disables the win condition scene change. Players can play indefinitely without being transitioned to a final cutscene.

When the player clicks the Endless Mode button, a confirmation popup appears asking them to confirm their choice. After confirming:
1. The endless mode flag is set (disabling win condition)
2. The game automatically starts (via MainMenuManager.OnPlayClicked())

## Quick Setup

### Step 1: Add the EndlessModeManager to your Arcade Scene
1. Open your **Arcade** scene
2. Create an empty GameObject: `GameObject > Create Empty`
3. Name it "EndlessModeManager"
4. Add the `EndlessModeManager` component to it
5. **(Optional)** Drag your `MainMenuManager` into the **Main Menu Manager** field
   - If not assigned, it will auto-find one in the scene

### Step 2: Create the Endless Mode Button
1. In your Arcade scene, find your existing Start button
2. Duplicate it (Ctrl+D) to create the Endless Mode button
3. Rename it to "EndlessModeButton" or similar
4. Change the button text to "Endless Mode" or "∞ Endless"
5. Position it near the regular Start button

### Step 3: Configure Button OnClick Events

#### For the Regular Start Button:
1. Select your regular Start button
2. In the Inspector, find the Button component's **On Click ()** section
3. Click the **+** button to add a new event
4. Drag the **EndlessModeManager** GameObject into the object field
5. Select **EndlessModeManager > SetRegularMode()** from the dropdown
6. Keep your existing start logic (MainMenuManager.OnPlayClicked, etc.) as additional OnClick events

#### For the Endless Mode Button:
1. Select your Endless Mode button
2. In the Inspector, find the Button component's **On Click ()** section
3. Click the **+** button to add a new event
4. Drag the **EndlessModeManager** GameObject into the object field
5. Select **EndlessModeManager > ShowEndlessModeConfirmation()** from the dropdown
6. **That's it!** The confirmation panel's confirm button will automatically start the game

### Step 4: Confirmation Panel (Optional)
The script will **automatically create** a confirmation panel at runtime if you don't provide one. However, if you want to customize the look:

1. Create a Panel in your Canvas
2. Add a title TextMeshProUGUI
3. Add a message TextMeshProUGUI
4. Add two buttons: "Cancel" and "Start Endless"
5. Assign these to the EndlessModeManager component in the Inspector

## How It Works

### Regular Mode
- Player clicks the regular Start button
- `SetRegularMode()` is called, ensuring the win condition is active
- Your existing start logic (MainMenuManager.OnPlayClicked) runs
- The game plays normally through the arcade scene
- When the player reaches the win rank, the `LevelUIManager` triggers the scene change to the final cutscene

### Endless Mode
- Player clicks the Endless Mode button
- A confirmation popup appears with "Cancel" and "Start Endless" buttons
- If confirmed, `SetEndlessMode()` is called:
  - The win rank is set to 0 (disabled)
  - All `FadeAndLoadScene` and `LoadScene` triggers in `LevelUIManager` are marked as triggered
  - **The game automatically starts** (via MainMenuManager.OnPlayClicked())
- When the player reaches what would be the win rank, nothing happens - they can keep playing indefinitely
- The "You Won!" announcement will never appear
- Level announcements still work normally

## Important Notes

- **The game starts automatically** after confirming endless mode
- If you have a `MainMenuManager` in the scene, it will be used to start the game
- Alternatively, use the **On Endless Mode Confirmed** event for custom start logic
- **The EndlessModeManager persists across scenes** using `DontDestroyOnLoad`
- When a new scene loads (like the Snake scene), the manager automatically applies the endless mode settings to the `LevelUIManager`
- When the player quits to the main menu and starts a new game, they can choose either mode

## Configuration Options

### Game Start Settings
| Property | Description |
|----------|-------------|
| **Main Menu Manager** | Reference to MainMenuManager (auto-found if not assigned) |
| **On Endless Mode Confirmed** | UnityEvent invoked after setting endless mode - use for custom start logic if MainMenuManager is not used |

### Confirmation Panel
| Property | Description |
|----------|-------------|
| **Confirmation Panel** | The panel GameObject (auto-created if not assigned) |
| **Confirm Button** | The button that enables endless mode and starts the game |
| **Cancel Button** | The button that closes the panel |
| **Title Text** | TextMeshProUGUI for the panel title |
| **Message Text** | TextMeshProUGUI for the panel message |

### Confirmation Panel Text
| Property | Description |
|----------|-------------|
| **Confirmation Title** | Title shown on the popup (default: "Endless Mode") |
| **Confirmation Message** | Message explaining endless mode |
| **Confirm Button Text** | Text on the confirm button (default: "Start Endless") |
| **Cancel Button Text** | Text on the cancel button (default: "Cancel") |

### Auto-Create Panel Settings
| Property | Description |
|----------|-------------|
| **Auto Create Panel** | If true, creates a panel at runtime if none assigned |
| **Target Canvas** | Canvas to parent the auto-created panel to |

### Debug
| Property | Description |
|----------|-------------|
| **Debug Mode** | Enable to see debug logs in the console |

## Advanced Usage

### Checking if Endless Mode is Active
You can check if the current game session is in endless mode from any script:

```csharp
if (EndlessModeManager.IsEndlessMode)
{
    // Do something specific for endless mode
    // e.g., show a different UI, track high scores differently
}
```

### Resetting Mode When Returning to Menu
If you want to ensure the mode is reset when returning to the main menu:

```csharp
// Call this when loading the main menu
EndlessModeManager.ResetToRegularMode();
```

### Programmatically Controlling the Confirmation Panel
```csharp
// Show the confirmation panel
EndlessModeManager.Instance.ShowEndlessModeConfirmation();

// Hide the confirmation panel
EndlessModeManager.Instance.HideConfirmationPanel();

// Set endless mode directly (skips confirmation)
EndlessModeManager.Instance.SetEndlessMode();

// Set regular mode
EndlessModeManager.Instance.SetRegularMode();
```

## Troubleshooting

### The scene change still happens in endless mode
- Make sure the `EndlessModeManager` is in your Arcade scene
- Verify that `SetEndlessMode()` is being called (enable Debug Mode)
- Check that the `LevelUIManager` exists in your scene

### The button doesn't do anything
- Verify the OnClick event is properly configured
- Make sure the EndlessModeManager GameObject is active
- Check the console for any error messages

### The confirmation panel doesn't appear
- Make sure **Auto Create Panel** is enabled, or assign a custom panel
- Ensure there's a Canvas in the scene for the auto-created panel
- Check the console for error messages

## Example Button Setup

```
Regular Start Button OnClick:
  1. EndlessModeManager.SetRegularMode()
  2. MainMenuManager.OnPlayClicked()  (or your existing start logic)

Endless Mode Button OnClick:
  1. EndlessModeManager.ShowEndlessModeConfirmation()
  
(The confirmation panel's Confirm button will:
  1. Set endless mode
  2. Automatically call MainMenuManager.OnPlayClicked())
```

## Example Scene Hierarchy

```
Arcade Scene
├── Canvas
│   ├── MainMenuPanel
│   │   ├── TitleText
│   │   ├── StartButton              <- Calls SetRegularMode() + existing start logic
│   │   ├── EndlessModeButton        <- Calls ShowEndlessModeConfirmation()
│   │   ├── SettingsButton
│   │   └── QuitButton
│   ├── EndlessModeConfirmPanel      <- (Optional) Custom confirmation panel
│   │   ├── TitleText
│   │   ├── MessageText
│   │   ├── CancelButton
│   │   └── ConfirmButton
│   └── ...
├── EndlessModeManager               <- Add this GameObject
├── LevelUIManager                   <- Your existing level UI manager
└── ...