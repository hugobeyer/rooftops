// Designed by KINEMATION, 2025.

using UnityEngine;

namespace KINEMATION.MagicBlend.Runtime
{
    public class MagicBlendState : StateMachineBehaviour
    {
        [SerializeField] private MagicBlendAsset magicBlendAsset;
        private bool _isInitialized;
        private MagicBlending _magicBlending;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isInitialized)
            {
                _magicBlending = animator.gameObject.GetComponent<MagicBlending>();
                if (_magicBlending == null) return;

                _isInitialized = true;
            }

            float blendTime = animator.GetAnimatorTransitionInfo(layerIndex).duration;
            _magicBlending.UpdateMagicBlendAsset(magicBlendAsset, true, blendTime);
        }
    }
}