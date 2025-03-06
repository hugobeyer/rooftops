using UnityEngine;
using UnityEngine.UI;

namespace RoofTops.UI
{
    /// <summary>
    /// Simple linker that connects 3D button clicks to UI Button events
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Button3DLinker : MonoBehaviour
    {
        [Tooltip("The UI Button that will be triggered when this 3D object is clicked")]
        public Button uiButton;
        
        private void OnMouseDown()
        {
            if (uiButton != null)
            {
                uiButton.onClick.Invoke();
                Debug.Log($"3D button '{gameObject.name}' clicked, invoking UI button '{uiButton.name}'");
            }
        }
    }
} 