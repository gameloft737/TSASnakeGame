# Performance Optimizations Applied

This document summarizes the performance optimizations made to the TSA Snake Game.

## Key Optimization: Static Enemy List

The most impactful optimization was replacing expensive `FindObjectsByType<AppleEnemy>()` calls with a static list maintained by the `AppleEnemy` class itself.

### How It Works

`AppleEnemy.cs` maintains a static list that automatically tracks all active enemies:

```csharp
// In AppleEnemy.cs
private static List<AppleEnemy> s_allAppleEnemies = new List<AppleEnemy>();

// Automatically adds/removes enemies on enable/disable
private void OnEnable() => s_allAppleEnemies.Add(this);
private void OnDisable() => s_allAppleEnemies.Remove(this);

// Public accessors for other scripts
public static List<AppleEnemy> GetAllActiveEnemies() => s_allAppleEnemies;
public static int GetActiveEnemyCount() => s_allAppleEnemies.Count;
```

### Files Optimized

The following files were updated to use `AppleEnemy.GetAllActiveEnemies()` instead of `FindObjectsByType<AppleEnemy>()`:

1. **HomingProjectileAttack.cs** - 3 instances
2. **ElectricFieldAbility.cs** - 1 instance
3. **FrostAuraAbility.cs** - 1 instance
4. **OrbitingAbility.cs** - 1 instance
5. **ForceFieldAbility.cs** - 1 instance
6. **ThornsAbility.cs** - 1 instance
7. **TurretAbility.cs** - 1 instance
8. **LightningStrikeAbility.cs** - 1 instance
9. **AbilityCollector.cs** - 1 instance
10. **AttackSelectionUI.cs** - 1 instance
11. **ClassicModeManager.cs** - 2 instances
12. **TutorialPanelManager.cs** - 2 instances
13. **SnakeScenePauseManager.cs** - 2 instances
14. **SnakePauseMenu.cs** - 2 instances

**Total: 17 instances replaced**

### Performance Impact

- `FindObjectsByType<T>()` is O(n) where n = all objects in scene
- Static list access is O(1)
- This optimization is especially impactful when called frequently (every frame or every ability tick)

## Existing Optimizations Already in Place

### EnemySpawner.cs
- ✅ Cached `WaitForSeconds` to avoid GC allocations
- ✅ Pre-allocated list capacities (64 for enemies, 16 for dead cache)
- ✅ Reusable `deadEnemiesCache` list for cleanup
- ✅ Manual iteration to avoid delegate allocations
- ✅ Object pooling integration

### SnakeBody.cs
- ✅ Adaptive constraint iterations (1-3 based on movement speed)
- ✅ Cached `cachedHeadRigidbody` reference
- ✅ Optimized loops with cached count variables
- ✅ Only backward pass on first iteration

### ObjectPool.cs
- ✅ Generic object pooling system
- ✅ Pre-warming support
- ✅ Automatic pool expansion

## Best Practices for Future Development

### DO:
1. **Use static lists for frequently-accessed collections** - Like `AppleEnemy.GetAllActiveEnemies()`
2. **Cache WaitForSeconds** - Create once, reuse in coroutines
3. **Cache component references** - Use `GetComponent` in Start/Awake, not Update
4. **Use object pooling** - For frequently spawned/destroyed objects
5. **Pre-allocate list capacities** - `new List<T>(expectedSize)`

### DON'T:
1. **Avoid FindObjectsByType in Update/FixedUpdate** - Use cached references or static lists
2. **Avoid creating new objects in hot paths** - Reuse lists, arrays, WaitForSeconds
3. **Avoid LINQ in performance-critical code** - Use manual loops
4. **Avoid delegate allocations** - Use manual iteration instead of `RemoveAll(x => x == null)`

## Profiling Tips

1. Use Unity Profiler (Window > Analysis > Profiler)
2. Look for GC.Alloc spikes - indicates memory allocations
3. Check CPU time in scripts - look for expensive methods
4. Monitor frame time consistency - spikes indicate performance issues