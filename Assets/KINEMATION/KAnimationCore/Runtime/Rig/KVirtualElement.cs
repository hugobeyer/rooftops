using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Rig
{
    public class KVirtualElement : MonoBehaviour
    {
        public Transform targetBone;

        public void Animate()
        {
            transform.position = targetBone.position;
            transform.rotation = targetBone.rotation;
        }
    }
}