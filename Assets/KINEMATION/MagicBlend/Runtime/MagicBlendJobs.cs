// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Runtime.Core;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace KINEMATION.MagicBlend.Runtime
{
    // Processes base locomotion pose.
    public struct PoseJob : IAnimationJob
    {
        [ReadOnly] public bool alwaysAnimate;
        [ReadOnly] public bool readPose;
        [ReadOnly] public TransformSceneHandle root;
        public NativeArray<BlendStreamAtom> atoms;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!alwaysAnimate && !readPose)
            {
                return;
            }
            
            KTransform rootTransform = new KTransform()
            {
                rotation = root.GetRotation(stream),
                position = root.GetPosition(stream)
            };
            
            int num = atoms.Length;
            for (int i = 0; i < num; i++)
            {
                var atom = atoms[i];
                KTransform atomTransform = new KTransform()
                {
                    position = atom.handle.GetPosition(stream),
                    rotation = atom.handle.GetRotation(stream)
                };

                atomTransform = rootTransform.GetRelativeTransform(atomTransform, false);
                atom.activePose.basePose = atomTransform;
                atom.activePose.basePose.position = atom.handle.GetLocalPosition(stream);
                atoms[i] = atom;
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
    
    // Processes active stream pose.
    public struct OverlayJob : IAnimationJob
    {
        [ReadOnly] public bool alwaysAnimate;
        [ReadOnly] public bool cachePose;
        [ReadOnly] public TransformSceneHandle root;
        public NativeArray<BlendStreamAtom> atoms;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!alwaysAnimate && !cachePose)
            {
                return;
            }
            
            KTransform rootTransform = new KTransform()
            {
                rotation = root.GetRotation(stream),
                position = root.GetPosition(stream)
            };
            
            int num = atoms.Length;
            for (int i = 0; i < num; i++)
            {
                var atom = atoms[i];
                
                KTransform atomTransform = new KTransform()
                {
                    rotation = atom.handle.GetRotation(stream),
                    position = atom.handle.GetPosition(stream)
                };

                atomTransform = rootTransform.GetRelativeTransform(atomTransform, false);
                
                atom.activePose.overlayPose = atomTransform;
                atom.activePose.overlayPose.position = atom.handle.GetLocalPosition(stream);
                atom.activePose.localOverlayRotation = atom.handle.GetLocalRotation(stream);
                
                atoms[i] = atom;
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
    
    // Processes final layering.
    public struct LayeringJob : IAnimationJob
    {
        [ReadOnly] public float blendWeight;
        [ReadOnly] public bool cachePose;
        [ReadOnly] public TransformSceneHandle root;
        public NativeArray<BlendStreamAtom> atoms;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            KTransform rootTransform = new KTransform()
            {
                rotation = root.GetRotation(stream),
                position = root.GetPosition(stream)
            };
            
            int num = atoms.Length;

            // Refresh the current pose.
            for (int i = 0; i < num; i++)
            {
                var atom = atoms[i];
                
                KTransform atomTransform = new KTransform()
                {
                    rotation = atom.handle.GetRotation(stream),
                    position = atom.handle.GetPosition(stream),
                    scale = Vector3.one
                };
                
                atom.meshStreamPose = rootTransform.GetRelativeTransform(atomTransform, false);
                atom.meshStreamPose.position = atom.handle.GetLocalPosition(stream);
                
                atom.activePose.additiveWeight = atom.additiveWeight;
                atom.activePose.baseWeight = atom.baseWeight;
                atom.activePose.localWeight = atom.localWeight;
                
                atoms[i] = atom;
            }
            
            // Apply mesh-space additive.
            for (int i = 0; i < num; i++)
            {
                var atom = atoms[i];
                AtomPose blendedPose = atom.GetBlendedAtomPose(blendWeight);
                
                if (cachePose)
                {
                    atom.cachedPose = blendedPose;
                    atoms[i] = atom;
                }

                KTransform meshBasePose = blendedPose.basePose;
                KTransform meshOverlayPose = blendedPose.overlayPose;
                Quaternion localOverlayRotation = blendedPose.localOverlayRotation;

                float additiveWeight = blendedPose.additiveWeight;
                float baseWeight = blendedPose.baseWeight;
                float localWeight = blendedPose.localWeight;
                
                KTransform additive = new KTransform()
                {
                    rotation = atom.meshStreamPose.rotation * Quaternion.Inverse(meshBasePose.rotation),
                    position = atom.meshStreamPose.position - meshBasePose.position
                };
                
                Quaternion rotation = additive.rotation * meshOverlayPose.rotation;
                
                // Blend additive.
                rotation = Quaternion.Slerp(meshOverlayPose.rotation, rotation, additiveWeight);
                // Blend locomotion pose.
                rotation = Quaternion.Slerp(atom.meshStreamPose.rotation, rotation, baseWeight);
                // Convert to world space.
                rotation = rootTransform.rotation * rotation;

                Vector3 position = meshOverlayPose.position + additive.position * additiveWeight;
                position = Vector3.Lerp(atom.meshStreamPose.position, position, baseWeight);
                
                atom.handle.SetRotation(stream, rotation);
                rotation = Quaternion.Slerp(atom.handle.GetLocalRotation(stream), localOverlayRotation, localWeight);
                atom.handle.SetLocalRotation(stream, rotation);

                position = Vector3.Lerp(position, meshOverlayPose.position, localWeight);
                atom.handle.SetLocalPosition(stream, position);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}