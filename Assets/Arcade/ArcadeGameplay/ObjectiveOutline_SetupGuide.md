# Objective Outline Setup Guide

This guide explains how to add glowing outlines to your objective triggers so players can easily identify where to go next.

## Quick Setup (2 Steps)

### Step 1: Add the ObjectiveOutline Component

1. Select your **ObjectiveTrigger** GameObject in the Hierarchy
2. Click **Add Component** in the Inspector
3. Search for and add **ObjectiveOutline**

### Step 2: Choose Your Outline Method

In the ObjectiveOutline component, select one of these methods:

| Method | Description | Best For |
|--------|-------------|----------|
| **FloatingIndicator** (Default) | Creates a glowing cube that floats and rotates above the objective | Most visible, works everywhere |
| **LightBeacon** | Creates a tall light beam shooting up from the objective | Large outdoor areas |
| **EmissionGlow** | Makes the object itself glow | Objects with Standard shader materials |

## That's It!

The outline will automatically:
- ✅ Show on the **current active objective** only
- ✅ Hide when the objective is completed
- ✅ Transfer to the **next objective** automatically
- ✅ Animate with pulsing/bobbing effects

## Configuration Options

### Floating Indicator Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Float Height** | Height above the object | 2 |
| **Indicator Size** | Size of the floating cube | 0.5 |
| **Bob Animation** | Enable up/down bobbing | true |
| **Bob Amplitude** | How far it bobs | 0.3 |
| **Bob Speed** | How fast it bobs | 2 |
| **Rotate Indicator** | Enable rotation | true |
| **Rotation Speed** | Degrees per second | 90 |

### Light Beacon Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Beacon Height** | Height of the light beam | 10 |
| **Beacon Width** | Width at the base | 1 |
| **Beacon Intensity** | Light brightness | 2 |

### Common Settings

| Property | Description | Default |
|----------|-------------|---------|
| **Outline Color** | Color of the glow | Golden Yellow |
| **Pulse Effect** | Enable pulsing animation | true |
| **Pulse Speed** | Speed of pulse | 1.5 |
| **Emission Intensity** | Glow brightness | 3 |

## Troubleshooting

### "I can't see the outline!"

1. **Check the Console** - Look for debug messages like:
   - `ObjectiveOutline: ENABLED for [ObjectName]`
   - If you don't see this, the outline isn't being activated

2. **Verify ObjectiveManager is set up**:
   - Make sure you have an ObjectiveManager in your scene
   - Check that `Auto Update Outlines` is enabled on it

3. **Check objective indices**:
   - The `myObjectiveIndex` on your ObjectiveTrigger must match its position in the ObjectiveManager's objectives array

4. **Try FloatingIndicator method**:
   - This is the most visible method
   - It creates a physical object you can see in the Scene view

5. **Increase Float Height**:
   - If the indicator is inside the object, increase `Float Height`

### "The outline shows on the wrong objective!"

- Check that each ObjectiveTrigger has the correct `myObjectiveIndex` value
- Index 0 = first objective, Index 1 = second, etc.

### "The outline doesn't disappear after completing!"

- Make sure you're calling `ObjectiveManager.Instance.CompleteObjective(index)`
- The ObjectiveManager will automatically update all outlines

## Debug Mode

Enable `Show Debug Info` on the ObjectiveOutline component to see detailed console logs:

```
ObjectiveOutline [ObjectName]: Current=0, My=0
ObjectiveOutline [ObjectName]: shouldBeActive=true, isActive=false
ObjectiveOutline [ObjectName]: Enabling with FloatingIndicator
ObjectiveOutline: ENABLED for ObjectName
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
│   ├── ObjectiveOutline (method = FloatingIndicator)
│   └── Collider (trigger)
│
└── Objective2 (GameObject)
    ├── ObjectiveTrigger (myObjectiveIndex = 1)
    ├── ObjectiveOutline (method = FloatingIndicator)
    └── Collider (trigger)
```

## Tips

1. **Use FloatingIndicator** for the most reliable visibility
2. **Increase indicatorSize** if the marker is too small
3. **Use bright colors** like yellow, cyan, or green for better visibility
4. **Add a ParticleSystem** for extra flair (assign to Glow Particles field)