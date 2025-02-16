// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Reflection;
using UnityEditor;

namespace KINEMATION.KAnimationCore.Editor.Attributes
{
    public class RigDrawerUtility
    {
        public static KRig TryGetRigAsset(FieldInfo fieldInfo, SerializedProperty property)
        {
            KRig rig = null;

            RigAssetSelectorAttribute assetAttribute = null;
            foreach (var customAttribute in fieldInfo.GetCustomAttributes(false))
            {
                if (customAttribute is RigAssetSelectorAttribute)
                {
                    assetAttribute = customAttribute as RigAssetSelectorAttribute;
                }
            }
            
            if (assetAttribute != null && !string.IsNullOrEmpty(assetAttribute.assetName))
            {
                if (property.serializedObject.FindProperty(assetAttribute.assetName) is var prop)
                {
                    rig = prop.objectReferenceValue as KRig;
                }
            }

            if (rig == null)
            {
                rig = property.serializedObject.targetObject as KRig;
            }
            
            if (rig == null)
            {
                rig = (property.serializedObject.targetObject as IRigUser)?.GetRigAsset();
            }

            return rig;
        }
    }
}