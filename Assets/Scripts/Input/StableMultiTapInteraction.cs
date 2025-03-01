namespace UnityEngine.InputSystem.Interactions
{
#if UNITY_EDITOR
    using UnityEditor;
    // Allow for the interaction to be utilized outside of Play Mode and so that it will actually show up as an option in the Input Manager
    [InitializeOnLoad]
#endif
    [Scripting.Preserve, System.ComponentModel.DisplayName("StableMultiTap"), System.Serializable]
    public class StableMultiTapInteraction : IInputInteraction<float>
    {
        /// <summary>
        /// The time in seconds within which the control needs to be pressed and released to perform the interaction.
        /// </summary>
        /// <remarks>
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultTapTime"/>) instead.
        /// </remarks>
        [Tooltip("The maximum time (in seconds) allowed to elapse between pressing and releasing a control for it to register as a tap.")]
        public float tapTime;

        /// <summary>
        /// The time in seconds which is allowed to pass between taps.
        /// </summary>
        /// <remarks>
        /// If this time is exceeded, the multi-tap interaction is canceled.
        /// If this value is equal to or smaller than zero, the input system will use the duplicate value of <see cref="tapTime"/> instead.
        /// </remarks>
        [Tooltip("The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is canceled.")]
        public float tapDelay;

        /// <summary>
        /// The number of taps required to perform the interaction.
        /// </summary>
        /// <remarks>
        /// How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.
        /// </remarks>
        [Tooltip("How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.")]
        public int tapCount = 2;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        public float maxTapDistance = 5;

        private float tapTimeOrDefault => tapTime > 0.0 ? tapTime : InputSystem.settings.defaultTapTime;
        internal float tapDelayOrDefault => tapDelay > 0.0 ? tapDelay : InputSystem.settings.multiTapDelayTime;
        private float pressPointOrDefault => pressPoint;
        private float releasePointOrDefault => pressPointOrDefault;

        /// <inheritdoc />
        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                // We use timers multiple times but no matter what, if they expire it means
                // that we didn't get input in time.
                context.Canceled();
                return;
            }

            switch (m_CurrentTapPhase)
            {
                case TapPhase.None:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                        m_CurrentTapStartTime = context.time;
                        context.Started();

                        var maxTapTime = tapTimeOrDefault;
                        var maxDelayInBetween = tapDelayOrDefault;
                        context.SetTimeout(maxTapTime);

                        // We'll be using multiple timeouts so set a total completion time that
                        // effects the result of InputAction.GetTimeoutCompletionPercentage()
                        // such that it accounts for the total time we allocate for the interaction
                        // rather than only the time of one single timeout.
                        context.SetTotalTimeoutCompletionTime(maxTapTime * tapCount + (tapCount - 1) * maxDelayInBetween);
                    }
                    break;

                case TapPhase.WaitingForNextRelease:
                    if (!context.ControlIsActuated(releasePointOrDefault))
                    {
                        if (context.time - m_CurrentTapStartTime <= tapTimeOrDefault)
                        {
                            ++m_CurrentTapCount;
                            if (m_CurrentTapCount >= tapCount && (Input.mousePosition - m_LastTapPosition).sqrMagnitude < maxTapDistance * maxTapDistance)
                            {
                                context.Performed();
                            }
                            else
                            {
                                m_CurrentTapPhase = TapPhase.WaitingForNextPress;
                                m_LastTapReleaseTime = context.time;
                                context.SetTimeout(tapDelayOrDefault);
                                m_LastTapPosition = Input.mousePosition;
                            }
                        }
                        else
                        {
                            context.Canceled();
                        }
                    }
                    break;

                case TapPhase.WaitingForNextPress:
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        if (context.time - m_LastTapReleaseTime <= tapDelayOrDefault)
                        {
                            m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                            m_CurrentTapStartTime = context.time;
                            context.SetTimeout(tapTimeOrDefault);
                        }
                        else
                        {
                            context.Canceled();
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_CurrentTapPhase = TapPhase.None;
            m_CurrentTapCount = 0;
            m_CurrentTapStartTime = 0;
            m_LastTapReleaseTime = 0;
            m_LastTapPosition = Vector3.zero;
        }

        private TapPhase m_CurrentTapPhase;
        private int m_CurrentTapCount;
        private double m_CurrentTapStartTime;
        private double m_LastTapReleaseTime;
        private Vector3 m_LastTapPosition = Vector3.zero;

        private enum TapPhase
        {
            None,
            WaitingForNextRelease,
            WaitingForNextPress,
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterInteraction()
        {
            if (InputSystem.TryGetInteraction("StableMultiTap") == null)
            {
                //For some reason if this is called again when it already exists, it permanently removees it from the drop-down options... So have to check first
                InputSystem.RegisterInteraction<StableMultiTapInteraction>("StableMultiTap");
            }
        }

        //Constructor will be called by our Editor [InitializeOnLoad] attribute when outside Play Mode
        static StableMultiTapInteraction() => RegisterInteraction();
    }
}