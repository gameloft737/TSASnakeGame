# Classic Snake Mode - Setup Guide

This guide explains how to set up the classic 2D top-down snake mode that can be toggled with the Tab key.

## Overview

The classic mode keeps the smooth movement of your 3D snake game but restricts directions to 4 cardinal directions (no diagonals):
- **Smooth movement**: Snake moves smoothly like normal, but can only turn in 90-degree increments
- **Top-down camera**: Camera switches to a bird's-eye view following the snake
- **Normal body following**: Body segments follow the head naturally (same as normal mode)
- **Cardinal-only enemies**: Apple enemies move smoothly but only in cardinal directions (N, E, S, W)
- **Full menu support**: Drop/attack menus work normally with the spinning dolly camera

## Quick Setup (3 Steps)

### Step 1: Create the ClassicModeManager GameObject

1. In your Unity scene, create an empty GameObject: `GameObject > Create Empty`
2. Name it "ClassicModeManager"
3. Add the `ClassicModeManager` script to it

That's it! The script will automatically find all necessary references.

### Step 2: (Optional) Adjust Settings

Select the ClassicModeManager GameObject and adjust these settings in the Inspector:

**Movement Settings:**
- Movement is smooth (not grid-based)
- Snake can only turn 90 degrees at a time (A/D or Left/Right arrows)
- Enemies move smoothly but only in cardinal directions

**Camera Settings:**
- `Camera Height`: How high the top-down camera is (default: 20 units)
- `Camera Damping`: How smoothly the camera follows (default: 0.5)

**Visual Settings:**
- `Show Grid Lines`: Toggle grid visualization
- `Grid Line Color`: Color of the grid lines
- `Grid Visible Radius`: How many cells around the snake to show

**Toggle Key:**
- `Toggle Key`: Key to toggle classic mode (default: Tab)

### Step 3: Play!

- Press **Tab** to toggle between normal and classic mode
- Use **WASD** or **Arrow Keys** to control the snake in classic mode
- Press **Tab** again to return to normal 3D mode
- **Menus work normally**: When you collect a drop or level up, the game pauses and shows the spinning dolly camera as usual

## How It Works

When you toggle classic mode:

1. **PlayerMovement** uses absolute direction controls (W=North, S=South, A=West, D=East)
2. **SnakeBody** works exactly the same as normal mode (bodies follow head naturally)
3. **AppleEnemy** instances move smoothly but only in cardinal directions (N, E, S, W)
4. **Camera** switches to a top-down view following the snake head

All changes are reversible - exiting classic mode restores normal 3D gameplay with free rotation.

### Menu Integration

When menus open (drop collection, attack selection, level up):
1. The game pauses as normal
2. Camera switches to the spinning dolly/pause camera
3. You can select abilities/attacks as usual
4. When the menu closes, the camera returns to top-down view (if in classic mode)

This means you get the full menu experience even while in classic mode!

### Key Differences from Normal Mode

| Feature | Normal Mode | Classic Mode |
|---------|-------------|--------------|
| Snake Controls | Mouse/keyboard rotation | WASD = absolute directions |
| Snake Movement | Smooth, any direction | Smooth, 4 directions |
| Enemy Movement | Smooth, any direction | Smooth, 4 directions |
| Camera | 3rd person / Aim | Top-down |
| Body Following | Natural | Natural (same) |
| Reversing | Allowed | Not allowed (like classic Snake) |

## Technical Details

### Modified Scripts

The following existing scripts have been modified to support classic mode:

- **PlayerMovement.cs**: Added `SetClassicMode()` method and grid-based movement logic
- **SnakeBody.cs**: Added `SetClassicMode()` method and position history tracking
- **AppleEnemy.cs**: Added `SetClassicMode()` method and grid-based AI movement
- **CameraManager.cs**: Already had `SetFrozen()` method for input blocking

### New Script

- **ClassicModeManager.cs**: Orchestrates the mode switching and manages the top-down camera

### Key Methods

```csharp
// Toggle classic mode programmatically
ClassicModeManager.Instance.ToggleClassicMode();

// Enter/exit classic mode directly
ClassicModeManager.Instance.EnterClassicMode();
ClassicModeManager.Instance.ExitClassicMode();

// Check if in classic mode
bool isClassic = ClassicModeManager.Instance.IsClassicMode;

// Check if a menu is currently open
bool menuOpen = ClassicModeManager.Instance.IsMenuOpen();

// Notify when menus open/close (called automatically by AbilityCollector and AttackSelectionUI)
ClassicModeManager.Instance.OnMenuOpened();
ClassicModeManager.Instance.OnMenuClosed();

// Subscribe to mode change events
ClassicModeManager.Instance.OnModeChanged += (bool isClassicMode) => {
    Debug.Log($"Mode changed to: {(isClassicMode ? "Classic" : "Normal")}");
};
```

## Customization

### Changing the Toggle Key

By default, classic mode is toggled with the Tab key. To change this:

1. Select the ClassicModeManager GameObject in your scene
2. In the Inspector, find the "Toggle Key" field
3. Change it to your preferred KeyCode (e.g., `F1`, `BackQuote`, etc.)

Alternatively, in code:
```csharp
// The toggle key is checked in Update()
if (Input.GetKeyDown(toggleKey) && !isMenuOpen && !isTransitioning)
{
    ToggleClassicMode();
}
```

### Custom Top-Down Camera

If you want to use your own Cinemachine camera instead of the auto-generated one:

1. Create a CinemachineCamera in your scene
2. Set it up with:
   - Position above the play area
   - Rotation: (90, 0, 0) to look straight down
   - Follow target: Your snake head transform
3. Assign it to the `Top Down Camera` field on ClassicModeManager

### Adjusting Enemy Behavior

Enemies move slower than the snake by default. Adjust `Enemy Move Interval` to change this:
- Lower values = faster enemies (more challenging)
- Higher values = slower enemies (easier)

## Troubleshooting

### Snake doesn't move in classic mode
- Ensure the PlayerMovement script is on your snake head
- Check that WASD/Arrow keys aren't bound to other actions

### Body segments don't follow properly
- Ensure SnakeBody script is present and has body parts assigned
- Check the console for any errors during mode switch

### Camera doesn't switch
- Ensure CameraManager is in the scene
- Check that the top-down camera was created (look for "ClassicMode_TopDownCamera" in hierarchy)

### Enemies don't move on grid
- Enemies need NavMeshAgent component (it gets disabled in classic mode)
- Check that AppleEnemy scripts are on your enemy objects

## Performance Notes

- Grid visualization uses LineRenderers that update each frame
- Disable `Show Grid Lines` for better performance on mobile
- The grid only renders around the snake (controlled by `Grid Visible Radius`)