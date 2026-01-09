# Classic Snake Mode - Setup Guide

This system adds a classic 2D top-down snake game mode that can be toggled with right-click during gameplay.

---

## Creating a Top-Down Camera with Cinemachine (Step-by-Step Tutorial)

### What is Cinemachine?
Cinemachine is Unity's camera system that makes it easy to create professional camera behaviors without writing complex code. Your project already uses it for the normal and aim cameras.

### Step-by-Step: Create a Top-Down Camera

#### Step 1: Open Your Scene
1. Open Unity and load your main game scene

#### Step 2: Create a Cinemachine Camera
1. In the menu bar, go to **GameObject > Cinemachine > Cinemachine Camera**
   - If you don't see this option, make sure Cinemachine is installed via Package Manager
2. A new "CinemachineCamera" object will appear in your Hierarchy
3. Rename it to **"TopDownCamera"** (right-click > Rename)

#### Step 3: Position the Camera for Top-Down View
1. Select your new TopDownCamera in the Hierarchy
2. In the **Inspector**, find the **Transform** component
3. Set these values:
   ```
   Position: X = 10, Y = 15, Z = 10  (adjust X and Z to center on your play area)
   Rotation: X = 90, Y = 0, Z = 0    (this makes it look straight down)
   Scale:    X = 1,  Y = 1,  Z = 1
   ```

