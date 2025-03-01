using DG.Tweening;
using UnityEngine;
using System;
using System.Collections.Generic;

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
}

public class DOTweenConfigurator : MonoBehaviour 
{
    [Header("Tween Options")]
    [Tooltip("List of DOTween tweens to apply to this GameObject")]
    public List<TweenOption> tweens = new List<TweenOption>();

    [Tooltip("If true, all tween options will be played sequentially in a single sequence (allowing additive stacking)")]
    public bool playInSequence = false;

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
    }

    // Creates and returns a tween based on a unity transform for a given TweenOption.
    private Tween CreateTween(TweenOption option)
    {
        Tween tween = null;
        switch (option.tweenType)
        {
            case DOTweenTweenType.Move:
            {
                if (option.useInitialValue)
                {
                    transform.position = option.initialValue;
                }
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
                    transform.localPosition = option.initialValue;
                }
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
                    transform.rotation = Quaternion.Euler(option.initialValue);
                }
                if (option.isAdditive)
                    tween = transform.DOBlendableRotateBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                else
                    tween = transform.DORotate(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                break;
            }
            case DOTweenTweenType.LocalRotate:
            {
                if (option.useInitialValue)
                {
                    transform.localRotation = Quaternion.Euler(option.initialValue);
                }
                if (option.isAdditive)
                    tween = transform.DOBlendableLocalRotateBy(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                else
                    tween = transform.DOLocalRotate(option.targetValue, option.duration)
                        .SetEase(GetEase(option));
                break;
            }
            case DOTweenTweenType.Scale:
            {
                if (option.useInitialValue)
                {
                    transform.localScale = option.initialValue;
                }
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
} 