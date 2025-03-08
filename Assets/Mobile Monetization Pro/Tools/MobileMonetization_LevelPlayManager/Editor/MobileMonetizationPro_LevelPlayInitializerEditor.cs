using UnityEditor;
using UnityEngine;


namespace MobileMonetizationPro
{
    [CustomEditor(typeof(MobileMonetizationPro_LevelPlayInitializer))]
    public class MobileMonetizationPro_LevelPlayInitializerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MobileMonetizationPro_LevelPlayInitializer adsInitializer = (MobileMonetizationPro_LevelPlayInitializer)target;

            // Display all fields in the AdsInitializer script
            DrawDefaultInspector();

            if (GUILayout.Button("Open Ad Network Dashboard"))
            {
                Application.OpenURL("https://platform.ironsrc.com/partners/dashboard");
            }
            // Add a button to open the Ads Mediation Integration Manager
            if (GUILayout.Button("Open Ad Network Integration Manager"))
            {
                OpenIntegrationManagerWindow();
            }
            if (GUILayout.Button("Configure Ad Network Mediation Settings"))
            {
                OpenLevelPlayMediationSettingsWindow();
            }
            if (GUILayout.Button("Configure Ad Network Mediated Network Settings"))
            {
                OpenMediatedNetworkSettingsWindow();
            }
        }

        private void OpenIntegrationManagerWindow()
        {
            // Use Unity's ExecuteMenuItem to open the Ads Mediation Integration Manager
            EditorApplication.ExecuteMenuItem("Ads Mediation/Network Manager");
        }
        private void OpenLevelPlayMediationSettingsWindow()
        {

            EditorApplication.ExecuteMenuItem("Ads Mediation/Developer Settings/LevelPlay Mediation Settings");
        }
        private void OpenMediatedNetworkSettingsWindow()
        {

            EditorApplication.ExecuteMenuItem("Ads Mediation/Developer Settings/Mediated Network Settings");
        }
    }
}
