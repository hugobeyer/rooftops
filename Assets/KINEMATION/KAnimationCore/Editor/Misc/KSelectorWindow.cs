using System;
using KINEMATION.KAnimationCore.Editor.Rig;

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Misc
{
    public class KSelectorWindow : EditorWindow
    {
        private OnTreeItemClicked _onClicked;
        private OnSelectionChanged _onSelectionChanged;
        
        private Vector2 _scrollPosition;
        private string _searchEntry = string.Empty;

        private RigTreeWidget _rigTreeWidget;
        private bool _useSelection = false;
        
        public static void ShowWindow(ref List<string> names, ref List<int> depths, OnTreeItemClicked onClicked, 
            OnSelectionChanged onSelectionChanged, bool useSelection, List<int> selection = null, string title = "Selection")
        {
            KSelectorWindow window = CreateInstance<KSelectorWindow>();

            window._useSelection = useSelection;
            window._onClicked = onClicked;
            window._onSelectionChanged = onSelectionChanged;
            window.titleContent = new GUIContent(title);

            (string, int)[] namesAndDepths = new (string, int)[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                namesAndDepths[i] = (names[i], depths[i]);
            }
            
            window._rigTreeWidget = new RigTreeWidget
            {
                rigTreeView =
                {
                    drawToggleBoxes = useSelection,
                    onItemClicked = window.OnItemClicked
                }
            };

            window._rigTreeWidget.Refresh(ref namesAndDepths);

            if (window._useSelection && selection != null)
            {
                foreach (var selectedIndex in selection) 
                    window._rigTreeWidget.rigTreeView.selectedItems[selectedIndex] = true;
                
            }
            
            window.ShowAuxWindow();
        }

        private void OnItemClicked(string itemName, int index)
        {
            _onClicked.Invoke(itemName, index);
            Close();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
            _searchEntry = EditorGUILayout.TextField(_searchEntry, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();
            
            _rigTreeWidget.rigTreeView.Filter(_searchEntry);
            _rigTreeWidget.Render();
        }

        private void OnDisable()
        {
            if (_useSelection) _onSelectionChanged?.Invoke(_rigTreeWidget.rigTreeView.GetSelectedItemPairs());
        }
    }
}