#### Step 4: Configure Camera Settings
1. With TopDownCamera selected, look at the **CinemachineCamera** component in Inspector
2. Set **Priority** to **0** (so it doesn't activate immediately)
3. Under **Lens**:
   - **Field of View**: 60 (or adjust to see more/less of the play area)
   - For a true 2D look, you can switch to **Orthographic**:
     - Check "Orthographic" if available, or
     - On the Main Camera, set Projection to Orthographic

#### Step 5: (Optional) Make Camera Follow the Snake
If you want the camera to follow the snake head:
1. In the CinemachineCamera component, find **Tracking Target**
2. Drag your snake head object from the Hierarchy into this field
3. Under **Position Control**, select **Follow**
4. Adjust **Damping** values for smooth following

#### Step 6: Test the Camera
1. To test, temporarily set **Priority** to **100** (higher than other cameras)
2. Enter Play Mode - you should see the top-down view
3. Set Priority back to **0** when done testing

### Understanding Camera Priority
Cinemachine uses a priority system:
- The camera with the **highest priority** is active
- Your normal camera might have priority 2
- Your aim camera might have priority 1
- Set top-down camera to priority 100 when you want it active, 0 when inactive

### Code to Switch Cameras
Here's how to switch to your top-down camera via code:

```csharp
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera topDownCamera;
    public CinemachineCamera normalCamera;
    
    public void SwitchToTopDown()
    {
        topDownCamera.Priority = 100;  // Make it active
        normalCamera.Priority = 0;      // Deactivate others
    }
    
    public void SwitchToNormal()
    {
        topDownCamera.Priority = 0;
        normalCamera.Priority = 100;
    }
}
```

### Visual Guide: Inspector Settings

```
┌─────────────────────────────────────────────────────┐
│ TopDownCamera                                        │
├─────────────────────────────────────────────────────┤
│ Transform                                            │
│   Position    X: 10    Y: 15    Z: 10               │
│   Rotation    X: 90    Y: 0     Z: 0    ← Look down │
│   Scale       X: 1     Y: 1     Z: 1                │
├─────────────────────────────────────────────────────┤
│ Cinemachine Camera                                   │
│   Priority: 0  ← Set to 100 to activate             │
│   Lens                                               │
│     Field of View: 60                                │
│   Tracking Target: (none or snake head)             │
└─────────────────────────────────────────────────────┘
```

### Tips for a Good Top-Down View

1. **Height (Y position)**: Higher = see more area, Lower = more zoomed in
2. **Field of View**: Lower = more zoomed in, Higher = see more
3. **Orthographic**: For true 2D look without perspective distortion
4. **Damping**: Higher values = smoother but slower camera movement

---

## Quick Setup (5 Minutes)

### Step 1: Add the ClassicModeManager to Your Scene

1. In Unity, go to your main game scene
2. Create an empty GameObject: `GameObject > Create Empty`
3. Name it **"ClassicModeManager"**
4. Add the `ClassicModeManager` component to it:
   - Select the GameObject
   - Click `Add Component`
   - Search for "ClassicModeManager" and add it

### Step 2: Configure Grid Settings (Optional)

The ClassicModeManager has several settings you can adjust in the Inspector:

| Setting | Default | Description |
|---------|---------|-------------|
| **Grid Cell Size** | 1 | Size of each grid cell in world units |
| **Grid Width** | 20 | Number of cells horizontally |
| **Grid Height** | 20 | Number of cells vertically |
| **Grid Origin** | (0,0,0) | World position of grid's bottom-left corner |
| **Classic Move Interval** | 0.15 | Time between snake movements (lower = faster) |
| **Initial Snake Length** | 3 | Starting body segments |
| **Apples In Classic Mode** | 3 | Number of apples on screen |

### Step 3: Play!

1. Enter Play Mode
2. **Right-click** to toggle between normal 3D mode and classic 2D mode
3. In classic mode:
   - Use **WASD** or **Arrow Keys** to change direction
   - Collect red apples to grow
   - Avoid hitting your own body
   - The snake wraps around screen edges

---

## How It Works

### Mode Switching
When you right-click:
1. The camera switches to a top-down orthographic view
2. Normal 3D movement is frozen
3. Enemy spawning is paused
4. A grid is displayed
5. The snake moves in discrete grid steps
6. Apples spawn on the grid

### Components Created

The system creates three new scripts:

1. **ClassicModeManager.cs** - Main controller that:
   - Handles mode switching
   - Manages the top-down camera
   - Spawns and tracks classic apples
   - Creates the grid visualization

2. **ClassicSnakeController.cs** - Snake movement that:
   - Moves on a grid at fixed intervals
   - Handles WASD/Arrow input
   - Manages body segments
   - Detects collisions with apples and self

3. **ClassicApple.cs** - Apple behavior that:
   - Animates with bobbing/rotation
   - Handles collection
   - Notifies manager when eaten

---

## Advanced Configuration

### Custom Prefabs

You can assign custom prefabs in the ClassicModeManager Inspector:

- **Classic Apple Prefab**: Custom apple visual (default: red sphere)
- **Classic Body Segment Prefab**: Custom body segment (default: green cube)
- **Grid Visualizer Prefab**: Custom grid visual (default: line renderers)

### Adjusting the Camera

The system automatically creates a Cinemachine camera for top-down view. To customize:

1. Create your own Cinemachine Camera in the scene
2. Position it above your play area looking down (rotation: 90, 0, 0)
3. Assign it to the **Top Down Camera** field in ClassicModeManager

### Grid Positioning

To position the grid in a specific area:

1. Set **Grid Origin** to the bottom-left corner of where you want the grid
2. Adjust **Grid Width** and **Grid Height** for the play area size
3. Use the Scene view gizmos to visualize the grid bounds (select the ClassicModeManager)

---

## Troubleshooting

### "Right-click doesn't work"
- Make sure the ClassicModeManager GameObject is active
- Check that PlayerControls input actions are set up (the game should already have this)
- Verify no other script is consuming the right-click input first

### "Snake doesn't move"
- Ensure you're in classic mode (camera should be top-down)
- Check that the ClassicSnakeController is enabled
- Try pressing WASD or Arrow keys

### "Apples don't spawn"
- Check the console for errors
- Verify Grid Width and Grid Height are > 0
- Make sure Grid Origin is in a valid position

### "Camera doesn't switch"
- Ensure CameraManager exists in the scene
- Check that Cinemachine is properly set up
- Verify the top-down camera has higher priority when active

---

## Integration Notes

### Compatibility
This system is designed to work alongside your existing:
- PlayerMovement (frozen during classic mode)
- SnakeBody (hidden during classic mode)
- EnemySpawner (paused during classic mode)
- CameraManager (priorities adjusted during classic mode)

### Events
You can subscribe to mode changes:
```csharp
ClassicModeManager.Instance.OnModeChanged += (isClassic) => {
    Debug.Log($"Mode changed to: {(isClassic ? "Classic" : "Normal")}");
};
```

### Extending
To add features like scoring or game over screens:
1. Subscribe to `OnModeChanged` event
2. Use `ClassicSnakeController.GetLength()` for score
3. Override `OnSnakeCollision()` for custom game over behavior

---

## Files Created

```
Assets/Scripts/
├── ClassicModeManager.cs    - Main mode controller
├── ClassicSnakeController.cs - Grid-based snake movement
├── ClassicApple.cs          - Apple collection behavior
└── ClassicMode_README.md    - This documentation
```

---

## Controls Summary

| Mode | Control | Action |
|------|---------|--------|
| Both | Right-Click | Toggle classic mode |
| Classic | W / ↑ | Move up |
| Classic | S / ↓ | Move down |
| Classic | A / ← | Move left |
| Classic | D / → | Move right |

---

*Created for TSA Snake Game - Classic Mode Extension*