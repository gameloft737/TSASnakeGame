# Main Menu Setup Guide for Arcade Scene

This guide explains how to set up the main menu UI overlay in your Arcade scene so that it displays over the arcade view, and seamlessly transitions to the intro cutscene when Play is clicked.

## Overview

The system works as follows:
1. When the Arcade scene loads, the main menu UI is displayed over the arcade scene view
2. The FPS controller is disabled (no movement/look) and cursor is visible
3. Player can interact with menu buttons (Play, Settings, Quit)
4. When Play is clicked, the UI fades out smoothly
5. The intro cutscene animation starts seamlessly
6. After the cutscene, player control is enabled

## Step-by-Step Setup

### 1. Create the Main Menu Canvas

1. In your **Arcade scene**, create a new Canvas:
   - Right-click in Hierarchy → UI → Canvas
   - Name it `MainMenuCanvas`

2. Configure the Canvas:
   - **Render Mode**: Screen Space - Overlay
   - **Sort Order**: 100 (to ensure it's on top)

3. Add a **Canvas Group** component to the Canvas:
   - This enables the fade-out effect
   - Set Alpha to 1, Interactable and Blocks Raycasts to true

### 2. Create the Main Menu Panel

1. Create a Panel as a child of MainMenuCanvas:
   - Right-click MainMenuCanvas → UI → Panel
   - Name it `MainMenuPanel`
   - Set the background color/image as desired (or transparent to see arcade behind)

2. Add your menu elements as children:
   ```
   MainMenuPanel
   ├── TitleText (TextMeshPro)
   ├── PlayButton (Button)
   ├── SettingsButton (Button)
   └── QuitButton (Button)
   ```

3. Style your buttons and text as desired

### 3. Create the Settings Panel

1. Create another Panel as a child of MainMenuCanvas:
   - Name it `SettingsPanel`
   - Set it to **inactive** by default (uncheck the checkbox in Inspector)

2. Add settings UI elements:
   ```
   SettingsPanel
   ├── SettingsTitle (TextMeshPro)
   ├── AudioSection
   │   ├── MasterVolumeSlider
   │   ├── MusicVolumeSlider
   │   └── SFXVolumeSlider
   ├── GraphicsSection
   │   ├── QualityDropdown (TMP_Dropdown)
   │   ├── ResolutionDropdown (TMP_Dropdown)
   │   ├── FullscreenToggle
   │   └── VSyncToggle
   ├── ControlsSection
   │   ├── SensitivitySlider
   │   └── InvertYToggle
   └── BackButton
   ```

### 4. Set Up the MainMenuManager

1. Create an empty GameObject in the scene:
   - Name it `MainMenuManager`

2. Add the `MainMenuManager` script component

3. Assign references in the Inspector:
   - **Main Menu Panel**: Drag your MainMenuPanel
   - **Settings Panel**: Drag your SettingsPanel
   - **Play Button**: Drag your PlayButton
   - **Settings Button**: Drag your SettingsButton
   - **Settings Back Button**: Drag your BackButton from SettingsPanel
   - **Quit Button**: Drag your QuitButton
   - **Menu Canvas Group**: Drag the CanvasGroup from MainMenuCanvas
   - **Fade Out Duration**: Set to 0.5 (or your preferred duration)
   - **Intro Cutscene Controller**: Drag your CutsceneController object
   - **FPS Controller**: Drag your EasyPeasyFirstPersonController object

### 5. Set Up the SettingsManager

1. Add the `SettingsManager` script to the same MainMenuManager GameObject (or create a separate one)

2. Assign references:
   - All sliders, dropdowns, and toggles from your SettingsPanel
   - **FPS Controller**: Same reference as MainMenuManager
   - **Audio Mixer** (optional): If you have an AudioMixer set up

### 6. Configure the CutsceneController

1. Select your existing CutsceneController object

2. Ensure it has:
   - **Main Camera**: Your FPS camera
   - **Cutscene Camera**: The camera used for the intro animation
   - **Cutscene Animator**: The animator with your intro animation
   - **Cutscene Duration**: Match your animation length

3. The CutsceneController's auto-start has been disabled - it will now be triggered by MainMenuManager

### 7. Set Up the Intro Animation

1. Your cutscene camera should have an Animator component

2. Create an animation clip for the intro:
   - Animate camera position, rotation, etc.
   - Name it appropriately (e.g., "IntroCutscene")

3. In the CutsceneController, update the animation name:
   - In `CutsceneRoutine()`, change `"YourAnimationClipName"` to your actual animation name

## Scene Hierarchy Example

```
Arcade Scene
├── MainMenuManager (with MainMenuManager + SettingsManager scripts)
├── MainMenuCanvas
│   ├── MainMenuPanel
│   │   ├── TitleText
│   │   ├── PlayButton
│   │   ├── SettingsButton
│   │   └── QuitButton
│   └── SettingsPanel (inactive by default)
│       ├── ... settings UI elements
│       └── BackButton
├── FPSController (EasyPeasyFirstPersonController)
│   └── Camera
├── CutsceneController
│   └── CutsceneCamera (with Animator)
├── Arcade Environment
│   └── ... your arcade models, lights, etc.
└── ... other scene objects
```

## Flow Diagram

```
Scene Loads
    ↓
MainMenuManager.Start()
    ↓
FPS Controller disabled, cursor visible
Menu UI shown (alpha = 1)
    ↓
[User clicks Play]
    ↓
MainMenuManager.OnPlayClicked()
    ↓
TransitionToGame() coroutine starts
    ↓
Menu UI fades out (alpha → 0)
    ↓
Menu panel deactivated
Cursor locked
    ↓
CutsceneController.StartCutscene()
    ↓
Intro animation plays
    ↓
Animation ends
    ↓
FPS Controller enabled
Player can move/look
```

## Tips

1. **Background Visibility**: If you want the arcade to be visible behind the menu, use a semi-transparent panel or no panel at all, just floating UI elements.

2. **Camera Position**: Position your FPS controller where you want the player to start after the cutscene ends.

3. **Cutscene Camera Start**: The cutscene camera should start from a position that makes sense for the intro (e.g., outside the arcade, looking at the building).

4. **Audio**: Add background music that plays during the menu and transitions smoothly into gameplay.

5. **Testing**: Test the flow by playing the scene - you should see the menu, click Play, watch it fade, see the cutscene, then gain control.

## Troubleshooting

- **Menu doesn't appear**: Check that MainMenuPanel is active and Canvas is set to Screen Space - Overlay
- **Can't click buttons**: Ensure Canvas has a GraphicRaycaster and EventSystem exists in scene
- **Cutscene doesn't play**: Verify CutsceneController reference is assigned and animation name matches
- **Player moves during menu**: Ensure FPS Controller reference is assigned in MainMenuManager
- **Settings don't save**: SettingsManager uses PlayerPrefs - check for errors in console