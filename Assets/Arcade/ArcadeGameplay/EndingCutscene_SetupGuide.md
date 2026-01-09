# Ending Cutscene Trigger - Setup Guide

This guide explains how to set up the `EndingCutsceneTrigger` script to create a smooth transition from FPS gameplay to the snake scene with camera look-at, FOV animation, and fade to white effect.

## Overview

When the player enters the trigger zone, the following sequence occurs:
1. **Player control is disabled** - Movement and camera look are frozen
2. **Camera looks at target** - FPS camera rotates to look at a specified transform
3. **FOV animation** - FPS camera field of view animates (zoom effect)
4. **Fade to white** - Screen fades to white immediately after FOV animation
5. **Scene load** - The snake scene is loaded

---

## Step-by-Step Setup

### Step 1: Create the Trigger Zone

1. In your scene, create an empty GameObject where you want the ending to trigger
2. Name it something like `EndingCutsceneTrigger`
3. Add a **Collider** component (Box Collider recommended):
   - Check **"Is Trigger"** ✓
   - Adjust the size to cover the area where the player should trigger the ending
4. Add the **EndingCutsceneTrigger** script to this GameObject

### Step 2: Create the Look At Target

1. Create an empty GameObject in your scene
2. Name it `LookAtTarget`
3. Position it where you want the player's camera to look during the ending
   - This could be an arcade machine, a portal, a sign, etc.
4. The camera will rotate to face this point when the cutscene triggers

### Step 3: Create the Fade UI

