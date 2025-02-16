// Designed by KINEMATION, 2025.

using System.Collections.Generic;

using KINEMATION.KAnimationCore.Editor.Misc;
using KINEMATION.KAnimationCore.Runtime.Rig;
using KINEMATION.MagicBlend.Runtime;

using UnityEditor;
using UnityEngine;

namespace KINEMATION.MagicBlend.Editor
{
    public class MagicBlendContextMenu
    {
        private const string ItemName = "Assets/Create Magic Blend";

        public static KRigElementChain[] GetMatchingChains(KRig rig, string[] queries)
        {
            List<KRigElementChain> chains = new List<KRigElementChain>();

            foreach (var elementChain in rig.rigElementChains)
            {
                foreach (var query in queries)
                {
                    if (!elementChain.chainName.ToLower().Contains(query.ToLower())) continue;
                    
                    chains.Add(elementChain);
                    break;
                }
            }
            
            return chains.ToArray();
        }

        public static KRigElementChain MergeChains(string name, in KRigElementChain[] chains)
        {
            KRigElementChain mergedChain = new KRigElementChain();
            mergedChain.chainName = name;

            foreach (var chain in chains)
            {
                if(chain == null) continue;
                foreach (var element in chain.elementChain) mergedChain.elementChain.Add(element);
            }

            return mergedChain;
        }

        public static void AddCompositeChain(MagicBlendAsset blendAsset, string name, string[] queries)
        {
            blendAsset.layeredBlends.Add(new LayeredBlend()
            {
                layer = MergeChains(name, GetMatchingChains(blendAsset.rigAsset, queries))
            });
        }
        
        [MenuItem(ItemName, true)]
        private static bool ValidateCreateRigMapping()
        {
            return Selection.activeObject is KRig;
        }

        [MenuItem(ItemName)]
        private static void CreateRigMapping()
        {
            KRig rig = Selection.activeObject as KRig;
            if (rig == null) return;
            
            MagicBlendAsset blendAsset = ScriptableObject.CreateInstance<MagicBlendAsset>();
            blendAsset.rigAsset = rig;

            AddCompositeChain(blendAsset, "LowerBody", new[] {"pelvis", "hip", "leg", "thigh"});
            AddCompositeChain(blendAsset, "Spine", new[] {"spine"});
            AddCompositeChain(blendAsset, "Head", new[] {"neck", "head"});
            AddCompositeChain(blendAsset, "Arms", new[] {"arm", "clavicle", "shoulder"});
            AddCompositeChain(blendAsset, "Fingers", new[]
            {
                "finger", "index", "thumb", "pinky", "ring", "middle"
            });

            string assetName = rig.name.Replace("Rig_", "");
            assetName = assetName.Replace("Rig", "");
            
            KEditorUtility.SaveAsset(blendAsset, KEditorUtility.GetProjectWindowFolder(), 
                $"MagicBlend_{assetName}.asset");
        }
    }
}