using UnityEngine;
#if UNITY_IOS || UNITY_ANDROID
using UnityEngine.EventSystems;
#else
using UnityEngine.EventSystems;
#endif


namespace RoofTops.Input
{
    public class PlatformEventInputManager : MonoBehaviour
    {
        #region Properties 
        [SerializeField]
        private BaseInputModule _oldInputModule;

        [SerializeField]
        private BaseInputModule _newInputModule;

        #endregion // Properties

        #region Unity Methods
        
        private void Awake()
        {
            // For performance reasons and development time, only use old input system for Android/IOS
#if UNITY_ANDROID || UNITY_IOS
            _oldInputModule.enabled = true;
            _newInputModule.enabled = false;
#else
            _oldInputModule.enabled = false;
            _newInputModule.enabled = true;
#endif

        }

        #endregion // Unity Methods

    }
}
