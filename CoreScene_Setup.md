# Core Scene Setup Guide (Updated)

## Overview

The Core scene contains all persistent systems that should remain loaded throughout the game's lifecycle. This includes managers for game state, achievements, ads, economy, and the main camera.

## Components to Include

1. **GameManager**
   - Controls game state, player data, and core game mechanics
   - Already has DontDestroyOnLoad implemented
   - Handles scene transitions and game restarts

2. **NoiseMovement (Camera Controller)**
   - Manages camera movement and effects
   - Contains AudioListener which is critical for audio across scenes
   - Already has DontDestroyOnLoad implemented

3. **GameAdsManager**
   - Handles ad display and timing
   - Already has DontDestroyOnLoad implemented
   - Currently in your Ads scene, but should be moved to Core

4. **GoalAchievementManager**
   - Manages goals and achievements
   - Controls message timing and display
   - Already has DontDestroyOnLoad implemented

5. **GoalValuesManager**
   - Stores goal values for different categories
   - Already has DontDestroyOnLoad implemented

6. **EconomyManager**
   - Tracks player resources (distance, tridots, memcards)
   - Used by collectibles and achievement systems
   - Already has DontDestroyOnLoad implemented
   - Critical for maintaining player economy across scene transitions

7. **SceneReferenceManager (NEW)**
   - Manages references between objects in different scenes
   - Solves the problem of cross-scene references
   - Automatically created if not found

## Setup Instructions

### Option 1: Using the CoreSceneSetup Helper

1. **Create a new "Core" scene**:
   - Create a new empty scene named "Core"
   - Add a GameObject named "CoreSetup"
   - Add the CoreSceneSetup script to this GameObject

2. **Configure the CoreSceneSetup**:
   - Assign the Systems.prefab from Assets/Gameplay to the systemsPrefab field
   - Assign the Camera.prefab from Assets/Gameplay to the cameraPrefab field
   - Set the initialSceneToLoad to "Main" (or your main menu scene)

3. **Run the scene**:
   - The CoreSceneSetup will automatically:
     - Create a SceneReferenceManager if needed
     - Instantiate the Systems prefab if needed
     - Instantiate the Camera prefab if needed
     - Load your main menu scene additively

### Option 2: Manual Setup

1. **Create a new "Core" scene**:
   - Create a new empty scene named "Core"
   - Add a single GameObject named "PersistentSystems"

2. **Add the core components**:
   - Option 1: Instantiate the Systems.prefab from Assets/Gameplay
   - Option 2: Create new GameObjects for each system with their respective components
   - Add the SceneReferenceManager script to a GameObject

3. **Camera Setup**:
   - Add a GameObject with the NoiseMovement script
   - Ensure it has a Camera component
   - Make sure it has an AudioListener component

4. **Update Build Settings**:
   - Make Core scene the first scene to load in the build settings
   - Followed by your Main Menu scene

## Handling Cross-Scene References

The SceneReferenceManager solves the problem of references between objects in different scenes:

1. **Registering Objects**:
   - The PlayerController now automatically registers itself with SceneReferenceManager
   - UI elements can be registered using GameManager.RegisterUIElement()
   - Other objects can be registered using SceneReferenceManager.RegisterGameObject()

2. **Retrieving Objects**:
   - NoiseMovement now uses SceneReferenceManager to find the player
   - GameManager can retrieve UI elements using GetUIElement()
   - Other scripts can get objects using SceneReferenceManager.GetGameObject()

3. **Example Usage**:
   ```csharp
   // Register an object
   SceneReferenceManager.Instance.RegisterGameObject("MyObject", myGameObject);
   
   // Get an object
   GameObject obj = SceneReferenceManager.Instance.GetGameObject("MyObject");
   ```

## Code Changes Made

The following changes have been made to support the Core scene:

1. **GameManager.RestartGame()**:
   - Updated to use additive scene loading
   - Preserves the Core scene when restarting
   - Properly resets the camera using NoiseMovement.Instance

2. **Scene Loading Logic**:
   - Now checks if the current scene is "Core" before unloading
   - Loads the Main scene additively instead of replacing the current scene

3. **Cross-Scene References**:
   - Added SceneReferenceManager to handle references between scenes
   - Updated NoiseMovement to find the player through SceneReferenceManager
   - Added methods to GameManager for registering and retrieving UI elements

4. **Error Handling**:
   - Added null checks for pauseIndicator in GameManager
   - Updated GameAdsManager to find ads manager without using tags
   - Fixed NoiseMovement to use initialFOV instead of defaultFOV

## Testing Your Setup

1. Place the Core scene first in your build settings
2. Run the game starting from the Core scene
3. Verify that game state persists when transitioning between scenes
4. Test the restart functionality to ensure it properly reloads the game without losing the Core systems

## Troubleshooting

- **Black Screen**: Make sure the NoiseMovement camera is properly set up in the Core scene
- **No Sound**: Verify that the AudioListener is enabled on the NoiseMovement camera
- **Multiple Instances**: Check for duplicate managers by adding debug logs to the Awake methods
- **Economy Issues**: If collectibles aren't working, ensure EconomyManager is properly initialized in the Core scene
- **Missing References**: If objects can't find each other, make sure they're registered with SceneReferenceManager
- **Tag Errors**: The code no longer relies on the "AdsManager" tag, so this error should be resolved 