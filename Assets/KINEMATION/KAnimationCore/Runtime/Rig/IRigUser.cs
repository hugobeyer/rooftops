namespace KINEMATION.KAnimationCore.Runtime.Rig
{
    public interface IRigUser
    {
        // Must return a reference to the used Rig Asset.
        public KRig GetRigAsset();
    }
}