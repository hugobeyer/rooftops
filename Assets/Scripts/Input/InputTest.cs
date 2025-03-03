using UnityEngine;

namespace RoofTops
{
    public class InputTest : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private TMPro.TextMeshProUGUI jumpEventStateText = null;

        [SerializeField]
        private TMPro.TextMeshProUGUI isHeldEventStateText = null;

        [SerializeField]
        private TMPro.TextMeshProUGUI isHeldTimeState = null;
        #endregion // Properties


        #region Unity Methods


        private void Start()
        {
            InputActionManager.Instance.OnJumpPressed.AddListener(JumpPerformed);
            InputActionManager.Instance.OnJumpReleased.AddListener(JumpReleased);
            InputActionManager.Instance.OnJumpHeldStarted.AddListener(JumpHeldPerformed);
            InputActionManager.Instance.OnJumpHeldUpdate.AddListener(JumpHeldUpdatePerformed);
        }



        private void OnDisable()
        {
            InputActionManager.Instance.OnJumpPressed.RemoveListener(JumpPerformed);
            InputActionManager.Instance.OnJumpReleased.RemoveListener(JumpReleased);
            InputActionManager.Instance.OnJumpHeldStarted.RemoveListener(JumpHeldPerformed);
            InputActionManager.Instance.OnJumpHeldUpdate.RemoveListener(JumpHeldUpdatePerformed);

        }

        #endregion // Unity Methods



        private void JumpReleased()
        {
            isHeldTimeState.text = "0";
            jumpEventStateText.text = "";
            isHeldEventStateText.text = "";
        }

        private void JumpPerformed()
        {
            jumpEventStateText.text = "Jumping!";
        }

        private void JumpHeldPerformed()
        {
            isHeldEventStateText.text = "Jumping Held!";
        }

        private void JumpHeldUpdatePerformed()
        {
            isHeldTimeState.text = InputActionManager.Instance.JumpPressedTime.ToString();
        }

    }
}
