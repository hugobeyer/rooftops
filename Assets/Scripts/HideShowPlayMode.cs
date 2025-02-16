using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HideInPlayMode : MonoBehaviour
{
    [Tooltip("If true, hides in play mode. If false, shows in play mode")]
    public bool hideInPlayMode = true;

    void Start()
    {
        gameObject.SetActive(!hideInPlayMode);
    }

    void OnEnable()
    {
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        #endif
    }

    void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        #endif
    }

    #if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            gameObject.hideFlags = hideInPlayMode ? HideFlags.HideInHierarchy : HideFlags.None;
        else if (state == PlayModeStateChange.EnteredEditMode)
            gameObject.hideFlags = hideInPlayMode ? HideFlags.None : HideFlags.HideInHierarchy;
    }
    #endif
} 