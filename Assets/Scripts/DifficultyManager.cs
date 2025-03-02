using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace RoofTops
{
    /// <summary>
    /// AI-driven difficulty manager that controls game difficulty based on distance traveled.
    /// Implements a chunk-based approach where difficulty parameters change at specific distance milestones.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Difficulty Chunks")]
        [Tooltip("Size of each difficulty chunk in meters")]
        [SerializeField] private float chunkSize = 200f;

        [Header("Base Difficulty Parameters")]
        [SerializeField] private float baseGameSpeed = 6f;
        [SerializeField] private float baseGapSize = 2.5f;
        [SerializeField] private float baseHeightVariation = 1.0f;
        [SerializeField] private float baseTridotFrequency = 1.25f;
        [SerializeField] private float baseJumpPadFrequency = 1.0f;
        [SerializeField] private float basePropFrequency = 1.0f;

        [Header("Frequency Clamp Values")]
        [Tooltip("Minimum tridot frequency")]
        public float minTridotFrequency = 0.05f;
        [Tooltip("Maximum tridot frequency")]
        public float maxTridotFrequency = 0.95f;
        
        [Tooltip("Minimum jump pad frequency")]
        public float minJumpPadFrequency = 0.005f;
        [Tooltip("Maximum jump pad frequency")]
        public float maxJumpPadFrequency = 0.5f;
        
        [Tooltip("Minimum prop frequency")]
        public float minPropFrequency = 0.001f;
        [Tooltip("Maximum prop frequency")]
        public float maxPropFrequency = 0.5f;

        [Header("Progression Curves")]
        [Tooltip("How game speed increases with distance")]
        [SerializeField] private AnimationCurve speedProgressionCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.8f);

        [Tooltip("How gap size increases with distance")]
        [SerializeField] private AnimationCurve gapProgressionCurve = AnimationCurve.EaseInOut(0, 1, 10, 3.0f);

        [Tooltip("How height variation increases with distance")]
        [SerializeField] private AnimationCurve heightVariationCurve = AnimationCurve.EaseInOut(0, 1, 10, 2.5f);

        [Tooltip("How tridots frequency changes with distance")]
        [SerializeField] private AnimationCurve tridotFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 0.7f);

        [Tooltip("How jump pad frequency changes with distance")]
        [SerializeField] private AnimationCurve jumpPadFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.5f);

        [Tooltip("How prop frequency changes with distance")]
        [SerializeField] private AnimationCurve propFrequencyCurve = AnimationCurve.EaseInOut(0, 1, 10, 1.2f);

        [Header("Jump Settings")]
        [Tooltip("Base jump force that determines maximum height differences")]
        [SerializeField] private float baseJumpForce = 15f;
        [Tooltip("Maximum number of jump pads allowed per chunk")]
        [Range(0, 2)]
        [SerializeField] private int maxJumpPadsPerChunk = 2;
        [Tooltip("Maximum height difference allowed when using jump pads")]
        [SerializeField] private float maxJumpPadHeight = 8f;
        [Tooltip("Safety margin for jump calculations (0-1, higher = more conservative)")]
        [SerializeField] private float jumpSafetyMargin = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Current state
        private float currentDistance = 0f;
        private int currentChunkIndex = 0;
        private Dictionary<int, ChunkDifficultyData> chunkData = new Dictionary<int, ChunkDifficultyData>();

        // Current parameters
        private float currentGameSpeed;
        private float currentGapSize;
        private float currentHeightVariation;
        private float currentTridotFrequency;
        private float currentJumpPadFrequency;
        private float currentPropFrequency;

        // References
        private ModulePool modulePool;
        private UnifiedSpawnManager spawnManager;
        private GameManager gameManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            modulePool = ModulePool.Instance;
            gameManager = GameManager.Instance;
            spawnManager = FindObjectOfType<UnifiedSpawnManager>();
            
            if (modulePool == null || gameManager == null)
            {
                Debug.LogError("DifficultyManager: Required components not found!");
                enabled = false;
                return;
            }

            InitializeBaseParameters();
            gameManager.onGameStarted.AddListener(OnGameStarted);
        }

        private void InitializeBaseParameters()
        {
            currentGameSpeed = baseGameSpeed;
            currentGapSize = baseGapSize;
            currentHeightVariation = baseHeightVariation;
            currentTridotFrequency = baseTridotFrequency;
            currentJumpPadFrequency = baseJumpPadFrequency;
            currentPropFrequency = basePropFrequency;
            ApplyCurrentParameters();
        }

        private void OnGameStarted()
        {
            currentDistance = 0f;
            currentChunkIndex = 0;
            InitializeBaseParameters();
        }

        public void UpdateDistance(float distance)
        {
            currentDistance = distance;
            int newChunkIndex = Mathf.FloorToInt(currentDistance / chunkSize);
            
            if (newChunkIndex != currentChunkIndex)
            {
                currentChunkIndex = newChunkIndex;
                UpdateChunkDifficulty();
            }
        }

        private void UpdateChunkDifficulty()
        {
            ChunkDifficultyData chunk = GenerateChunkDifficulty(currentChunkIndex);
            ApplyChunkDifficulty(chunk);
        }

        private float CalculateMaxHeightDifference()
        {
            // Using physics formula: max height = v0^2 / (2*g)
            // where v0 is initial velocity (jump force) and g is gravity
            float gravity = Physics.gravity.magnitude;
            float maxJumpHeight = (baseJumpForce * baseJumpForce) / (2f * gravity);
            
            // Apply safety margin to ensure jumps are comfortable
            return maxJumpHeight * (1f - jumpSafetyMargin);
        }

        private ChunkDifficultyData GenerateChunkDifficulty(int chunkIndex)
        {
            float normalizedProgress = chunkIndex * 0.1f;
            float maxSafeHeight = CalculateMaxHeightDifference();

            ChunkDifficultyData newChunk = new ChunkDifficultyData
            {
                chunkIndex = chunkIndex,
                gameSpeed = Mathf.Max(currentGameSpeed, baseGameSpeed * speedProgressionCurve.Evaluate(normalizedProgress)),
                gapSize = baseGapSize * gapProgressionCurve.Evaluate(normalizedProgress),
                // Limit height variation to what's safely jumpable
                heightVariation = Mathf.Min(baseHeightVariation * heightVariationCurve.Evaluate(normalizedProgress), maxSafeHeight),
                tridotFrequency = Mathf.Clamp(baseTridotFrequency * tridotFrequencyCurve.Evaluate(normalizedProgress), minTridotFrequency, maxTridotFrequency),
                // Set jump pad frequency to 0 - we'll place them strategically instead
                jumpPadFrequency = 0f,
                propFrequency = Mathf.Clamp(basePropFrequency * propFrequencyCurve.Evaluate(normalizedProgress), minPropFrequency, maxPropFrequency)
            };

            return newChunk;
        }

        private void ApplyChunkDifficulty(ChunkDifficultyData chunk)
        {
            currentGameSpeed = Mathf.Max(currentGameSpeed, chunk.gameSpeed);
            currentGapSize = chunk.gapSize;
            currentHeightVariation = chunk.heightVariation;
            currentTridotFrequency = chunk.tridotFrequency;
            currentJumpPadFrequency = chunk.jumpPadFrequency;
            currentPropFrequency = chunk.propFrequency;

            ApplyCurrentParameters();
        }

        private void ApplyCurrentParameters()
        {
            if (modulePool != null)
            {
                modulePool.currentMoveSpeed = currentGameSpeed;
                modulePool.maxGapSize = currentGapSize;
                modulePool.maxHeightVariation = currentHeightVariation;
            }

            if (spawnManager != null)
            {
                spawnManager.UpdateSpawnFrequencies(currentTridotFrequency, currentJumpPadFrequency, currentPropFrequency);
            }
        }

        [System.Serializable]
        public class ChunkDifficultyData
        {
            public int chunkIndex;
            public float gameSpeed;
            public float gapSize;
            public float heightVariation;
            public float tridotFrequency;
            public float jumpPadFrequency;
            public float propFrequency;
        }
    }

    // Extension method for UnifiedSpawnManager
    public static class SpawnManagerExtensions
    {
        public static void UpdateSpawnFrequencies(this UnifiedSpawnManager spawnManager, float tridotFrequency, float jumpPadFrequency, float propFrequency)
        {
            // Access the private fields via reflection (only if necessary)
            var tridotField = spawnManager.GetType().GetField("tridotFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var jumpPadField = spawnManager.GetType().GetField("jumpPadFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var propField = spawnManager.GetType().GetField("propFrequency", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            if (tridotField != null) tridotField.SetValue(spawnManager, tridotFrequency);
            if (jumpPadField != null) jumpPadField.SetValue(spawnManager, jumpPadFrequency);
            if (propField != null) propField.SetValue(spawnManager, propFrequency);
        }
    }
}
