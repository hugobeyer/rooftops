using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using static QuickMonetization.MobileMonetizationPro_UnityAdsManager;

namespace QuickMonetization
{
    [CustomEditor(typeof(MobileMonetizationPro_UnityAdsManager))]
    public class MobileMonetizationPro_UnityAdsManagerEditor : Editor
    {
        private SerializedProperty actionButtonsProperty;
        private ReorderableList actionButtonsList;
        int count;
        SerializedProperty ShowBannerAdButton;
        MobileMonetizationPro_UnityAdsInitializer adsInitializer;

        private void OnEnable()
        {
            adsInitializer = FindObjectOfType<MobileMonetizationPro_UnityAdsInitializer>();
            ShowBannerAdButton = serializedObject.FindProperty("ShowBannerAdButton");
            actionButtonsProperty = serializedObject.FindProperty("ActionButtonsToInvokeInterstitalAds");
            actionButtonsList = new ReorderableList(serializedObject, actionButtonsProperty, true, true, true, true);
            actionButtonsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Buttons To Show Interstitial Ads");
            actionButtonsList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
            actionButtonsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = actionButtonsProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };
        }
        private GUIStyle GetButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.green;
            style.fontSize = 14; // Adjust the font size as needed
            style.fontStyle = FontStyle.Bold;

            return style;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MobileMonetizationPro_UnityAdsManager script = (MobileMonetizationPro_UnityAdsManager)target;

            EditorGUILayout.Space();
            GUIStyle headerStylenew2 = new GUIStyle(EditorStyles.boldLabel);
            headerStylenew2.normal.textColor = Color.yellow;

            if (adsInitializer != null)
            {
                if (adsInitializer.ShowBannerAdsInStart == false)
                {
                    EditorGUILayout.LabelField("Banner Ads CallBack", headerStylenew2);
                    EditorGUILayout.PropertyField(ShowBannerAdButton);
                }
            }

            EditorGUILayout.LabelField("Interstital Ads CallBacks", headerStylenew2);
            actionButtonsList.DoLayoutList();

            GUIStyle headerStylenew = new GUIStyle(EditorStyles.boldLabel);
            headerStylenew.normal.textColor = Color.yellow;

            EditorGUILayout.LabelField("Rewarded Ads CallBacks", headerStylenew);

