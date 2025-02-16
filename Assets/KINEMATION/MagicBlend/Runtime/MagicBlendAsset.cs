// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;
using UnityEngine;

namespace KINEMATION.MagicBlend.Runtime
{
    public class MagicBlendAsset : ScriptableObject, IRigUser
    {
        [Header("Rig")]
        public KRig rigAsset;
        
        [Header("Blending")]
        [Min(0f)] public float blendTime = 0.15f;
        public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Poses")]
        public AnimationClip basePose;
        public AnimationClip overlayPose;
        public List<OverrideOverlay> overrideOverlays = new List<OverrideOverlay>();
        [Min(0f)] public float overlaySpeed = 1f;
        [Tooltip("If Overlay is static or not.")]
        public bool isAnimation = false;
        
        [Unfold] public List<LayeredBlend> layeredBlends = new List<LayeredBlend>();
        [Range(0f, 1f)] public float globalWeight = 1f;

        public KRig GetRigAsset()
        {
            return rigAsset;
        }

        public bool HasOverrides()
        {
            if (overrideOverlays.Count == 0) return false;
            foreach (var overlay in overrideOverlays) if (overlay.overlay == null) return false;
            return true;
        }
    }
}