1. Create a **Canvas** in your scene (if you don't have one):
   - Right-click in Hierarchy → UI → Canvas
   - Set **Render Mode** to "Screen Space - Overlay"
   - Set **Sort Order** to a high number (e.g., 100) so it renders on top
2. Create an **Image** as a child of the Canvas:
   - Right-click on Canvas → UI → Image
   - Name it `FadeImage`
   - Set **Anchor** to stretch in all directions (hold Alt and click the bottom-right anchor preset)
   - Set **Left, Right, Top, Bottom** all to 0
   - Set **Color** to white (or your desired fade color)
   - **Disable the Image GameObject** initially

### Step 4: Configure the Script

Select your `EndingCutsceneTrigger` GameObject and configure these fields in the Inspector:

#### Player References
| Field | Description |
|-------|-------------|
| **FPS Controller** | Drag your player/FPS controller GameObject here |
| **FPS Camera** | Drag the player's FPS camera here |

#### Look At Target
| Field | Description |
|-------|-------------|
| **Look At Target** | Drag the transform you want the camera to look at |
| **Look At Speed** | How fast the camera rotates (degrees/second). 0 = instant |
| **Smooth Look At** | If true, smoothly rotates. If false, snaps instantly |

#### FOV Animation
| Field | Description |
|-------|-------------|
| **Animate FOV** | Enable/disable FOV animation |
| **Start FOV** | The FOV value at the start (usually 60) |
| **End FOV** | The FOV value at the end (30 = zoom in, 90 = zoom out) |
| **FOV Animation Duration** | How long the FOV animation takes (in seconds) |
| **FOV Curve** | Animation curve for smooth easing |

#### Fade Settings
| Field | Description |
|-------|-------------|
| **Fade Image** | Drag your FadeImage UI element here |
| **Fade Duration** | How long the fade takes (e.g., 1.5 seconds) |
| **Fade Color** | Color to fade to (default: white) |
| **Post Fade Delay** | Wait time after fade before loading scene |

#### Scene Settings
| Field | Description |
|-------|-------------|
| **Snake Scene Name** | The exact name of your snake scene (must be in Build Settings!) |

#### Audio (Optional)
| Field | Description |
|-------|-------------|
| **Ending Audio Source** | AudioSource for ending music/sound |
| **Ending Audio Clip** | The audio clip to play |

---

## How the Look At Works

When triggered, the FPS camera will rotate to look at the target transform:

### Smooth Look At (Recommended)
- Set **Smooth Look At** = true
- Set **Look At Speed** to control rotation speed (e.g., 90 = 90 degrees per second)
- The camera smoothly rotates toward the target during the FOV animation

### Instant Look At
- Set **Smooth Look At** = false
- Or set **Look At Speed** = 0
- The camera instantly snaps to look at the target

### How It Handles FPS Controller
The script properly handles the FPS controller's split rotation:
- **Player body** rotates horizontally (Y axis)
- **Camera** rotates vertically (X axis / pitch)

---

## How FOV Animation Works

FOV animation creates a zoom effect:

### Understanding FOV Values
- **Lower FOV (30)** = Zoomed IN (telephoto effect)
- **Higher FOV (90)** = Zoomed OUT (wide-angle effect)
- **Normal FOV** = Usually 60-70

### Common Effects

#### Dramatic Zoom In
- Start FOV: 60
- End FOV: 30
- Duration: 2-3 seconds
- Creates a "tunnel vision" focus effect

#### Subtle Zoom
- Start FOV: 60
- End FOV: 45
- Duration: 3-4 seconds
- Gentle, cinematic feel

### Using the Animation Curve

The **FOV Curve** controls the easing:
1. Click on the **FOV Curve** field
2. X-axis = time (0 to 1)
3. Y-axis = interpolation (0 = Start FOV, 1 = End FOV)

Preset options:
- **Linear**: Constant speed
- **Ease In**: Starts slow, speeds up
- **Ease Out**: Starts fast, slows down
- **Ease In-Out**: Smooth start and end (default)

---

## Important Notes

### Scene Must Be in Build Settings!
1. Go to **File → Build Settings**
2. Click **Add Open Scenes** or drag your snake scene into the list
3. Note the exact scene name (case-sensitive!)

### Player Tag
The script checks for the "Player" tag as a fallback. Make sure your FPS controller has the **"Player"** tag assigned.

### Testing
You can test the cutscene by:
1. Selecting the trigger GameObject
2. In the Inspector, right-click the script header
3. Choose "TriggerEndingCutscene" from the context menu

---

## Hierarchy Example

```
Scene
├── FPSController (with Player tag)
│   └── Camera (FPS Camera)
├── LookAtTarget (empty GameObject positioned where camera should look)
├── Canvas (Sort Order: 100)
│   └── FadeImage (disabled, white, stretched)
└── EndingCutsceneTrigger
    └── BoxCollider (Is Trigger: true)
```

---

## Troubleshooting

### Cutscene doesn't trigger
- Check that the collider has **"Is Trigger"** enabled
- Verify the FPS Controller reference is assigned OR the player has the "Player" tag
- Check the Console for any error messages

### Camera doesn't look at target
- Make sure **Look At Target** is assigned
- Check that the target is positioned correctly in the scene
- Try increasing **Look At Speed** if it's too slow

### FOV doesn't animate
- Make sure **Animate FOV** is checked
- Verify the FPS Camera is assigned
- Check that Start FOV and End FOV have different values

### Fade doesn't appear
- Ensure the Canvas has a high Sort Order
- Check that the FadeImage is stretched to fill the screen
- Verify the Image component is assigned in the script

### Scene doesn't load
- Verify the scene name is spelled correctly (case-sensitive)
- Make sure the scene is added to Build Settings
- Check the Console for loading errors

---

## Example Settings

### Cinematic Ending
- Look At Speed: 60
- Smooth Look At: true
- Start FOV: 60
- End FOV: 35
- FOV Animation Duration: 3 seconds
- Fade Duration: 2 seconds
- FOV Curve: Ease In-Out

### Quick Dramatic Ending
- Look At Speed: 180
- Smooth Look At: true
- Start FOV: 60
- End FOV: 25
- FOV Animation Duration: 1.5 seconds
- Fade Duration: 1 second
- FOV Curve: Ease In