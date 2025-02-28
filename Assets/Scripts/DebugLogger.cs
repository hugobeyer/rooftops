using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RoofTops
{
    public class DebugLogger : MonoBehaviour
    {
        public static DebugLogger Instance { get; private set; }
        
        [SerializeField] private bool enableFileLogging = true;
        [SerializeField] private string logFileName = "debug_log.txt";
        [SerializeField] private bool appendToExistingLog = false;
        [SerializeField] private bool includeTimestamp = true;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private int maxLogFilesToKeep = 3;
        [SerializeField] private string logFilePrefix = "debug_log";
        
        private string logFilePath;
        private StringBuilder logBuffer = new StringBuilder();
        private bool isInitialized = false;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogger();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeLogger()
        {
            if (!enableFileLogging) return;
            
            try
            {
                // Create a timestamped filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string timestampedFileName = $"{logFilePrefix}_{timestamp}.txt";
                
                // Use a path in the project folder instead of persistentDataPath
                #if UNITY_EDITOR
                // In the Editor, save to the project's root folder
                string logDirectory = Path.Combine(Application.dataPath, "..");
                #else
                // In a build, still use persistentDataPath since we can't write to the app folder
                string logDirectory = Application.persistentDataPath;
                #endif
                
                // Make sure the directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // Set the full path for the new log file
                logFilePath = Path.Combine(logDirectory, timestampedFileName);
                
                // Make sure the path is normalized
                logFilePath = Path.GetFullPath(logFilePath);
                
                // Log the path so you know where to find it
                Debug.Log($"Debug log will be saved to: {logFilePath}");
                
                // Rotate log files - keep only the most recent ones
                RotateLogFiles(logDirectory);
                
                // Create initial log entry
                string header = $"=== DEBUG LOG STARTED: {DateTime.Now} ===\n" +
                               $"Game Version: {Application.version}\n" +
                               $"Platform: {Application.platform}\n" +
                               $"Device: {SystemInfo.deviceModel}\n" +
                               $"OS: {SystemInfo.operatingSystem}\n" +
                               $"Log Path: {logFilePath}\n" +
                               $"=================================\n\n";
                
                File.AppendAllText(logFilePath, header);
                
                isInitialized = true;
                Log("Debug logger initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize debug logger: {e.Message}");
                enableFileLogging = false;
            }
        }
        
        private void RotateLogFiles(string directory)
        {
            try
            {
                // Get all log files in the directory
                string[] logFiles = Directory.GetFiles(directory, $"{logFilePrefix}_*.txt");
                
                // If we have more files than we want to keep
                if (logFiles.Length >= maxLogFilesToKeep)
                {
                    // Sort by creation time (oldest first)
                    Array.Sort(logFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
                    
                    // Delete the oldest files, keeping only (maxLogFilesToKeep - 1) to make room for the new one
                    int filesToDelete = logFiles.Length - (maxLogFilesToKeep - 1);
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        Log($"Deleting old log file: {logFiles[i]}");
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error rotating log files: {e.Message}");
            }
        }
        
        public void Log(string message, LogCategory logType = LogCategory.Log)
        {
            switch (logType)
            {
                case LogCategory.Log:
                    if (logToConsole) Debug.Log(message);
                    break;
                case LogCategory.Warning:
                    if (logToConsole) Debug.LogWarning(message);
                    break;
                case LogCategory.Error:
                    if (logToConsole) Debug.LogError(message);
                    break;
            }
            
            if (!enableFileLogging || !isInitialized) return;
            
            try
            {
                string formattedMessage = includeTimestamp 
                    ? $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n" 
                    : $"{message}\n";
                
                logBuffer.Append(formattedMessage);
                
                // Write to file periodically to avoid performance issues
                if (logBuffer.Length > 1024)
                {
                    FlushBuffer();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing to log file: {e.Message}");
            }
        }
        
        private void FlushBuffer()
        {
            if (logBuffer.Length == 0) return;
            
            try
            {
                File.AppendAllText(logFilePath, logBuffer.ToString());
                logBuffer.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error flushing log buffer: {e.Message}");
            }
        }
        
        private void OnApplicationQuit()
        {
            if (enableFileLogging && isInitialized)
            {
                Log("Application shutting down");
                FlushBuffer();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (enableFileLogging && isInitialized)
            {
                Log($"Application {(pauseStatus ? "paused" : "resumed")}");
                FlushBuffer();
            }
        }
        
        // Call this to get the path to the log file
        public string GetLogFilePath()
        {
            return logFilePath;
        }
        
        // Call this to manually flush the buffer
        public void ForceFlushBuffer()
        {
            FlushBuffer();
        }
        
        public void LogWarning(string message)
        {
            if (logToConsole)
            {
                Debug.LogWarning(message);
            }
            
            if (!enableFileLogging || !isInitialized) return;
            
            try
            {
                string formattedMessage = includeTimestamp 
                    ? $"[{DateTime.Now:HH:mm:ss.fff}] [WARNING] {message}\n" 
                    : $"[WARNING] {message}\n";
                
                logBuffer.Append(formattedMessage);
                
                // Write to file periodically to avoid performance issues
                if (logBuffer.Length > 1024)
                {
                    FlushBuffer();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing warning to log file: {e.Message}");
            }
        }
        
        public void LogError(string message)
        {
            if (logToConsole)
            {
                Debug.LogError(message);
            }
            
            if (!enableFileLogging || !isInitialized) return;
            
            try
            {
                string formattedMessage = includeTimestamp 
                    ? $"[{DateTime.Now:HH:mm:ss.fff}] [ERROR] {message}\n" 
                    : $"[ERROR] {message}\n";
                
                logBuffer.Append(formattedMessage);
                
                // Always flush immediately for errors to ensure they're captured
                FlushBuffer();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing error to log file: {e.Message}");
            }
        }
    }
} 