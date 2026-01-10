# Objective Wireframe Outline Setup Guide

This guide explains how to add a glowing wireframe outline around your objective triggers so players can easily identify where to go next.

## Quick Setup (2 Steps)

### Step 1: Add the ObjectiveOutline Component

1. Select your **ObjectiveTrigger** GameObject in the Hierarchy
2. Click **Add Component** in the Inspector
3. Search for and add **ObjectiveOutline**

### Step 2: Make Sure You Have a Collider

The wireframe outline draws around the collider bounds. Make sure your ObjectiveTrigger has a collider (BoxCollider, SphereCollider, CapsuleCollider, etc.)

## That's It!

The wireframe will automatically:
- ✅ Draw a glowing box around the **current active objective's collider**
- ✅ Pulse with a breathing animation
- ✅ Hide when the objective is completed
- ✅ Transfer to the **next objective** automatically
- ✅ Update if the object moves

## Configuration Options

### Wireframe Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Outline Color** | Color of the wireframe lines | Golden Yellow |
| **Line Width** | Thickness of the wireframe lines | 0.05 |
| **Padding** | Extra space around the collider | 0.1 |

### Animation Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Pulse Effect** | Enable pulsing animation | true |
| **Pulse Speed** | Speed of the pulse | 1.5 |
| **Pulse Min Alpha** | Minimum opacity during pulse | 0.3 |
| **Pulse Max Alpha** | Maximum opacity during pulse | 1.0 |

### Glow Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Emission Intensity** | Brightness of the glow | 2.0 |

## How It Works

1. The script finds the Collider on the ObjectiveTrigger (or its children)
2. It calculates the world-space bounding box of the collider
3. It creates 12 LineRenderers to draw the edges of the box
4. The lines pulse in opacity to create a breathing effect
5. If the object moves, the wireframe updates automatically

## Troubleshooting

### "I can't see the wireframe!"

1. **Check for a Collider** - The ObjectiveTrigger needs a Collider component
2. **Check the Console** - Look for:
   - `ObjectiveOutline: ENABLED wireframe for [ObjectName]`
   - If you see "No collider found!", add a BoxCollider

3. **Increase Line Width** - Try setting Line Width to 0.1 or higher

4. **Check Outline Color** - Make sure it's not black or transparent

5. **Verify ObjectiveManager** - Make sure ObjectiveManager exists and has objectives set up

### "The wireframe is too small/big!"

- Adjust the **Padding** value to add more space around the collider
- The wireframe matches the collider's bounds, so resize the collider if needed

### "The wireframe shows on the wrong objective!"

- Check that each ObjectiveTrigger has the correct `myObjectiveIndex` value
- Index 0 = first objective, Index 1 = second, etc.

## Debug Mode

Enable `Show Debug Info` to see console logs:

```
ObjectiveOutline [ObjectName]: Found collider: BoxCollider
ObjectiveOutline [ObjectName]: Current=0, My=0
ObjectiveOutline [ObjectName]: shouldBeActive=true, isActive=false
ObjectiveOutline [ObjectName]: Created wireframe with 12 lines, bounds: ...
ObjectiveOutline: ENABLED wireframe for ObjectName
```

## Example Scene Setup

```
Scene
├── ObjectiveManager (with Auto Update Outlines = true)
│   └── objectives[0] = Objective1
│   └── objectives[1] = Objective2
│
├── Objective1 (GameObject)
│   ├── ObjectiveTrigger (myObjectiveIndex = 0)
│   ├── ObjectiveOutline
│   └── BoxCollider (trigger) ← Required!
│
└── Objective2 (GameObject)
    ├── ObjectiveTrigger (myObjectiveIndex = 1)
    ├── ObjectiveOutline
    └── BoxCollider (trigger) ← Required!
```

## Tips

1. **Use BoxCollider** for the clearest wireframe visualization
2. **Increase Line Width** to 0.1 for better visibility from a distance
3. **Use bright colors** like yellow, cyan, or green
4. **Add Padding** of 0.2-0.5 to make the wireframe stand out from the object
5. The wireframe is visible through walls - this helps players find objectives!

## Editor Preview

When you select an ObjectiveTrigger with ObjectiveOutline, you'll see a yellow wireframe gizmo in the Scene view showing where the outline will appear.