using DG.Tweening;
using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum DOTweenTweenType 
{
    None,
    Move,
    LocalMove,
    Rotate,
    LocalRotate,
    Scale
}

public enum EasingCurveType 
{
    Linear,
    Quad,
    Cubic,
    Quart,
    Quint,
    Sin,
    Expo,
    Circ,
    Back,
    Elastic,
    Bounce
}

public enum EasingDirection 
{
    In,
    Out,
    InOut
}

[Serializable]
public class TweenOption 
{
    [Tooltip("Type of DOTween tween to perform")]
    public DOTweenTweenType tweenType = DOTweenTweenType.Move;
    
    [Tooltip("Target value for the tween (e.g., target position, rotation, or scale)")]
    public Vector3 targetValue = Vector3.zero;
    
    [Tooltip("Initial value for the tween (if empty, current transform values will be used)")]
    public Vector3 initialValue = Vector3.zero;
    
    [Tooltip("Whether to use the specified initial value instead of the current transform value")]
    public bool useInitialValue = false;
    
    [Tooltip("Whether to use the current editor position as the initial value (Editor only)")]
    public bool useEditorPositionAsInitial = false;
    
    [Tooltip("Duration (in seconds) for the tween")]
    public float duration = 1.0f;
    
    [Tooltip("Easing Curve Type (e.g., Linear, Quad, Cubic, etc.)")]
    public EasingCurveType easingCurveType = EasingCurveType.Quad;
    
    [Tooltip("Easing Direction (In, Out, InOut)")]
    public EasingDirection easingDirection = EasingDirection.InOut;
    
    [Tooltip("If true, this tween will play automatically on Start")]
    public bool playOnStart = true;
    
    [Tooltip("Delay (in seconds) before this tween starts")]
    public float delay = 0f;
    
    [Tooltip("Number of loops for the tween (-1 for infinite, 0 for no loop)")]
    public int loops = 0;
    
    [Tooltip("Loop type for the tween (Restart, Yoyo, Incremental)")]
    public LoopType loopType = LoopType.Restart;
    
    [Tooltip("If true, this tween will be added relative to the current property value (does not overwrite)")]
    public bool isAdditive = false;
    
    [Tooltip("If true, debug visualization will be shown for this tween")]
    public bool showDebugVisuals = false;
}

public class DOTweenConfigurator : MonoBehaviour 
{
    [Header("Tween Options")]
    [Tooltip("List of DOTween tweens to apply to this GameObject")]
    public List<TweenOption> tweens = new List<TweenOption>();

    [Tooltip("If true, all tween options will be played sequentially in a single sequence (allowing additive stacking)")]
    public bool playInSequence = false;
    
    [Header("Debug Visualization")]
    [Tooltip("Enable debug visualization for movement paths")]
    public bool enableDebugVisualization = false;
    [Tooltip("Number of points to visualize along the path")]
    public int debugPathResolution = 20;
    [Tooltip("Duration to show debug visualization (in seconds)")]
    public float debugVisualizationDuration = 3f;
    [Tooltip("Color for the debug path")]
    public Color debugPathColor = Color.yellow;
    [Tooltip("Enable visualization in editor (without playing)")]
    public bool showInEditor = true;
    
    private List<Vector3[]> debugPaths = new List<Vector3[]>();
    
    #if UNITY_EDITOR
    // Store the editor position for each tween option
    private Dictionary<int, Vector3> editorPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> editorLocalPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> editorRotations = new Dictionary<int, Quaternion>();
    private Dictionary<int, Quaternion> editorLocalRotations = new Dictionary<int, Quaternion>();
    private Dictionary<int, Vector3> editorScales = new Dictionary<int, Vector3>();
    
    private void OnValidate()
    {
        // Store current transform values for each tween option
        for (int i = 0; i < tweens.Count; i++)
        {
            var option = tweens[i];
            
            if (option.useEditorPositionAsInitial)
            {
                switch (option.tweenType)
                {
                    case DOTweenTweenType.Move:
                        editorPositions[i] = transform.position;
                        break;
                    case DOTweenTweenType.LocalMove:
                        editorLocalPositions[i] = transform.localPosition;
                        break;
                    case DOTweenTweenType.Rotate:
                        editorRotations[i] = transform.rotation;
                        break;
                    case DOTweenTweenType.LocalRotate:
                        editorLocalRotations[i] = transform.localRotation;
                        break;
                    case DOTweenTweenType.Scale:
                        editorScales[i] = transform.localScale;
                        break;
                }
            }
        }
    }
    #endif

