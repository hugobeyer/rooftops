// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Editor.Misc;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Rig
{
    public delegate void OnTreeItemClicked(string displayName, int index);
    public delegate void OnSelectionChanged(List<(string, int)> selectedItems);
    
    public class RigTreeView : TreeView
    {
        public OnTreeItemClicked onItemClicked;
        public List<bool> selectedItems;
        
        public float singleRowHeight = 0f;
        public bool drawToggleBoxes;
        
        private List<TreeViewItem> _treeItems;
        private (string, int)[] _originalItems;
        
        public RigTreeView(TreeViewState state) : base(state)
        {
            _treeItems = new List<TreeViewItem>();
            selectedItems = new List<bool>();
            Reload();
        }

        public void InitializeTreeItems(ref (string, int)[] items)
        {
            _treeItems.Clear();
            
            int count = items.Length;
            _originalItems = new (string, int)[count];

            int depthOffset = drawToggleBoxes ? 1 : 0;
            for (int i = 0; i < count; i++)
            {
                _treeItems.Add(new TreeViewItem(i + 1, items[i].Item2 + depthOffset, items[i].Item1));
                selectedItems.Add(false);
            }
            
            items.CopyTo(_originalItems, 0);
        }
        
        public void Filter(string query)
        {
            int depthOffset = drawToggleBoxes ? 1 : 0;
            
            _treeItems.Clear();
            query = query.ToLower().Trim();
            
            int count = _originalItems.Length;
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(query))
                {
                    _treeItems.Add(new TreeViewItem(i + 1, _originalItems[i].Item2 + depthOffset,
                        _originalItems[i].Item1));
                    continue;
                }
                
                if (!_originalItems[i].Item1.ToLower().Trim().Contains(query)) continue;
                
                _treeItems.Add(new TreeViewItem(i + 1, depthOffset, _originalItems[i].Item1));
            }
            
            Reload();
        }

        public List<(string, int)> GetSelectedItemPairs()
        {
            List<(string, int)> output = new List<(string, int)>();

            int index = 0;
            foreach (var item in _originalItems)
            {
                if (selectedItems[index]) output.Add((item.Item1, index));
                index++;
            }

            return output;
        }

        protected override TreeViewItem BuildRoot()
        {
            // 0 is the root ID, -1 means the root has no parent
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            // Utility method to setup the parent/children relationship
            SetupParentsAndChildrenFromDepths(root, _treeItems);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            Color darkGrey = new Color(0.2f, 0.2f, 0.2f);
            Color lightGrey = new Color(0.25f, 0.25f, 0.25f);
            Color blue = new Color(115f / 255f, 147f / 255f, 179f / 255f, 0.25f);

            var color = args.selected ? blue : args.row % 2 == 0 ? lightGrey : darkGrey;
            EditorGUI.DrawRect(args.rowRect, color);
            
            if (drawToggleBoxes)
            {
                var rect = args.rowRect;
                rect.width = rect.height;

                bool prevToggle = selectedItems[args.item.id - 1];
                bool toggle = EditorGUI.Toggle(rect, prevToggle);

                if (toggle != prevToggle)
                {
                    // If this item is a part of a larger selection, update the status globally.
                    if (IsSelected(args.item.id))
                    {
                        var selection = GetSelection();
                        foreach (var selectedId in selection) selectedItems[selectedId - 1] = toggle;
                    } // Otherwise, change this toggle only.
                    else selectedItems[args.item.id - 1] = toggle;
                }
            }

            singleRowHeight = rowHeight;

            if (!drawToggleBoxes)
            {
                Rect buttonRect = args.rowRect;
                float indent = GetContentIndent(args.item);
                buttonRect.x += indent;
                
                if (GUI.Button(buttonRect, args.item.displayName, EditorStyles.label))
                {
                    string displayName = _originalItems[args.item.id - 1].Item1;
                    int index = args.item.id - 1;
                    onItemClicked?.Invoke(displayName, index);
                }

                return;
            }
            
            base.RowGUI(args);
        }
    }

    public class RigTreeWidget : IEditorTool
    {
        public RigTreeView rigTreeView;
        private TreeViewState _rigTreeViewState;
        
        public RigTreeWidget()
        {
            _rigTreeViewState = new TreeViewState();
            rigTreeView = new RigTreeView(_rigTreeViewState);
        }

        public void Refresh(ref (string, int)[] items)
        {
            rigTreeView.InitializeTreeItems(ref items);
            rigTreeView.Reload();
            rigTreeView.ExpandAll();
        }
        
        public void Render()
        {
            float maxHeight = rigTreeView.singleRowHeight + rigTreeView.totalHeight;
            float height = Mathf.Max(rigTreeView.singleRowHeight * 2f, maxHeight);
            
            EditorGUILayout.BeginHorizontal();
            Rect parentRect = GUILayoutUtility.GetRect(0f, 0f, 0f, height);
            EditorGUILayout.EndHorizontal();
            
            float padding = 7f;
            
            GUI.Box(parentRect, "", EditorStyles.helpBox);

            parentRect.x += padding;
            parentRect.y += padding;

            parentRect.width -= 2f * padding;
            parentRect.height -= 2f * padding;
        
            rigTreeView.OnGUI(parentRect);
        }
    }
}