# Core Scene Setup Fixes

This document explains the fixes made to prevent NullReferenceExceptions in the Core scene setup.

## Issues Fixed

1. **NullReferenceException in PlayerController.PredictJumpTrajectory**
   - Added null checks for `modulePool` to prevent exceptions
   - Added fallback to use `GameManager.initialGameSpeed` if `modulePool` is null
   - Added default fallback speed if both are null

2. **NullReferenceException in PlayerAnimatorController.HandleJumpTrigger**
   - Added null checks for `playerController` and its `PredictJumpTrajectory` method
   - Added try-catch block to handle potential exceptions
   - Added default values for jump parameters if prediction fails

3. **Player Visibility Issues in Playing State**
   - Updated `GameManager.StartGame` to find the player if not assigned
   - Added multiple methods to find the player (SceneReferenceManager, tag, component)
   - Added delayed player finding in `GameManager.Awake`

4. **SceneReferenceManager Integration**
   - Improved player registration with SceneReferenceManager in CoreSceneSetup
   - Ensured GameManager gets player reference from SceneReferenceManager
   - Added proper error handling for missing references

## How These Fixes Work

### Robust Null Checking

All critical methods now check for null references before accessing objects. This prevents crashes when objects aren't initialized in the expected order.

### Delayed Initialization

Some components now use coroutines to delay initialization, giving other components time to initialize first.

### Multiple Fallback Options

When a primary reference is missing, the code now tries multiple fallback options:
1. First try SceneReferenceManager
2. Then try finding by tag
3. Finally try finding by component type
4. Use default values if all else fails

### Clear Separation of Component Instantiation

The CoreSceneSetup script now properly separates:
- Manager instantiation
- Scene loading
- Player spawning
- Gameplay systems initialization

This ensures components are created in the correct order.

## Testing Your Setup

1. Make sure you've set up the Core scene as described in README_CORE_SCENE_SETUP.md
2. Ensure all prefabs are assigned in the CoreSceneSetup component
3. Remove the player from your Main scene
4. Run the game from the Core scene

If everything is set up correctly, you should no longer see NullReferenceExceptions related to the player, ModulePool, or VistaPool. 