    /// <summary>
    /// Automatically executes tweens marked with Play On Start.
    /// </summary>
    void Start() 
    {
        foreach (var option in tweens)
        {
            if (option.playOnStart)
            {
                PlayTween(option);
            }
        }
    }

    /// <summary>
    /// Plays all tweens configured on this component.
    /// </summary>
    public void PlayTweens() 
    {
        if (playInSequence)
        {
            // Create a sequence so that tweens play one after the other.
            Sequence seq = DOTween.Sequence();
            foreach (var option in tweens)
            {
                // Append the delay (if any) then the tween.
                if (option.delay > 0)
                    seq.AppendInterval(option.delay);
                Tween t = CreateTween(option);
                if (t != null)
                    seq.Append(t);
            }
        }
        else
        {
            foreach (var option in tweens)
            {
                PlayTween(option);
            }
        }
    }

    /// <summary>
    /// Plays a specific tween based on its configuration.
    /// </summary>
    /// <param name="option">Tween configuration option.</param>
    void PlayTween(TweenOption option) 
    {
        Tween t = CreateTween(option);
        if (t != null)
            t.Play();
        
        // Draw debug visualization if enabled
        if (enableDebugVisualization && option.showDebugVisuals && 
            (option.tweenType == DOTweenTweenType.Move || option.tweenType == DOTweenTweenType.LocalMove))
        {
            DrawMovementPath(option);
        }
    }

