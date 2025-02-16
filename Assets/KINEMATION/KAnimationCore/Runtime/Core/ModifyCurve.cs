// Designed by KINEMATION, 2024.

using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Core
{
    public class ModifyCurve : StateMachineBehaviour
    {
        [SerializeField] private string paramName;
        [SerializeField] private float paramTargetValue;

        private int _paramId;
        private float _paramStartValue;
        private bool _isInitialized;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isInitialized)
            {
                _paramId = Animator.StringToHash(paramName);
                _isInitialized = true;
            }
            
            _paramStartValue = animator.GetFloat(_paramId);
        }
        
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            int nextHash = animator.GetNextAnimatorStateInfo(layerIndex).fullPathHash;
            if (nextHash != stateInfo.fullPathHash && nextHash != 0)
            {
                return;
            }
        
            float alpha = 0f;
            if (animator.IsInTransition(layerIndex))
            {
                alpha = animator.GetAnimatorTransitionInfo(layerIndex).normalizedTime;
            }
            else
            {
                alpha = 1f;
            }
        
            animator.SetFloat(_paramId, Mathf.Lerp(_paramStartValue, paramTargetValue, alpha));
        }
        
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }
    }
}
