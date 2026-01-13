# Checkpoint Manager Setup Guide

## Overview

The Checkpoint Manager provides a checkpoint/respawn system that saves the player's state at level milestones (every 10 ranks). When the player dies and clicks the restart button, instead of reloading the entire scene, the game restores the player to their last checkpoint with all their abilities, attack level, and stats preserved.

## How It Works

1. **Checkpoints are saved automatically** when `LevelUIManager.ShowLevelAnnouncement()` is called (every 10 ranks by default)
2. **On death**, clicking the restart button restores the player to their last checkpoint instead of reloading the scene
3. **Player state preserved** includes:
   - XP level and progress
   - Attack type and level
   - All abilities and their levels
   - Player stat bonuses (damage, health, speed, etc.)
   - Wave index

## Setup Instructions

### 1. Add CheckpointManager to Your Scene

1. Create an empty GameObject in your game scene
2. Name it "CheckpointManager"
3. Add the `CheckpointManager` component to it
4. (Optional) Enable "Debug Mode" to see checkpoint save/restore logs

### 2. Verify DeathScreenManager Settings

The `DeathScreenManager` has been updated with a new setting:

- **Use Checkpoint System** (enabled by default): When enabled, the restart button will restore from checkpoint instead of reloading the scene

### 3. Verify LevelUIManager Settings

The `LevelUIManager` automatically saves checkpoints when showing level announcements. Make sure:

- `ranksPerLevel` is set (default: 10) - this determines how often checkpoints are saved
- `autoShowLevelAnnouncements` is enabled

## Example Flow

1. Player starts game → Initial checkpoint saved at level 0
2. Player reaches rank 10 → "Level 1" announcement shows → Checkpoint saved
3. Player gains abilities and upgrades attack to level 5
4. Player reaches rank 20 → "Level 2" announcement shows → Checkpoint saved with current state
5. Player gains 2 more abilities, upgrades attack to level 7
6. Player dies at rank 25
7. Player clicks restart → Restored to rank 20 checkpoint with:
   - Attack at level 5 (not 7)
   - Only abilities from checkpoint (2 new abilities removed)
   - Stats from checkpoint

## What Gets Saved/Restored

| Data | Saved | Restored |
|------|-------|----------|
| XP Level (Rank) | ✅ | ✅ |
| XP Progress | ✅ | ✅ |
| Wave Index | ✅ | ✅ |
| Attack Level | ✅ | ✅ |
| Abilities | ✅ | ✅ (extras removed) |
| Ability Levels | ✅ | ✅ |
| Player Stats Bonuses | ✅ | ✅ |
| Health | N/A | Reset to full |
| Enemy Positions | N/A | Cleared |

## Fallback Behavior

If the checkpoint system fails or is disabled:
- The game falls back to reloading the current scene (original behavior)
- This ensures the game always has a working restart mechanism

## Troubleshooting

### Checkpoint not saving
- Ensure `CheckpointManager` exists in the scene
- Check that `LevelUIManager.ShowLevelAnnouncement()` is being called
- Enable debug mode on CheckpointManager to see logs

### Abilities not restoring correctly
- Abilities gained after the checkpoint are removed on restore
- Ability levels are reset to their checkpoint values
- Make sure ability prefab names are consistent

### Stats not restoring
- PlayerStats bonuses are saved via reflection
- Ensure PlayerStats singleton is properly initialized

## API Reference

### CheckpointManager

```csharp
// Save a checkpoint at a specific level milestone
CheckpointManager.Instance.SaveCheckpoint(int levelMilestone);

// Restore to the last saved checkpoint
bool success = CheckpointManager.Instance.RestoreCheckpoint();

// Check if a checkpoint exists
bool hasCheckpoint = CheckpointManager.Instance.HasCheckpoint();

// Get the level of the current checkpoint
int level = CheckpointManager.Instance.GetCheckpointLevel();
```

## Notes

- The initial checkpoint (level 0) is saved automatically when the game starts
- Checkpoints are not persisted between game sessions (they reset when the scene reloads)
- The checkpoint system can be disabled per-death by setting `useCheckpointSystem = false` on DeathScreenManager