            for (int i = 0; i < script.functions.Count; i++)
            {
                FunctionInfo functionInfo = script.functions[i];

                EditorGUILayout.LabelField("Rewarded Button Property" + " " + (i + 1));

                functionInfo.RewardedButton = (Button)EditorGUILayout.ObjectField("Rewarded Button", functionInfo.RewardedButton, typeof(Button), true);
                functionInfo.script = (MonoBehaviour)EditorGUILayout.ObjectField("Add Script", functionInfo.script, typeof(MonoBehaviour), true);

                if (functionInfo.script != null)
                {
                    functionInfo.scriptName = functionInfo.script.GetType().Name;
                    functionInfo.functionNames = GetPublicVoidMethodNames(functionInfo.script);
                    if (functionInfo.functionNames.Count > 0)
                    {
                        functionInfo.selectedFunctionIndex = EditorGUILayout.Popup("Function To Invoke", functionInfo.selectedFunctionIndex, functionInfo.functionNames.ToArray());
                    }
                }

                count = i;

                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Add Function"))
            {
                script.functions.Add(new FunctionInfo());
            }

            if (GUILayout.Button("Remove Function"))
            {
                script.functions.RemoveAt(count);
                count--;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE THE CHANGES", GetButtonStyle(), GUILayout.Width(450), GUILayout.Height(40)))
            {
                // Check if the prefab exists
                string prefabPath = "Assets/Mobile Monetization Pro/Tools/MobileMonetization_UnityAdsManager/Prefab/MobileMonetizationPro_UnityAdsManager.prefab";
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                // Get information for PlayerPrefs key
                string sceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                string gameObjectName = script.gameObject.name;
                string prefabKey = $"{sceneName}_{gameObjectName}";

                if (existingPrefab != null && PlayerPrefs.HasKey(prefabKey))
                {
                    // If the prefab exists and the key exists, apply changes directly to the existing prefab
                    MobileMonetizationPro_UnityAdsManager existingPrefabScript = existingPrefab.GetComponent<MobileMonetizationPro_UnityAdsManager>();
                    if (existingPrefabScript != null)
                    {
                        EditorUtility.CopySerialized(script, existingPrefabScript);

                        // Mark the scene as dirty to ensure changes are saved
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                }
                else
                {
                    // If the prefab doesn't exist or the key doesn't exist, instantiate a new prefab from the original asset
                    GameObject prefabAsset = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (prefabAsset != null)
                    {
                        // Apply changes to the instantiated prefab
                        MobileMonetizationPro_UnityAdsManager newPrefabScript = prefabAsset.GetComponent<MobileMonetizationPro_UnityAdsManager>();
                        if (newPrefabScript != null)
                        {
                            EditorUtility.CopySerialized(script, newPrefabScript);

                            // Save the instantiated prefab as a new prefab asset
                            PrefabUtility.SaveAsPrefabAsset(prefabAsset, prefabPath);

                            // Save information about the new prefab in PlayerPrefs
                            PlayerPrefs.SetString(prefabKey, prefabPath);
                            PlayerPrefs.Save();

                            // Unload the prefab contents
                            PrefabUtility.UnloadPrefabContents(prefabAsset);
                        }
                    }
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private List<string> GetPublicVoidMethodNames(MonoBehaviour script)
        {
            List<string> methodNames = new List<string>();

            Type type = script.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                if (method.ReturnType == typeof(void))
                {
                    methodNames.Add(method.Name);
                }
            }

            return methodNames;
        }
    }
}









//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System;
//using System.Reflection;
//using UnityEditor;
//using UnityEditorInternal; // Import this namespace
//using static QuickMonetization.AdsManager;


//namespace QuickMonetization
//{
//    [CustomEditor(typeof(AdsManager))]
//    public class AdsManagerEditor : Editor
//    {
//        private SerializedProperty actionButtonsProperty; // SerializedProperty for ActionButtonsToDisplayInterstitalAds

//        private ReorderableList actionButtonsList; // Reorderable list for ActionButtonsToDisplayInterstitalAds

//        int count;

//        SerializedProperty ShowBannerAdButton;
//        AdsInitializer adsInitializer;

//        private void OnEnable()
//        {
//            adsInitializer = FindObjectOfType<AdsInitializer>();

//            ShowBannerAdButton = serializedObject.FindProperty("ShowBannerAdButton");
//            // Initialize the SerializedProperty
//            actionButtonsProperty = serializedObject.FindProperty("ActionButtonsToInvokeInterstitalAds");

//            // Initialize the ReorderableList
//            actionButtonsList = new ReorderableList(serializedObject, actionButtonsProperty, true, true, true, true);

//            // Set the list header name (optional)
//            actionButtonsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Buttons To Show Interstitial Ads");

//            // Set the list element height (optional)
//            actionButtonsList.elementHeight = EditorGUIUtility.singleLineHeight + 6;

//            // Draw the list element
//            actionButtonsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
//            {
//                var element = actionButtonsProperty.GetArrayElementAtIndex(index);
//                rect.y += 2;
//                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
//            };

//        }

//        public override void OnInspectorGUI()
//        {
//            serializedObject.Update();

//            AdsManager script = (AdsManager)target;

//            EditorGUILayout.Space();

//            GUIStyle headerStylenew2 = new GUIStyle(EditorStyles.boldLabel);
//            headerStylenew2.normal.textColor = Color.green; // Set the text color to black

//            if (adsInitializer != null)
//            {
//                if (adsInitializer.ShowBannerAdsInStart == false)
//                {
//                    EditorGUILayout.LabelField("Banner Ads CallBack", headerStylenew2);
//                    EditorGUILayout.PropertyField(ShowBannerAdButton);
//                }
//            }

//            EditorGUILayout.LabelField("Interstital Ads CallBacks", headerStylenew2);

//            // Draw the reorderable list for ActionButtonsToDisplayInterstitalAds
//            actionButtonsList.DoLayoutList();

//            // Create a custom GUIStyle for the header label
//            GUIStyle headerStylenew = new GUIStyle(EditorStyles.boldLabel);
//            headerStylenew.normal.textColor = Color.green; // Set the text color to black

//            // Header Label with custom GUIStyle
//            EditorGUILayout.LabelField("Rewarded Ads CallBacks", headerStylenew);

//            // Display functions and remove buttons
//            for (int i = 0; i < script.functions.Count; i++)
//            {
//                FunctionInfo functionInfo = script.functions[i];

//                EditorGUILayout.LabelField("Rewarded Button Property" + " " + (i + 1));


//                // Add the RewardedButton field inside FunctionInfo
//                functionInfo.RewardedButton = (Button)EditorGUILayout.ObjectField("Rewarded Button", functionInfo.RewardedButton, typeof(Button), true);

//                functionInfo.script = (MonoBehaviour)EditorGUILayout.ObjectField("Add Script", functionInfo.script, typeof(MonoBehaviour), true);

//                if (functionInfo.script != null)
//                {
//                    functionInfo.scriptName = functionInfo.script.GetType().Name;
//                    functionInfo.functionNames = GetPublicVoidMethodNames(functionInfo.script);
//                    if (functionInfo.functionNames.Count > 0)
//                    {
//                        functionInfo.selectedFunctionIndex = EditorGUILayout.Popup("Function To Invoke", functionInfo.selectedFunctionIndex, functionInfo.functionNames.ToArray());
//                    }
//                }

//                count = i;

//                EditorGUILayout.Space();
//            }

//            // Add Function button
//            if (GUILayout.Button("Apply Changes and Create New Prefab (If do not exist)"))
//            {
//                script.functions.Add(new FunctionInfo());
//            }

//            // Add Function button
//            if (GUILayout.Button("Add Function"))
//            {
//                script.functions.Add(new FunctionInfo());
//            }

//            if (GUILayout.Button("Remove Function"))
//            {
//                script.functions.RemoveAt(count);
//                count--;
//            }

//            serializedObject.ApplyModifiedProperties();
//        }

//        private List<string> GetPublicVoidMethodNames(MonoBehaviour script)
//        {
//            List<string> methodNames = new List<string>();

//            Type type = script.GetType();
//            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

//            foreach (MethodInfo method in methods)
//            {
//                if (method.ReturnType == typeof(void))
//                {
//                    methodNames.Add(method.Name);
//                }
//            }

//            return methodNames;
//        }
//    }
//}























//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System;
//using System.Reflection;
//using UnityEditor;
//using UnityEditorInternal; // Import this namespace
//using static QuickMonetization.AdsManager;


//namespace QuickMonetization
//{
//    [CustomEditor(typeof(AdsManager))]
//    public class AdsManagerEditor : Editor
//    {
//        private SerializedProperty actionButtonsProperty; // SerializedProperty for ActionButtonsToDisplayInterstitalAds

//        private ReorderableList actionButtonsList; // Reorderable list for ActionButtonsToDisplayInterstitalAds

//        int count;

//        SerializedProperty ShowBannerAdButton;

//        AdsInitializer adsInitializer;

//        private void OnEnable()
//        {
//            adsInitializer = FindObjectOfType<AdsInitializer>();

//            ShowBannerAdButton = serializedObject.FindProperty("ShowBannerAdButton");
//            // Initialize the SerializedProperty
//            actionButtonsProperty = serializedObject.FindProperty("ActionButtonsToInvokeInterstitalAds");

//            // Initialize the ReorderableList
//            actionButtonsList = new ReorderableList(serializedObject, actionButtonsProperty, true, true, true, true);

//            // Set the list header name (optional)
//            actionButtonsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Buttons To Show Interstitial Ads");

//            // Set the list element height (optional)
//            actionButtonsList.elementHeight = EditorGUIUtility.singleLineHeight + 6;

//            // Draw the list element
//            actionButtonsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
//            {
//                var element = actionButtonsProperty.GetArrayElementAtIndex(index);
//                rect.y += 2;
//                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
//            };

//            RetrieveSavedFunctionIndices();

//            AdsManager script = (AdsManager)target;
//            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//            string key = $"{sceneName}_{target.name}";
//            int newvalue = PlayerPrefs.GetInt("FunctionsExist" + key, 0);
//            if (newvalue == 0)
//            {
//                script.functions.Clear();
//            }

//        }
//        private void RetrieveSavedFunctionIndices()
//        {
//            AdsManager script = (AdsManager)target;

//            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//            string key = $"{sceneName}_{target.name}";
//            // Get the total count of saved functions
//            int savedFunctionCount = PlayerPrefs.GetInt("FunctionsExist" + key, script.functions.Count);

//            //script.functions = new List<FunctionInfo>(savedFunctionCount);

//            //Debug.Log(savedFunctionCount + "Saved");
//            //Debug.Log(script.functions.Count + "script.functions.Count");
//            // Make sure the script.functions list is at least as long as the saved count
//            //while (script.functions.Count < savedFunctionCount)
//            //{
//            //    script.functions.Add(new FunctionInfo());
//            //}

//            // Load the saved indices for existing functions
//            for (int i = 0; i < script.functions.Count; i++)
//            {
//                int savedIndex = PlayerPrefs.GetInt(key + i, 0);    

//                if (i < script.functions.Count)
//                {
//                    script.functions[i].selectedFunctionIndex = savedIndex;
//                }
//            }
//        }

//        private void SaveFunctionIndices()
//        {
//            AdsManager script = (AdsManager)target;

//            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//            string key = $"{sceneName}_{target.name}";

//            // Save the count of functions
//            PlayerPrefs.SetInt("FunctionsExist" + key, script.functions.Count);

//            // Save the indices for each function
//            for (int i = 0; i < script.functions.Count; i++)
//            {
//                PlayerPrefs.SetInt(key + i, script.functions[i].selectedFunctionIndex);
//            }

//            int newvalue = PlayerPrefs.GetInt("FunctionsExist" + key, 0);
//            if (newvalue == 0)
//            {
//                script.functions.Clear();
//            }
//        }
//        public override void OnInspectorGUI()
//        {
//            serializedObject.Update();

//            AdsManager script = (AdsManager)target;

//            EditorGUILayout.Space();

//            GUIStyle headerStylenew2 = new GUIStyle(EditorStyles.boldLabel);
//            headerStylenew2.normal.textColor = Color.green; // Set the text color to black


//            if (adsInitializer != null)
//            {
//                if (adsInitializer.ShowBannerAdsInStart == false)
//                {
//                    EditorGUILayout.LabelField("Banner Ads CallBack", headerStylenew2);
//                    EditorGUILayout.PropertyField(ShowBannerAdButton);
//                }
//            }

//            EditorGUILayout.LabelField("Interstital Ads CallBacks", headerStylenew2);

//            // Draw the reorderable list for ActionButtonsToDisplayInterstitalAds
//            actionButtonsList.DoLayoutList();

//            // Create a custom GUIStyle for the header label
//            GUIStyle headerStylenew = new GUIStyle(EditorStyles.boldLabel);
//            headerStylenew.normal.textColor = Color.green; // Set the text color to black

//            // Header Label with custom GUIStyle
//            EditorGUILayout.LabelField("Rewarded Ads CallBacks", headerStylenew);

//            // Display functions and remove buttons
//            for (int i = 0; i < script.functions.Count; i++)
//            {
//                FunctionInfo functionInfo = script.functions[i];

//                EditorGUILayout.LabelField("Rewarded Button Property" + " " + (i + 1));

//                // Add the RewardedButton field inside FunctionInfo
//                functionInfo.RewardedButton = (Button)EditorGUILayout.ObjectField("Rewarded Button", functionInfo.RewardedButton, typeof(Button), true);

//                functionInfo.script = (MonoBehaviour)EditorGUILayout.ObjectField("Add Script", functionInfo.script, typeof(MonoBehaviour), true);

//                if (functionInfo.script != null)
//                {
//                    functionInfo.scriptName = functionInfo.script.GetType().Name;
//                    functionInfo.functionNames = GetPublicVoidMethodNames(functionInfo.script);
//                    if (functionInfo.functionNames.Count > 0)
//                    {
//                        //functionInfo.selectedFunctionIndex = EditorGUILayout.Popup("Function To Invoke", functionInfo.selectedFunctionIndex, functionInfo.functionNames.ToArray());
//                        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//                        string key = sceneName + target.name + i;

//                        int GetIndex = PlayerPrefs.GetInt(key, 0);
//                        int selectedIndex = EditorGUILayout.Popup("Function To Invoke", GetIndex, functionInfo.functionNames.ToArray());

//                        // Update the selected index
//                        functionInfo.selectedFunctionIndex = selectedIndex;

//                        // Save the selected index using EditorPrefs
//                        //EditorPrefs.SetInt(key, selectedIndex);
//                        PlayerPrefs.SetInt(key, selectedIndex);

//                    }
//                }

//                count = i;

//                EditorGUILayout.Space();
//            }

//            // Add Function button
//            if (GUILayout.Button("Add Function"))
//            {
//                script.functions.Add(new FunctionInfo());
//                SaveFunctionIndices();
//            }

//            if (GUILayout.Button("Remove Function"))
//            {
//                script.functions.RemoveAt(count);
//                count--;
//                Debug.Log(script.functions.Count);
//                SaveFunctionIndices();
//            }

//            serializedObject.ApplyModifiedProperties();
//        }

//        private List<string> GetPublicVoidMethodNames(MonoBehaviour script)
//        {
//            List<string> methodNames = new List<string>();

//            Type type = script.GetType();
//            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

//            foreach (MethodInfo method in methods)
//            {
//                if (method.ReturnType == typeof(void))
//                {
//                    methodNames.Add(method.Name);
//                }
//            }

//            return methodNames;
//        }
//    }
//}