# TutorialPanelManager Setup Guide

## Overview
The TutorialPanelManager displays a tutorial panel that pauses the game and shows instructions to the player. It integrates with the pause menu system to prevent players from opening the pause menu while the tutorial is displayed.

## Setup Instructions

### Step 1: Create the Tutorial Panel UI

1. In your Canvas, create a new Panel (Right-click Canvas → UI → Panel)
2. Name it "TutorialPanel"
3. Set the Panel's Image color to a semi-transparent dark color (e.g., RGBA: 0, 0, 0, 200)
4. Make sure it covers the entire screen (Anchor: Stretch-Stretch, all offsets = 0)

### Step 2: Add Title Text

1. Inside TutorialPanel, create a TextMeshPro - Text (Right-click → UI → Text - TextMeshPro)
2. Name it "TitleText"
3. Position it near the top-center of the panel
4. Set font size to 48-72
5. Set alignment to Center
6. Set color to white

### Step 3: Add Instructions Text

1. Inside TutorialPanel, create another TextMeshPro - Text
2. Name it "InstructionsText"
3. Position it in the center of the panel
4. Set font size to 24-36
5. Set alignment to Center
6. Set color to white
7. Enable "Auto Size" if you want text to scale

### Step 4: Add Continue Button

1. Inside TutorialPanel, create a Button (Right-click → UI → Button - TextMeshPro)
2. Name it "ContinueButton"
3. Position it at the bottom-center of the panel
4. Set the button text to "Continue" or "Got it!"
5. Style the button as desired

### Step 5: Create TutorialPanelManager GameObject

1. Create an empty GameObject in your scene
2. Name it "TutorialPanelManager"
3. Add the TutorialPanelManager script to it
4. Assign the references in the Inspector:
   - **Tutorial Panel**: The TutorialPanel GameObject
   - **Title Text**: The TitleText component
   - **Instructions Text**: The InstructionsText component
   - **Continue Button**: The ContinueButton component

### Step 6: Initial State

1. Make sure the TutorialPanel is **disabled** (unchecked) in the hierarchy
2. The TutorialPanelManager will enable it when needed

## Using with LevelUIManager

### Triggering Tutorial After Subtitle

To show a tutorial panel after a subtitle plays:

1. In LevelUIManager, add a trigger with:
   - **Trigger Type**: XPLevel or WaveNumber
   - **Trigger Value**: The level/wave number
   - **Action Type**: ShowSubtitle
   - Configure your subtitle text

2. Add another trigger with:
   - **Trigger Type**: Same as above
   - **Trigger Value**: Same as above
   - **Action Type**: ShowTutorialPanel
   - **Delay**: Set to subtitle duration + small buffer (e.g., 3.5 seconds if subtitle is 3 seconds)
   - **Tutorial Title**: Your title text
   - **Tutorial Instructions**: Your instruction text

### Example Configuration

For a Level 1 tutorial that shows after the intro subtitle:

**Trigger 1 (Subtitle):**
- Trigger Type: XPLevel
- Trigger Value: 1
- Action Type: ShowSubtitle
- Subtitle Text: "Welcome to the game!"
- Subtitle Duration: 3

**Trigger 2 (Tutorial):**
- Trigger Type: XPLevel
- Trigger Value: 1
- Action Type: ShowTutorialPanel
- Delay: 3.5
- Tutorial Title: "How to Play"
- Tutorial Instructions: "Use WASD to move\nCollect apples to grow\nAvoid enemies!"

## API Reference

### Public Methods

```csharp
// Show the tutorial panel with custom content
TutorialPanelManager.Instance.ShowTutorial(string title, string instructions);

// Show tutorial with callback when closed
TutorialPanelManager.Instance.ShowTutorial(string title, string instructions, System.Action onClosed);

// Hide the tutorial panel and resume game
TutorialPanelManager.Instance.HideTutorial();

// Check if tutorial is currently showing
bool isShowing = TutorialPanelManager.Instance.IsTutorialShowing();
```

### Events

```csharp
// Subscribe to tutorial events
TutorialPanelManager.Instance.OnTutorialShown += () => { /* Tutorial opened */ };
TutorialPanelManager.Instance.OnTutorialHidden += () => { /* Tutorial closed */ };
```

## Behavior

When the tutorial panel is shown:
1. The game is paused (Time.timeScale = 0)
2. The pause menu is disabled (cannot be opened)
3. The cursor is unlocked and visible
4. Player input is effectively frozen

When the Continue button is clicked:
1. The tutorial panel is hidden
2. The game resumes (Time.timeScale = 1)
3. The pause menu is re-enabled
4. The cursor is locked again (if applicable)

## Integration with Other Systems

### SnakeScenePauseManager
The TutorialPanelManager automatically integrates with SnakeScenePauseManager:
- Calls `DisablePausing()` when tutorial shows
- Calls `EnablePausing()` when tutorial hides

### ScreenFadeManager
You can combine with fade effects by using multiple triggers:
1. FadeFromWhite trigger
2. ShowSubtitle trigger (with delay)
3. ShowTutorialPanel trigger (with longer delay)

## Troubleshooting

### Tutorial doesn't appear
- Check that TutorialPanelManager exists in the scene
- Verify all UI references are assigned
- Check the Console for error messages

### Game doesn't pause
- Ensure Time.timeScale is being set correctly
- Check that no other scripts are overriding Time.timeScale

### Pause menu still works during tutorial
- Verify SnakeScenePauseManager exists in the scene
- Check that DisablePausing() is being called

### Button doesn't work
- Ensure the Button component has the OnClick event connected
- The TutorialPanelManager auto-connects this in Awake()
- Make sure the button is interactable