    // Creates and returns a tween based on a unity transform for a given TweenOption.
    private Tween CreateTween(TweenOption option)
    {
        Tween tween = null;
        
        #if UNITY_EDITOR
        // Apply editor position if needed and we're in play mode
        if (!Application.isPlaying && option.useEditorPositionAsInitial)
        {
            // This is just for visualization in editor, actual values will be set at runtime
            return null;
        }
        #endif
        
        switch (option.tweenType)
        {
            case DOTweenTweenType.Move:
            {
                Vector3 initialPos = transform.position;
                
                if (option.useInitialValue)
                {
                    // Make initial position relative to current position
                    transform.position = transform.position + option.initialValue;
                }
                #if UNITY_EDITOR
                else if (Application.isPlaying && option.useEditorPositionAsInitial)
                {
                    // Find the index of this option in the tweens list
                    int index = tweens.IndexOf(option);
                    if (index >= 0 && editorPositions.ContainsKey(index))
                    {
                        transform.position = editorPositions[index];
                    }
                }
                #endif
                
                if (option.isAdditive)
                    tween = transform.DOBlendableMoveBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                else
                    tween = transform.DOMove(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                break;
            }
            case DOTweenTweenType.LocalMove:
            {
                if (option.useInitialValue)
                {
                    // Make initial position relative to current local position
                    transform.localPosition = transform.localPosition + option.initialValue;
                }
                #if UNITY_EDITOR
                else if (Application.isPlaying && option.useEditorPositionAsInitial)
                {
                    // Find the index of this option in the tweens list
                    int index = tweens.IndexOf(option);
                    if (index >= 0 && editorLocalPositions.ContainsKey(index))
                    {
                        transform.localPosition = editorLocalPositions[index];
                    }
                }
                #endif
                
                if (option.isAdditive)
                    tween = transform.DOBlendableLocalMoveBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                else
                    tween = transform.DOLocalMove(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                break;
            }
            case DOTweenTweenType.Rotate:
            {
                if (option.useInitialValue)
                {
                    // Make initial rotation relative to current rotation
                    transform.rotation = transform.rotation * Quaternion.Euler(option.initialValue);
                }
                #if UNITY_EDITOR
                else if (Application.isPlaying && option.useEditorPositionAsInitial)
                {
                    // Find the index of this option in the tweens list
                    int index = tweens.IndexOf(option);
                    if (index >= 0 && editorRotations.ContainsKey(index))
                    {
                        transform.rotation = editorRotations[index];
                    }
                }
                #endif
                
                if (option.isAdditive)
                {
                    // Use DOBlendableRotateBy for additive rotation
                    tween = transform.DOBlendableRotateBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                }
                else
                {
                    // For non-additive, use the target rotation directly
                    tween = transform.DORotate(option.targetValue, option.duration, RotateMode.FastBeyond360)
                        .SetEase(GetEase(option));
                }
                break;
            }
            case DOTweenTweenType.LocalRotate:
            {
                if (option.useInitialValue)
                {
                    // Make initial rotation relative to current local rotation
                    transform.localRotation = transform.localRotation * Quaternion.Euler(option.initialValue);
                }
                #if UNITY_EDITOR
                else if (Application.isPlaying && option.useEditorPositionAsInitial)
                {
                    // Find the index of this option in the tweens list
                    int index = tweens.IndexOf(option);
                    if (index >= 0 && editorLocalRotations.ContainsKey(index))
                    {
                        transform.localRotation = editorLocalRotations[index];
                    }
                }
                #endif
                
                if (option.isAdditive)
                {
                    // Use DOBlendableLocalRotateBy for additive local rotation
                    tween = transform.DOBlendableLocalRotateBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                }
                else
                {
                    // For non-additive, use the target rotation directly
                    tween = transform.DOLocalRotate(option.targetValue, option.duration, RotateMode.FastBeyond360)
                        .SetEase(GetEase(option));
                }
                break;
            }
            case DOTweenTweenType.Scale:
            {
                if (option.useInitialValue)
                {
                    // Make initial scale relative to current scale
                    transform.localScale = Vector3.Scale(transform.localScale, option.initialValue);
                }
                #if UNITY_EDITOR
                else if (Application.isPlaying && option.useEditorPositionAsInitial)
                {
                    // Find the index of this option in the tweens list
                    int index = tweens.IndexOf(option);
                    if (index >= 0 && editorScales.ContainsKey(index))
                    {
                        transform.localScale = editorScales[index];
                    }
                }
                #endif
                
                if (option.isAdditive)
                    tween = transform.DOBlendableScaleBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                else
                    tween = transform.DOScale(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                break;
            }
            default:
                break;
        }

        if (tween != null)
        {
            int loopsToSet = option.loops;
            if (loopsToSet == 0 && option.loopType == LoopType.Yoyo)
                loopsToSet = -1;
            if (loopsToSet != 0)
                tween.SetLoops(loopsToSet, option.loopType);
            tween.SetDelay(option.delay);
        }

        return tween;
    }

    /// <summary>
    /// Returns a DOTween Ease based on the selected easing curve type and direction.
    /// </summary>
    private Ease GetEase(TweenOption option)
    {
        switch (option.easingCurveType)
        {
            case EasingCurveType.Linear:
                return Ease.Linear;
            case EasingCurveType.Quad:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InQuad;
                    case EasingDirection.Out: return Ease.OutQuad;
                    case EasingDirection.InOut: return Ease.InOutQuad;
                }
                break;
            case EasingCurveType.Cubic:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InCubic;
                    case EasingDirection.Out: return Ease.OutCubic;
                    case EasingDirection.InOut: return Ease.InOutCubic;
                }
                break;
            case EasingCurveType.Quart:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InQuart;
                    case EasingDirection.Out: return Ease.OutQuart;
                    case EasingDirection.InOut: return Ease.InOutQuart;
                }
                break;
            case EasingCurveType.Quint:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InQuint;
                    case EasingDirection.Out: return Ease.OutQuint;
                    case EasingDirection.InOut: return Ease.InOutQuint;
                }
                break;
            case EasingCurveType.Sin:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InSine;
                    case EasingDirection.Out: return Ease.OutSine;
                    case EasingDirection.InOut: return Ease.InOutSine;
                }
                break;
            case EasingCurveType.Expo:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InExpo;
                    case EasingDirection.Out: return Ease.OutExpo;
                    case EasingDirection.InOut: return Ease.InOutExpo;
                }
                break;
            case EasingCurveType.Circ:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InCirc;
                    case EasingDirection.Out: return Ease.OutCirc;
                    case EasingDirection.InOut: return Ease.InOutCirc;
                }
                break;
            case EasingCurveType.Back:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InBack;
                    case EasingDirection.Out: return Ease.OutBack;
                    case EasingDirection.InOut: return Ease.InOutBack;
                }
                break;
            case EasingCurveType.Elastic:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InElastic;
                    case EasingDirection.Out: return Ease.OutElastic;
                    case EasingDirection.InOut: return Ease.InOutElastic;
                }
                break;
            case EasingCurveType.Bounce:
                switch(option.easingDirection)
                {
                    case EasingDirection.In: return Ease.InBounce;
                    case EasingDirection.Out: return Ease.OutBounce;
                    case EasingDirection.InOut: return Ease.InOutBounce;
                }
                break;
        }
        return Ease.Linear;
    }
    
    /// <summary>
    /// Draws a debug visualization of the movement path for a tween
    /// </summary>
    private void DrawMovementPath(TweenOption option)
    {
        Vector3[] pathPoints = CalculatePathPoints(option);
        
        // Store the path for drawing in OnDrawGizmos
        debugPaths.Add(pathPoints);
        
        // Schedule removal of the debug path after the specified duration
        StartCoroutine(RemoveDebugPathAfterDelay(pathPoints, debugVisualizationDuration));
    }
    
    /// <summary>
    /// Calculates points along the path for a given tween option
    /// </summary>
    private Vector3[] CalculatePathPoints(TweenOption option)
    {
        Vector3 startPos;
        Vector3 endPos;
        
        #if UNITY_EDITOR
        int index = tweens.IndexOf(option);
        #endif
        
        if (option.tweenType == DOTweenTweenType.Move)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && option.useEditorPositionAsInitial && index >= 0 && editorPositions.ContainsKey(index))
            {
                startPos = editorPositions[index];
            }
            else
            #endif
            {
                startPos = option.useInitialValue ? transform.position + option.initialValue : transform.position;
            }
            
            endPos = option.isAdditive ? startPos + option.targetValue : option.targetValue;
        }
        else if (option.tweenType == DOTweenTweenType.LocalMove)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && option.useEditorPositionAsInitial && index >= 0 && editorLocalPositions.ContainsKey(index))
            {
                startPos = editorLocalPositions[index];
            }
            else
            #endif
            {
                startPos = option.useInitialValue ? transform.localPosition + option.initialValue : transform.localPosition;
            }
            
            endPos = option.isAdditive ? startPos + option.targetValue : option.targetValue;
            
            // Convert local positions to world positions for visualization
            if (transform.parent != null)
            {
                startPos = transform.parent.TransformPoint(startPos);
                endPos = transform.parent.TransformPoint(endPos);
            }
        }
        else
        {
            // For non-movement tweens, return empty array
            return new Vector3[0];
        }
        
        // Create an array to store path points
        Vector3[] pathPoints = new Vector3[debugPathResolution];
        
        // Calculate points along the path based on the easing function
        for (int i = 0; i < debugPathResolution; i++)
        {
            float t = (float)i / (debugPathResolution - 1);
            float easedT = DOVirtual.EasedValue(0, 1, t, GetEase(option));
            pathPoints[i] = Vector3.Lerp(startPos, endPos, easedT);
        }
        
        return pathPoints;
    }
    
    private System.Collections.IEnumerator RemoveDebugPathAfterDelay(Vector3[] pathPoints, float delay)
    {
        yield return new WaitForSeconds(delay);
        debugPaths.Remove(pathPoints);
    }
    
    private void OnDrawGizmos()
    {
        if (!enableDebugVisualization)
            return;
            
        // Draw runtime paths
        if (debugPaths != null && debugPaths.Count > 0)
        {
            DrawPaths(debugPaths, debugPathColor);
        }
        
        // Draw editor paths if enabled
        #if UNITY_EDITOR
        if (showInEditor && !Application.isPlaying)
        {
            DrawEditorPaths();
        }
        #endif
    }
    
    private void DrawPaths(List<Vector3[]> paths, Color color)
    {
        Gizmos.color = color;
        
        // Draw all stored debug paths
        foreach (Vector3[] path in paths)
        {
            if (path.Length == 0)
                continue;
                
            // Draw lines connecting the points
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
            
            // Draw spheres at each point
            for (int i = 0; i < path.Length; i++)
            {
                Gizmos.DrawSphere(path[i], 0.05f);
            }
            
            // Draw a larger sphere at the start and end points
            Gizmos.DrawSphere(path[0], 0.1f);
            Gizmos.DrawSphere(path[path.Length - 1], 0.1f);
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Draws paths for all movement tweens in the editor
    /// </summary>
    private void DrawEditorPaths()
    {
        List<Vector3[]> editorPaths = new List<Vector3[]>();
        
        // Update stored positions if needed
        OnValidate();
        
        foreach (var option in tweens)
        {
            if (option.showDebugVisuals && 
                (option.tweenType == DOTweenTweenType.Move || option.tweenType == DOTweenTweenType.LocalMove))
            {
                Vector3[] pathPoints = CalculatePathPoints(option);
                if (pathPoints.Length > 0)
                {
                    editorPaths.Add(pathPoints);
                }
            }
        }
        
        // Use a slightly different color for editor paths
        Color editorColor = debugPathColor;
        editorColor.a = 0.7f;
        DrawPaths(editorPaths, editorColor);
        
        // Add labels for start and end points
        foreach (var path in editorPaths)
        {
            if (path.Length > 0)
            {
                Handles.Label(path[0], "Start");
                Handles.Label(path[path.Length - 1], "End");
            }
        }
    }
    #endif
} 