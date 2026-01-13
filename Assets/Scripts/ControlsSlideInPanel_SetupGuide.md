# Controls Slide-In Panel Setup Guide

This guide explains how to set up the **ControlsSlideInPanel** system that displays control hints (WASD to move, Mouse to look, E to interact) sliding in from the top-right corner after the arcade cutscene ends.

---

## Overview

The `ControlsSlideInPanel` script creates a smooth slide-in animation for a controls panel that:
- Automatically appears when the intro cutscene ends
- Slides in from the top-right corner
- Auto-hides after a configurable duration
- Can be toggled with the H key

---

## Quick Setup (5 Minutes)

### Step 1: Create the UI Panel

1. In your **Arcade scene**, find or create a **Canvas**
2. Right-click Canvas → **UI → Panel**
3. Rename it to `ControlsSlideInPanel`

### Step 2: Configure Panel Anchoring

1. Select the panel
2. In the **RectTransform**, click the anchor preset button (square icon)
3. Hold **Alt + Shift** and click **top-right** anchor
4. Set these values:
   - **Pivot**: X = 1, Y = 1
   - **Width**: 250
   - **Height**: 150
   - **Pos X**: -20 (slight margin from edge)
   - **Pos Y**: -20 (slight margin from top)

### Step 3: Add Control Text Elements

1. Right-click the panel → **UI → Text - TextMeshPro**
2. Create these text elements:

**Title (optional):**
- Text: "Controls"
- Font Size: 24
- Alignment: Center
- Position at top of panel

**Control 1:**
- Text: "WASD to move"
- Font Size: 18
- Alignment: Left

**Control 2:**
- Text: "Mouse to look"
- Font Size: 18
- Alignment: Left

**Control 3:**
- Text: "E to interact"
- Font Size: 18
- Alignment: Left

### Step 4: Add the Script

1. Select the `ControlsSlideInPanel` GameObject
2. Click **Add Component**
3. Search for and add `ControlsSlideInPanel`

### Step 5: Configure the Script

The script will auto-configure most settings, but verify:

| Setting | Recommended Value | Description |
|---------|-------------------|-------------|
| Panel Rect Transform | (auto-assigned) | The panel to animate |
| Slide In Duration | 0.5 | Animation duration in seconds |
| Slide In Delay | 0.5 | Delay after cutscene ends |
| Off Screen Offset | 400 | How far off-screen to start |
| Auto Hide | ✓ | Enable auto-hide |
| Auto Hide Delay | 8 | Seconds before hiding |
| Auto Find Cutscene Controller | ✓ | Automatically find CutsceneController |

---

## Hierarchy Example

```
Canvas
└── ControlsSlideInPanel (Panel with ControlsSlideInPanel script)
    ├── Background (Image - semi-transparent black)
    ├── TitleText (TextMeshProUGUI) - "Controls"
    ├── Control1Text (TextMeshProUGUI) - "WASD to move"
    ├── Control2Text (TextMeshProUGUI) - "Mouse to look"
    └── Control3Text (TextMeshProUGUI) - "E to interact"
```

---

## Integration with CutsceneController

The panel automatically integrates with `CutsceneController` (located in Assets/Arcade/ArcadeGameplay/):

1. **Automatic Detection**: If `Auto Find Cutscene Controller` is enabled, the script will find the CutsceneController in the scene
2. **Event Subscription**: It subscribes to the `OnCutsceneEnded` event
3. **Trigger**: When the arcade intro cutscene ends and the objective text appears, `SlideIn()` is called automatically

### Manual Integration (Alternative)

If you need manual control:

```csharp
// In your game manager or cutscene script
public class MyGameManager : MonoBehaviour
{
    void OnCutsceneComplete()
    {
        // Trigger the controls panel
        if (ControlsSlideInPanel.Instance != null)
        {
            ControlsSlideInPanel.Instance.SlideIn();
        }
    }
}
```

---

## Customization Options

### Animation Curves

The script uses `AnimationCurve` for smooth animations:
- **Slide In Curve**: Controls the easing of the slide-in animation
- **Slide Out Curve**: Controls the easing of the slide-out animation

Default is `EaseInOut` for smooth acceleration and deceleration.

### Toggle Key

Players can toggle the panel after it first appears:
- Default key: **H**
- Can be changed in the Inspector under "Toggle Settings"
- Set `Allow Toggle` to false to disable this feature

### Styling Suggestions

For a polished look:

1. **Background**: Use a semi-transparent dark color (e.g., RGBA: 0, 0, 0, 200)
2. **Border**: Add an `Outline` component for a subtle border
3. **Text Color**: White or light gray for readability
4. **Font**: Use a clean, readable font

---

## API Reference

### Public Methods

| Method | Description |
|--------|-------------|
| `SlideIn()` | Animate the panel sliding in from the right |
| `SlideOut()` | Animate the panel sliding out to the right |
| `TogglePanel()` | Toggle between visible and hidden states |
| `ShowImmediate()` | Instantly show the panel (no animation) |
| `HideImmediate()` | Instantly hide the panel (no animation) |

### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `Instance` | static | Singleton instance for easy access |
| `IsVisible` | bool | Whether the panel is currently visible |
| `HasAppeared` | bool | Whether the panel has appeared at least once |

---

## Troubleshooting

### Panel doesn't appear after cutscene

1. Verify `CutsceneController` exists in the scene (Assets/Arcade/ArcadeGameplay/)
2. Check that `Auto Find Cutscene Controller` is enabled
3. Ensure the panel is not disabled in the hierarchy
4. Check the Console for error messages

### Panel appears in wrong position

1. Verify anchor is set to **top-right**
2. Verify pivot is set to **(1, 1)**
3. Check `Off Screen Offset` value (should be positive, e.g., 400)
4. Check `Visible Position X` (usually 0 or a small negative value)

### Animation looks jerky

1. Ensure `Slide In Duration` is at least 0.3 seconds
2. Check that the animation curves are smooth (not linear)
3. Verify Time.timeScale is 1

### Panel doesn't auto-hide

1. Check that `Auto Hide` is enabled
2. Verify `Auto Hide Delay` is greater than 0
3. Ensure no other script is calling `SlideIn()` repeatedly

---

## Testing in Editor

The script includes a custom editor with testing tools:

1. Enter **Play Mode**
2. Select the ControlsSlideInPanel in the hierarchy
3. In the Inspector, find the "Testing Tools" section
4. Use the buttons to test:
   - **Slide In** / **Slide Out**
   - **Show Immediate** / **Hide Immediate**

---

## Complete Example Scene Setup

1. **Canvas** (Screen Space - Overlay)
   - Render Mode: Screen Space - Overlay
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

2. **ControlsSlideInPanel** (child of Canvas)
   - Anchor: Top-Right
   - Pivot: (1, 1)
   - Size: 250 x 150
   - Position: (-20, -20)
   - ControlsSlideInPanel script attached

3. **CutsceneController** (in the Arcade scene)
   - Located at Assets/Arcade/ArcadeGameplay/CutsceneController.cs
   - The ControlsSlideInPanel will auto-detect this and subscribe to OnCutsceneEnded

---

## Version History

- **v1.0**: Initial implementation with slide animation, auto-hide, and cutscene integration