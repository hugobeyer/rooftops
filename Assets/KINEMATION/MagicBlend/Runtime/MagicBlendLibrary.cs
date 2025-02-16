// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.MagicBlend.Runtime
{
    public struct AtomPose
    {
        public KTransform basePose;
        public KTransform overlayPose;
        public Quaternion localOverlayRotation;
        
        public float baseWeight;
        public float additiveWeight;
        public float localWeight;

        public static AtomPose Lerp(AtomPose a, AtomPose b, float alpha)
        {
            AtomPose outPose = new AtomPose();
            
            outPose.basePose = KTransform.Lerp(a.basePose, b.basePose, alpha);
            outPose.overlayPose = KTransform.Lerp(a.overlayPose, b.overlayPose, alpha);
            outPose.localOverlayRotation = Quaternion.Slerp(a.localOverlayRotation, b.localOverlayRotation, alpha);

            outPose.additiveWeight = Mathf.Lerp(a.additiveWeight, b.additiveWeight, alpha);
            outPose.baseWeight = Mathf.Lerp(a.baseWeight, b.baseWeight, alpha);
            outPose.localWeight = Mathf.Lerp(a.localWeight, b.localWeight, alpha);
            
            return outPose;
        }
    }
    
    public struct BlendStreamAtom
    {
        [ReadOnly] public TransformStreamHandle handle;
        [ReadOnly] public float baseWeight;
        [ReadOnly] public float additiveWeight;
        [ReadOnly] public float localWeight;
        
        public KTransform meshStreamPose;
        public AtomPose activePose;
        public AtomPose cachedPose;
        
        public AtomPose GetBlendedAtomPose(float blendWeight)
        {
            return AtomPose.Lerp(cachedPose, activePose, blendWeight);
        }
    }

    [Serializable]
    public struct LayeredBlend
    {
        public KRigElementChain layer;
        [Range(0f, 1f)] public float baseWeight;
        [Range(0f, 1f)] public float additiveWeight;
        [Range(0f, 1f)] public float localWeight;
    }

    [Serializable]
    public struct OverrideOverlay
    {
        public AnimationClip overlay;
        public AvatarMask mask;
        [Range(0f, 1f)] public float weight;
    }
    
    public class MagicBlendLibrary
    {
        public static NativeArray<BlendStreamAtom> SetupBlendAtoms(Animator animator, KRigComponent rigComponent)
        {
            var bones = rigComponent.GetRigTransforms();
            
            int num = bones.Length;
            var blendAtoms = new NativeArray<BlendStreamAtom>(num, Allocator.Persistent);
            for (int i = 0; i < num; i++)
            {
                Transform bone = bones[i];
                blendAtoms[i] = new BlendStreamAtom()
                {
                    handle = animator.BindStreamTransform(bone)
                };
            }

            return blendAtoms;
        }

        public static void ConnectPose(AnimationScriptPlayable playable, PlayableGraph graph, AnimationClip pose, 
            float speed = 0f)
        {
            if (playable.GetInput(0).IsValid())
            {
                playable.DisconnectInput(0);
            }

            var posePlayable = AnimationClipPlayable.Create(graph, pose);
            posePlayable.SetSpeed(speed);
            posePlayable.SetApplyFootIK(false);
            
            playable.ConnectInput(0, posePlayable, 0, 1f);
        }
        
        public static void ConnectOverlays(AnimationScriptPlayable playable, PlayableGraph graph,
            AnimationClip pose, List<OverrideOverlay> overrides, float speed = 0f)
        {
            if (playable.GetInput(0).IsValid())
            {
                playable.DisconnectInput(0);
            }
            
            if (overrides == null || overrides.Count == 0)
            {
                ConnectPose(playable, graph, pose, speed);
                return;
            }

            var mixer = AnimationLayerMixerPlayable.Create(graph);
            
            var overlayPlayable = AnimationClipPlayable.Create(graph, pose);
            overlayPlayable.SetDuration(pose.length);
            overlayPlayable.SetSpeed(speed);
            overlayPlayable.SetApplyFootIK(false);
            
            mixer.AddInput(overlayPlayable, 0, 1f);
            
            foreach (var overlayOverride in overrides)
            {
                var posePlayable = AnimationClipPlayable.Create(graph, overlayOverride.overlay);
                posePlayable.SetDuration(overlayOverride.overlay.length);
                posePlayable.SetSpeed(speed);
                posePlayable.SetApplyFootIK(false);
                
                var index = mixer.AddInput(posePlayable, 0, overlayOverride.weight);
                var mask = overlayOverride.mask == null ? new AvatarMask() : overlayOverride.mask;
                
                mixer.SetLayerMaskFromAvatarMask((uint) index, mask);
            }
            
            playable.ConnectInput(0, mixer, 0, 1f);
        }
    }
}