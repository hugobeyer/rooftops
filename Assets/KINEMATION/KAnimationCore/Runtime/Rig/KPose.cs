using System;
using KINEMATION.KAnimationCore.Runtime.Core;

namespace KINEMATION.KAnimationCore.Runtime.Rig
{
    // Represents the space we will modify bone transform in.
    public enum ESpaceType
    {
        BoneSpace,
        ParentBoneSpace,
        ComponentSpace,
        WorldSpace
    }

    // Whether the operation is additive or absolute.
    public enum EModifyMode
    {
        Add,
        Replace
    }
    
    // Represents the pose for the specific rig element.
    [Serializable]
    public struct KPose
    {
        public KRigElement element;
        public KTransform pose;
        public ESpaceType space;
        public EModifyMode modifyMode;
    }
}