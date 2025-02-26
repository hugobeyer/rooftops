# <span style="color:#4CAF50;">GOAL MESSAGING SYSTEM - QUICK REFERENCE</span>

## <span style="color:#2196F3;">COLLECTION IDs</span>
---------------------------------------

| <span style="color:#FF9800;">ID</span> | <span style="color:#FFFFFF;">Description</span> | <span style="color:#FFFFFF;">Values</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">DIST_GOAL_ARRAY</span> | Distance goals | [100, 250, 500, 1000, 2000, 5000] |
| <span style="color:#FF9800;">BONUS_GOAL_ARRAY</span> | Bonus items | [5, 10, 25, 50, 100] |
| <span style="color:#FF9800;">SURVIVAL_GOAL_ARRAY</span> | Survival time | [30, 60, 120, 300, 600] |
| <span style="color:#FF9800;">JUMP_GOAL_ARRAY</span> | Jump count | [10, 25, 50, 100, 200] |


## <span style="color:#2196F3;">MESSAGE IDs</span>
---------------------------------------

### <span style="color:#E91E63;">GAME STATUS MESSAGES</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#FFFFFF;">Message Template</span> | <span style="color:#FFFFFF;">Description</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">ZERO_TRIDOTS</span> | "Not enough Tridots!" | Displayed when player lacks sufficient Tridots |
| <span style="color:#FF9800;">LEVEL_UP</span> | "Level Up! You reached level {0}" | Shown when player levels up |
| <span style="color:#FF9800;">NEW_HIGHSCORE</span> | "New Highscore: {0}!" | Displayed when player achieves a new high score |
| <span style="color:#FF9800;">GAME_OVER</span> | "Game Over! Distance: {0}m" | Shown when the game ends |
| <span style="color:#FF9800;">BONUS_COLLECTED</span> | "+{0} Bonus!" | Displayed when collecting bonus items |
| <span style="color:#FF9800;">1ST_BONUS_DASH_INFO</span> | "Press JUMP in mid-air to DASH! Each dash uses 1 bonus." | Shown when player collects their first bonus |

### <span style="color:#E91E63;">ANNOUNCEMENTS</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|
| <span style="color:#FF9800;">RUN_START_GOAL_DISTANCE</span> | "Your goal: Run {0}m!" |
| <span style="color:#FF9800;">RUN_START_GOAL_BONUS</span> | "Your goal: Collect {0} bonus items!" |
| <span style="color:#FF9800;">RUN_START_GOAL_SURVIVAL</span> | "Your goal: Survive for {0} seconds!" |
| <span style="color:#FF9800;">RUN_START_GOAL_JUMP</span> | "Your goal: Perform {0} jumps!" |

### <span style="color:#E91E63;">ACHIEVEMENTS</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|
| <span style="color:#FF9800;">GOAL_ACHIEVED_DISTANCE</span> | "Goal achieved! You ran {0}m!" |
| <span style="color:#FF9800;">GOAL_ACHIEVED_BONUS</span> | "Goal achieved! You collected {0} bonus items!" |
| <span style="color:#FF9800;">GOAL_ACHIEVED_SURVIVAL</span> | "Goal achieved! You survived for {0} seconds!" |
| <span style="color:#FF9800;">GOAL_ACHIEVED_JUMP</span> | "Goal achieved! You performed {0} jumps!" |

### <span style="color:#E91E63;">PROGRESS</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|
| <span style="color:#FF9800;">GOAL_PROGRESS_DISTANCE</span> | "{0}m out of {1}m" |
| <span style="color:#FF9800;">GOAL_PROGRESS_BONUS</span> | "{0}/{1} bonus items" |
| <span style="color:#FF9800;">GOAL_PROGRESS_SURVIVAL</span> | "{0}s/{1}s survived" |
| <span style="color:#FF9800;">GOAL_PROGRESS_JUMP</span> | "{0}/{1} jumps" |


## <span style="color:#2196F3;">RANGE-BASED MESSAGES</span>
---------------------------------------

### <span style="color:#E91E63;">DISTANCE RANGES</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#9C27B0;">Range</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">DIST_RANGE_BEGINNER</span> | <span style="color:#9C27B0;">(0-300)</span> | "Beginner goal: Run {0}m!" |
| <span style="color:#FF9800;">DIST_RANGE_INTERMEDIATE</span> | <span style="color:#9C27B0;">(301-1000)</span> | "Intermediate goal: Run {0}m!" |
| <span style="color:#FF9800;">DIST_RANGE_ADVANCED</span> | <span style="color:#9C27B0;">(1001-3000)</span> | "Advanced goal: Run {0}m!" |
| <span style="color:#FF9800;">DIST_RANGE_EXPERT</span> | <span style="color:#9C27B0;">(3001+)</span> | "Expert goal: Run {0}m!" |

