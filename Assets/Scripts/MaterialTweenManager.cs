using UnityEngine;
using System;
using DG.Tweening;
using System.Collections.Generic;

[Serializable]
public class MaterialTweenSetting
{
    public enum PropertyType
    {
        Float,
        Color,
        Vector,
        TextureOffset,
        TextureScale
    }

    public string propertyName;
    public PropertyType type;
    
    [Header("Animation Settings")]
    public float duration = 1f;
    public Ease easeType = Ease.Linear;
    public bool loop;
    public LoopType loopType = LoopType.Restart;
    public bool pingPong;
    
    [Header("Float Settings")]
    public float floatFrom;
    public float floatTo = 1f;
    
    [Header("Color Settings")]
    public Color colorFrom = Color.white;
    public Color colorTo = Color.white;
    
    [Header("Vector Settings")]
    public Vector4 vectorFrom;
    public Vector4 vectorTo = Vector4.one;
    
    [Header("Texture Settings")]
    public Vector2 offsetFrom;
    public Vector2 offsetTo = Vector2.one;
    public Vector2 scaleFrom = Vector2.one;
    public Vector2 scaleTo = Vector2.one;
}

public class MaterialTweenManager : MonoBehaviour
{
    public Material targetMaterial;
    public MaterialTweenSetting[] tweenSettings;
    private List<Tween> activeTweens = new List<Tween>();
    private Dictionary<string, List<Tween>> propertyTweens = new Dictionary<string, List<Tween>>();

    void OnEnable()
    {
        PlayAll();
    }

    void OnDisable()
    {
        StopAll();
    }

    [ContextMenu("Play All")]
    public void PlayAll()
    {
        if (targetMaterial == null || tweenSettings == null) return;

        StopAll();
        propertyTweens.Clear();

        foreach (var setting in tweenSettings)
        {
            if (string.IsNullOrEmpty(setting.propertyName)) continue;

            Tween tween = null;

            switch (setting.type)
            {
                case MaterialTweenSetting.PropertyType.Float:
                    tween = targetMaterial.DOFloat(setting.floatTo, setting.propertyName, setting.duration)
                        .From(setting.floatFrom);
                    break;

                case MaterialTweenSetting.PropertyType.Color:
                    tween = targetMaterial.DOColor(setting.colorTo, setting.propertyName, setting.duration)
                        .From(setting.colorFrom);
                    break;

                case MaterialTweenSetting.PropertyType.Vector:
                    tween = targetMaterial.DOVector(setting.vectorTo, setting.propertyName, setting.duration)
                        .From(setting.vectorFrom);
                    break;

                case MaterialTweenSetting.PropertyType.TextureOffset:
                    tween = targetMaterial.DOOffset(setting.offsetTo, setting.propertyName, setting.duration)
                        .From(setting.offsetFrom);
                    break;

                case MaterialTweenSetting.PropertyType.TextureScale:
                    tween = targetMaterial.DOTiling(setting.scaleTo, setting.propertyName, setting.duration)
                        .From(setting.scaleFrom);
                    break;
            }

            if (tween != null)
            {
                tween.SetEase(setting.easeType);
                
                if (setting.loop)
                {
                    if (setting.pingPong)
                    {
                        tween.SetLoops(-1, setting.loopType);
                    }
                    else
                    {
                        tween.SetLoops(-1, setting.loopType);
                    }
                }

                // Track the tween by property name
                if (!propertyTweens.ContainsKey(setting.propertyName))
                {
                    propertyTweens[setting.propertyName] = new List<Tween>();
                }
                propertyTweens[setting.propertyName].Add(tween);
                activeTweens.Add(tween);
            }
        }
    }

    [ContextMenu("Stop All")]
    public void StopAll()
    {
        foreach (var tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeTweens.Clear();
        propertyTweens.Clear();
    }

    public void PlayProperty(string propertyName)
    {
        if (targetMaterial == null) return;

        var setting = Array.Find(tweenSettings, s => s.propertyName == propertyName);
        if (setting != null)
        {
            // Stop existing tween for this property if any
            StopProperty(propertyName);

            Tween tween = null;

            switch (setting.type)
            {
                case MaterialTweenSetting.PropertyType.Float:
                    tween = targetMaterial.DOFloat(setting.floatTo, setting.propertyName, setting.duration)
                        .From(setting.floatFrom);
                    break;

                case MaterialTweenSetting.PropertyType.Color:
                    tween = targetMaterial.DOColor(setting.colorTo, setting.propertyName, setting.duration)
                        .From(setting.colorFrom);
                    break;

                case MaterialTweenSetting.PropertyType.Vector:
                    tween = targetMaterial.DOVector(setting.vectorTo, setting.propertyName, setting.duration)
                        .From(setting.vectorFrom);
                    break;

                case MaterialTweenSetting.PropertyType.TextureOffset:
                    tween = targetMaterial.DOOffset(setting.offsetTo, setting.propertyName, setting.duration)
                        .From(setting.offsetFrom);
                    break;

                case MaterialTweenSetting.PropertyType.TextureScale:
                    tween = targetMaterial.DOTiling(setting.scaleTo, setting.propertyName, setting.duration)
                        .From(setting.scaleFrom);
                    break;
            }

            if (tween != null)
            {
                tween.SetEase(setting.easeType);
                
                if (setting.loop)
                {
                    if (setting.pingPong)
                    {
                        tween.SetLoops(-1, LoopType.Yoyo);
                    }
                    else
                    {
                        tween.SetLoops(-1, setting.loopType);
                    }
                }

                // Track the tween by property name
                if (!propertyTweens.ContainsKey(setting.propertyName))
                {
                    propertyTweens[setting.propertyName] = new List<Tween>();
                }
                propertyTweens[setting.propertyName].Add(tween);
                activeTweens.Add(tween);
            }
        }
    }

    public void StopProperty(string propertyName)
    {
        if (propertyTweens.TryGetValue(propertyName, out var tweens))
        {
            foreach (var tween in tweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                    activeTweens.Remove(tween);
                }
            }
            propertyTweens.Remove(propertyName);
        }
    }
} 