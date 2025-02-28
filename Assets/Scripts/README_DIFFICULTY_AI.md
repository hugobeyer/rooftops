# AI-Driven Difficulty Manager for RoofTops

This system implements an intelligent, distance-based difficulty progression that automatically balances your game based on player performance.

## Features

- **Distance-Based Chunks**: Difficulty progresses in 200m chunks (configurable)
- **AI-Driven Balancing**: Automatically adjusts difficulty based on player performance
- **Centralized Control**: All difficulty parameters managed in one place
- **Easy to Configure**: Simple inspector settings with tooltips
- **Performance Tracking**: Records player metrics to inform AI decisions
- **Always Fast-Paced**: Game speed NEVER decreases, ensuring constant excitement

## Setup Instructions

1. Create an empty GameObject in your scene and name it "DifficultyManager"
2. Add the `DifficultyManager.cs` script to this GameObject
3. Configure the basic settings in the Inspector:
   - **Chunk Size**: How often difficulty changes (200m recommended)
   - **Base Parameters**: Starting values for game speed, gap size, etc.
   - **Progression Curves**: How each parameter scales with distance
   - **AI Learning Settings**: How quickly the AI adapts to player performance

## How It Works

The system divides the game into distance-based "chunks" (default 200m each). For each chunk:

1. The AI calculates appropriate difficulty parameters based on:
   - Current distance (using the progression curves)
   - Player performance in previous chunks
   - A small amount of randomization for variety

2. When a player dies repeatedly in a chunk, the AI automatically eases the difficulty by:
   - Reducing gap sizes between modules
   - Decreasing height variations
   - Adjusting spawn frequencies
   - **NEVER reducing game speed** - maintaining excitement at all times

3. When a player performs well (few deaths, fast progression), the AI gradually increases all difficulty parameters.

## Smart Speed Management

The DifficultyManager is designed to maintain high energy gameplay:
- Game speed only increases, never decreases
- When players struggle, other parameters are adjusted instead
- Each new chunk will maintain or increase current speed, never slow down

This ensures your game always feels fast and exciting, even when other difficulty elements are being adjusted.

## Customizing Difficulty Curves

The inspector contains AnimationCurves for each difficulty parameter:
- **X-axis**: Represents progression (0 = start, 10 = very far)
- **Y-axis**: Multiplier for the base value (1 = no change, 2 = twice as difficult)

Adjust these curves to create your ideal difficulty progression.

## Advanced Usage

- **Adaptation Rate**: Controls how quickly AI adapts (higher = faster adjustments)
- **Max Deaths Before Easing**: How many deaths trigger difficulty reduction
- **Show Debug Info**: Enable to see difficulty changes in console

## Spawn Frequencies

The system uses extremely conservative spawn rates by default:
- **Props**: Very rare (0.1% chance per spawn point)
- **Jump Pads**: Rare (0.5% chance per spawn point)
- **Tridotes**: Moderate (25% chance per spawn point)

These values ensure the gameplay remains clean and uncluttered, with special elements appearing at key moments rather than overwhelming the player.

## Integration Notes

The DifficultyManager automatically interfaces with:
- ModulePool.cs (for game speed and module height/gap parameters)
- UnifiedSpawnManager.cs (for spawn frequencies)

No manual connections required between these components. 