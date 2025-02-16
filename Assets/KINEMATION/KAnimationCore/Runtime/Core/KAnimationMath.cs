// Designed by KINEMATION, 2023

using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Core
{
    public class KAnimationMath
    {
        public static Quaternion RotateInSpace(Quaternion space, Quaternion target, Quaternion rotation, float alpha)
        {
            return Quaternion.Slerp(target, space * rotation * (Quaternion.Inverse(space) * target), alpha);
        }
        
        public static Quaternion RotateInSpace(KTransform space, KTransform target, Quaternion offset, float alpha)
        {
            return RotateInSpace(space.rotation, target.rotation, offset, alpha);
        }

        public static void RotateInSpace(Transform space, Transform target, Quaternion offset, float alpha)
        {
            target.rotation = RotateInSpace(space.rotation, target.rotation, offset, alpha);
        }

        public static Vector3 MoveInSpace(KTransform space, KTransform target, Vector3 offset, float alpha)
        {
            return target.position + (space.TransformPoint(offset, false) - space.position) * alpha;
        }
        
        public static void MoveInSpace(Transform space, Transform target, Vector3 offset, float alpha)
        {
            target.position += (space.TransformPoint(offset) - space.position) * alpha;
        }
        
        public static bool IsWeightFull(float weight)
        {
            return Mathf.Approximately(weight, 1f);
        }

        public static bool IsWeightRelevant(float weight)
        {
            return !Mathf.Approximately(weight, 0f);
        }

        public static void ModifyTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.modifyMode == EModifyMode.Add)
            {
                AddTransform(component, target, in pose, alpha);
                return;
            }
            
            ReplaceTransform(component, target, in pose, alpha);
        }

        private static void AddTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.space == ESpaceType.BoneSpace)
            {
                MoveInSpace(target, target, pose.pose.position, alpha);
                RotateInSpace(target, target, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ParentBoneSpace)
            {
                Transform parent = target.parent;
                
                MoveInSpace(parent, target, pose.pose.position, alpha);
                RotateInSpace(parent, target, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                MoveInSpace(component, target, pose.pose.position, alpha);
                RotateInSpace(component, target, pose.pose.rotation, alpha);
                return;
            }

            Vector3 position = target.position;
            Quaternion rotation = target.rotation;

            target.position = Vector3.Lerp(position, position + pose.pose.position, alpha);
            target.rotation = Quaternion.Slerp(rotation, rotation * pose.pose.rotation, alpha);
        }
        
        private static void ReplaceTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.space == ESpaceType.BoneSpace || pose.space == ESpaceType.ParentBoneSpace)
            {
                target.localPosition = Vector3.Lerp(target.localPosition, pose.pose.position, alpha);
                target.localRotation = Quaternion.Slerp(target.localRotation, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                target.position = Vector3.Lerp(target.position, component.TransformPoint(pose.pose.position), alpha);
                target.rotation = Quaternion.Slerp(target.rotation, component.rotation * pose.pose.rotation, alpha);
                return;
            }

            target.position = Vector3.Lerp(target.position, pose.pose.position, alpha);
            target.rotation = Quaternion.Slerp(target.rotation, pose.pose.rotation, alpha);
        }
    }
}