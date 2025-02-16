// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Linq;

using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(KRigElement))]
    public class RigElementDrawer : PropertyDrawer
    {
        private void SelectRigElement(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            KRig rig = RigDrawerUtility.TryGetRigAsset(fieldInfo, property);
            
            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty index = property.FindPropertyRelative("index");

            if (rig != null)
            {
                var rigHierarchy = rig.rigHierarchy;
                
                // Calculate label width
                float labelWidth = EditorGUIUtility.labelWidth;
                float indentLevel = EditorGUI.indentLevel;

                // Calculate button width and property field width
                float totalWidth = position.width - indentLevel - labelWidth;

                // Display the default property field
                Rect propertyFieldRect = new Rect(position.x + indentLevel, position.y,
                    labelWidth, position.height);
                
                GUI.enabled = false;
                EditorGUI.LabelField(propertyFieldRect, label.text);
                GUI.enabled = true;

                // Display the bone selection button
                Rect buttonRect = new Rect(position.x + indentLevel + labelWidth, position.y,
                    totalWidth, EditorGUIUtility.singleLineHeight);

                string currentName = string.IsNullOrEmpty(name.stringValue) ? "None" : name.stringValue;

                if (GUI.Button(buttonRect, currentName))
                {
                    var elementNames = rigHierarchy.Select(element => element.name).ToList();
                    KSelectorWindow.ShowWindow(ref elementNames, ref rig.rigDepths,
                        (selectedName, selectedIndex) =>
                        {
                            name.stringValue = selectedName;
                            index.intValue = selectedIndex;
                            name.serializedObject.ApplyModifiedProperties();
                        },
                        items => { },
                        false, null, "Rig Element Selection"
                    );
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, name, label, true);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SelectRigElement(position, property, label);
        }
    }
}