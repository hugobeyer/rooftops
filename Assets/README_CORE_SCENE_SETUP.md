# Core Scene Setup Guide

This guide explains how to properly set up the Core scene for the RoofTops game to avoid initialization issues and ensure proper cross-scene references.

## Overview

The Core scene approach uses a persistent scene that contains all essential game systems and managers. This scene is loaded first, and then gameplay scenes are loaded additively. This solves cross-scene reference issues and ensures that core systems are always available.

## Step 1: Create Prefabs for All Managers

First, create prefabs for all your managers if you haven't already:

1. In your Main scene, locate the following managers:
   - GameManager
   - EconomyManager
   - GameAdsManager
   - GoalAchievementManager
   - GoalValuesManager
   - InputActionManager
   - Camera (with NoiseMovement)
   - ModulePool
   - VistaPool
   - Player

2. For each manager:
   - Select the GameObject in the Hierarchy
   - Drag it into the `Assets/Prefabs/Managers` folder
   - Ensure the prefab has all necessary components and references

## Step 2: Create the Core Scene

1. Create a new scene named "Core"
2. Add an empty GameObject named "CoreSetup"
3. Add the `CoreSceneSetup` script to this GameObject

## Step 3: Configure the CoreSceneSetup Component

In the Inspector for the CoreSetup GameObject:

1. Set "Initial Scene To Load" to "Main"
2. Set "Load Mode" to "Additive"
3. Assign all your manager prefabs to their respective fields:
   - Game Manager Prefab
   - Economy Manager Prefab
   - Game Ads Manager Prefab
   - Goal Achievement Manager Prefab
   - Goal Values Manager Prefab
   - Input Action Manager Prefab
   - Camera Prefab
   - Module Pool Prefab
   - Vista Pool Prefab
   - Player Prefab
4. Set "Player Spawn Position" if needed
5. Ensure "Spawn Player On Start" is checked
6. Ensure "Setup On Start" is checked

## Step 4: Remove Managers from the Main Scene

To prevent duplicate managers, remove these objects from your Main scene:
- GameManager
- EconomyManager
- GameAdsManager
- GoalAchievementManager
- GoalValuesManager
- InputActionManager
- Camera (with NoiseMovement)
- Player

**Note:** Keep ModulePool and VistaPool in the Main scene for now. The CoreSceneSetup will only instantiate them if they don't already exist.

## Step 5: Set Up Build Settings

1. Open Build Settings (File > Build Settings)
2. Add the Core scene as the first scene in the build order
3. Add the Main scene as the second scene

## Troubleshooting Initialization Issues

If you encounter NullReferenceExceptions related to managers not being found:

1. **Check Execution Order**: Go to Edit > Project Settings > Script Execution Order
   - Add GameManager and set it to execute early (-100)
   - Add SceneReferenceManager and set it to execute early (-90)
   - Add ModulePool and set it to execute after GameManager (-80)
   - Add VistaPool and set it to execute after ModulePool (-70)

2. **Check for Circular Dependencies**: Ensure your managers don't have circular dependencies where A needs B and B needs A.

3. **Use Delayed Initialization**: Some components have been updated to retry initialization after a delay if dependencies aren't found immediately.

## Testing the Setup

1. Open the Core scene
2. Enter Play mode
3. The Core scene should:
   - Create the SceneReferenceManager
   - Instantiate all managers from prefabs
   - Load the Main scene additively
   - Spawn the player
   - Instantiate ModulePool and VistaPool if needed

If everything is working correctly, you should see your game running normally with only one instance of each manager.

## Advanced: Registering Objects with SceneReferenceManager

To make objects accessible across scenes:

```csharp
// Register an object
SceneReferenceManager.Instance.RegisterGameObject("MyObject", gameObject);

// Retrieve an object
GameObject obj = SceneReferenceManager.Instance.GetGameObject("MyObject");
```

This is especially useful for UI elements and other objects that need to be accessed from different scenes. 