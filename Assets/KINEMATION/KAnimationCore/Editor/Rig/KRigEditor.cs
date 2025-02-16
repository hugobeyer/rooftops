using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Editor.Rig
{
    [CustomEditor(typeof(KRig), true)]
    public class KRigEditor : UnityEditor.Editor
    {
        private KRig _rigAsset;
        private KRigComponent _rigComponent;

        private SerializedProperty _rigElementChains;
        private SerializedProperty _rigCurves;

        private KToolbarWidget _kToolbarWidget;
        private RigTreeWidget _rigTreeWidget;

        private (string, int)[] _elementNamesAndDepths;

        private void PopulateNameContainer()
        {
            if (_rigAsset.rigHierarchy == null) return;

            int count = _rigAsset.rigHierarchy.Count;
            _elementNamesAndDepths = new (string, int)[count];
            
            for (int i = 0; i < count; i++)
            {
                string elementName = _rigAsset.rigHierarchy[i].name;
                int depth = _rigAsset.rigDepths[i];
                _elementNamesAndDepths[i] = (elementName, depth);
            }
        }
        
        private void RenderHierarchy()
        {
            if (_elementNamesAndDepths.Length == 0) return;
            
            _rigTreeWidget.Render();
        }

        private void RenderElementChains()
        {
            EditorGUILayout.PropertyField(_rigElementChains);
        }

        private void RenderCurves()
        {
            EditorGUILayout.PropertyField(_rigCurves);
        }
        
        private void OnEnable()
        {
            _rigAsset = (KRig) target;
            
            _rigElementChains = serializedObject.FindProperty("rigElementChains");
            _rigCurves = serializedObject.FindProperty("rigCurves");

            _kToolbarWidget = new KToolbarWidget(new KToolbarTab[]
            {
                new KToolbarTab()
                {
                    name = "Hierarchy",
                    onTabRendered = RenderHierarchy
                },
                new KToolbarTab()
                {
                    name = "Element Chains",
                    onTabRendered = RenderElementChains
                },
                new KToolbarTab()
                {
                    name = "Curves",
                    onTabRendered = RenderCurves
                }
            });
            
            PopulateNameContainer();
            _rigTreeWidget = new RigTreeWidget();
            _rigTreeWidget.Refresh(ref _elementNamesAndDepths);
        }

        private bool ImportRig()
        {
            if (_rigAsset == null)
            {
                return false;
            }
            
            _rigAsset.ImportRig(_rigComponent);
            return true;
        }

        public override void OnInspectorGUI()
        {
            _rigComponent = (KRigComponent) EditorGUILayout.ObjectField("Rig Component", 
                _rigComponent, typeof(KRigComponent), true);
            
            _rigAsset.targetAnimator = (RuntimeAnimatorController) EditorGUILayout.ObjectField("Animator", 
                _rigAsset.targetAnimator, typeof(RuntimeAnimatorController), true);
            
            _rigAsset.inputConfig = (UserInputConfig) EditorGUILayout.ObjectField("Input Config", 
                _rigAsset.inputConfig, typeof(UserInputConfig), true);

            if (_rigComponent == null)
            {
                EditorGUILayout.HelpBox("Rig Component not specified", MessageType.Warning);
            }
            else if (GUILayout.Button("Import Rig"))
            {
                ImportRig();
                PopulateNameContainer();
                _rigTreeWidget.Refresh(ref _elementNamesAndDepths);
            }
            
            _kToolbarWidget.Render();
            serializedObject.ApplyModifiedProperties();
        }
    }
}