using UnityEngine;
using UnityEditor;

namespace MobileMonetizationPro
{
    public class DeletePlayerPrefsWindow : EditorWindow
    {
        [MenuItem("Tools/Mobile Monetization Pro/Delete PlayerPrefs")]
        public static void ShowWindow()
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to delete PlayerPrefs?", "Yes", "No"))
            {
                PlayerPrefs.DeleteAll();
            }
        }
    }
}