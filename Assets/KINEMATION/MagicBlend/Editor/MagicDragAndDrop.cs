// Designed by KINEMATION, 2025.

using KINEMATION.MagicBlend.Runtime;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.MagicBlend.Editor
{
    public class MagicDragAndDrop
    {
        private static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects, bool perform)
        {
            var asset = DragAndDrop.objectReferences[0] as MagicBlendAsset;
            if (asset == null)
            {
                return DragAndDropVisualMode.None;
            }
            
            if (perform)
            {
                var selection = Selection.activeGameObject;
                if (selection != null)
                {
                    var component = selection.GetComponent<MagicBlending>();
                    if (component == null) component = selection.AddComponent<MagicBlending>();
                    component.SetMagicBlendAsset(asset);
                }
            }
            
            return DragAndDropVisualMode.Copy;
        }
        
        private static DragAndDropVisualMode OnInspectorDrop(UnityEngine.Object[] targets, bool perform)
        {
            var asset = DragAndDrop.objectReferences[0] as MagicBlendAsset;
            if (asset == null)
            {
                return DragAndDropVisualMode.None;
            }
            
            if (perform)
            {
                var selection = Selection.activeGameObject;
                if (selection != null)
                {
                    var component = selection.GetComponent<MagicBlending>();
                    if (component == null) component = selection.AddComponent<MagicBlending>();
                    component.SetMagicBlendAsset(asset);
                }
            }

            return DragAndDropVisualMode.Copy;
        }
        
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            DragAndDrop.AddDropHandler(OnInspectorDrop);
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
        }
    }
}