### <span style="color:#E91E63;">BONUS RANGES</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#9C27B0;">Range</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">BONUS_RANGE_EASY</span> | <span style="color:#9C27B0;">(1-10)</span> | "Easy goal: Collect {0} bonus items!" |
| <span style="color:#FF9800;">BONUS_RANGE_MEDIUM</span> | <span style="color:#9C27B0;">(11-50)</span> | "Medium goal: Collect {0} bonus items!" |
| <span style="color:#FF9800;">BONUS_RANGE_HARD</span> | <span style="color:#9C27B0;">(51+)</span> | "Hard goal: Collect {0} bonus items!" |

### <span style="color:#E91E63;">SURVIVAL RANGES</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#9C27B0;">Range</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">SURVIVAL_RANGE_SHORT</span> | <span style="color:#9C27B0;">(0-60)</span> | "Quick run: Survive {0} seconds!" |
| <span style="color:#FF9800;">SURVIVAL_RANGE_MEDIUM</span> | <span style="color:#9C27B0;">(61-300)</span> | "Extended run: Survive {0} seconds!" |
| <span style="color:#FF9800;">SURVIVAL_RANGE_LONG</span> | <span style="color:#9C27B0;">(301+)</span> | "Marathon: Survive {0} seconds!" |

### <span style="color:#E91E63;">JUMP RANGES</span>

| <span style="color:#FF9800;">ID</span> | <span style="color:#9C27B0;">Range</span> | <span style="color:#FFFFFF;">Message Template</span> |
|:---|:---|:---|
| <span style="color:#FF9800;">JUMP_RANGE_FEW</span> | <span style="color:#9C27B0;">(1-25)</span> | "Casual jumper: Perform {0} jumps!" |
| <span style="color:#FF9800;">JUMP_RANGE_MANY</span> | <span style="color:#9C27B0;">(26-100)</span> | "Active jumper: Perform {0} jumps!" |
| <span style="color:#FF9800;">JUMP_RANGE_EXTREME</span> | <span style="color:#9C27B0;">(101+)</span> | "Extreme jumper: Perform {0} jumps!" |


## <span style="color:#2196F3;">GOAL SELECTION STRATEGIES</span>
---------------------------------------

### <span style="color:#E91E63;">SELECTION METHODS</span>

| <span style="color:#FF9800;">Method</span> | <span style="color:#FFFFFF;">Description</span> |
|:---|:---|
| <span style="color:#FF9800;">BY PLAYER EXPERIENCE</span> | ```// New player: Distance goal (100m)```<br>```// Intermediate: Mix of Distance and Bonus```<br>```// Experienced: All goal types``` |
| <span style="color:#FF9800;">BY PERFORMANCE</span> | ```// If good at distance but not bonuses:```<br>```// Challenge with bonus goal``` |
| <span style="color:#FF9800;">WEIGHTED RANDOM</span> | ```// Assign weights to each goal type```<br>```// Adjust based on player history``` |
| <span style="color:#FF9800;">VARIETY ENFORCEMENT</span> | ```// Avoid repeating same goal type```<br>```// Prioritize unseen types``` |

### <span style="color:#E91E63;">VALUE DETERMINATION</span>

| <span style="color:#FF9800;">Method</span> | <span style="color:#FFFFFF;">Description</span> |
|:---|:---|
| <span style="color:#FF9800;">INCREMENTAL</span> | Start with base values, increase gradually |
| <span style="color:#FF9800;">PERCENTAGE</span> | Increase from player's best (e.g., 120%) |
| <span style="color:#FF9800;">DIFFICULTY CURVE</span> | Map player progress to predefined curve |


## <span style="color:#2196F3;">USAGE EXAMPLES</span>
---------------------------------------

```csharp
// Show goal message
ShowGoalMessage("DIST_GOAL_ARRAY", 450);

// Show achievement
ShowGoalMessage("GOAL_ACHIEVED_DISTANCE", 450);

// Show progress
string progress = string.Format(GetMessageTemplate("GOAL_PROGRESS_DISTANCE"), 300, 500);
GameMessageDisplay.Instance.ShowMessage(progress);

// Show game status message
GameMessageDisplay.Instance.ShowMessageByID("ZERO_TRIDOTS");
```

<!-- 
COLOR SCHEME:
- Main Titles: #4CAF50 (Green)
- Section Headers: #2196F3 (Blue)
- Categories: #E91E63 (Pink)
- IDs/Keys: #FF9800 (Orange)
- Ranges/Values: #9C27B0 (Purple)
- Text: #FFFFFF (White)
--> 