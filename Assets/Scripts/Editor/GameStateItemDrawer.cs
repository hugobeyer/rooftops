// using UnityEngine;
// using UnityEditor;
// using RoofTops;

// [CustomPropertyDrawer(typeof(DelayedActivation.GameStateItem))]
// public class GameStateItemDrawer : PropertyDrawer
// {
//     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//     {
//         EditorGUI.BeginProperty(position, label, property);

//         // Calculate rects
//         float lineHeight = EditorGUIUtility.singleLineHeight;
//         float spacing = EditorGUIUtility.standardVerticalSpacing;
        
//         // Target
//         Rect targetRect = new Rect(position.x, position.y, position.width, lineHeight);
//         EditorGUI.PropertyField(targetRect, property.FindPropertyRelative("target"), new GUIContent("Target"));
        
//         // Start Active
//         Rect startActiveRect = new Rect(position.x, targetRect.y + lineHeight + spacing, position.width, lineHeight);
//         EditorGUI.PropertyField(startActiveRect, property.FindPropertyRelative("startActive"), new GUIContent("Start Active"));
        
//         // Activate State
//         Rect activateStateRect = new Rect(position.x, startActiveRect.y + lineHeight + spacing * 2, position.width * 0.7f, lineHeight);
//         EditorGUI.PropertyField(activateStateRect, property.FindPropertyRelative("activateState"), new GUIContent("Activate"));
        
//         // Activate Delay
//         Rect activateDelayRect = new Rect(position.x + position.width * 0.7f, activateStateRect.y, position.width * 0.3f, lineHeight);
//         EditorGUI.PropertyField(activateDelayRect, property.FindPropertyRelative("activateDelay"), new GUIContent(""));
        
//         // Deactivate State
//         Rect deactivateStateRect = new Rect(position.x, activateStateRect.y + lineHeight + spacing, position.width * 0.7f, lineHeight);
//         EditorGUI.PropertyField(deactivateStateRect, property.FindPropertyRelative("deactivateState"), new GUIContent("Deactivate"));
        
//         // Deactivate Delay
//         Rect deactivateDelayRect = new Rect(position.x + position.width * 0.7f, deactivateStateRect.y, position.width * 0.3f, lineHeight);
//         EditorGUI.PropertyField(deactivateDelayRect, property.FindPropertyRelative("deactivateDelay"), new GUIContent(""));
        
//         // Destroy
//         Rect destroyRect = new Rect(position.x, deactivateStateRect.y + lineHeight + spacing, position.width, lineHeight);
//         EditorGUI.PropertyField(destroyRect, property.FindPropertyRelative("destroy"), new GUIContent("Destroy"));
        
//         EditorGUI.EndProperty();
//     }

//     public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//     {
//         float lineHeight = EditorGUIUtility.singleLineHeight;
//         float spacing = EditorGUIUtility.standardVerticalSpacing;
        
//         // 5 lines (target, start active, activate, deactivate, destroy) + spacing
//         return lineHeight * 5 + spacing * 6;
//     }
// } 