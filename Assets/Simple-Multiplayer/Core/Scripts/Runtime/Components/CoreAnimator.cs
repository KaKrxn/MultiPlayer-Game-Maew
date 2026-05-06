using UnityEngine;
using Unity.Netcode.Components;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Controls the player's Animator component based on the state of the <see cref="CoreMovement"/> controller.
    /// This component is responsible for setting locomotion parameters (speed, grounded, jump, etc.)
    /// and handling Animation Events to trigger sound effects like footsteps and landing sounds.
    /// It inherits from <see cref="NetworkAnimator"/> to automatically synchronize animation states across the network.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CoreAnimator : NetworkAnimator
    {
        #region Fields & Properties

        [Header("Component Dependencies")]
        [Tooltip("Reference to the CoreMovement component to get movement state information.")]
        [SerializeField] private CoreMovement coreMovement;

        [Header("Sound Effects")]
        [Tooltip("Sound definition for footstep sounds.")]
        [SerializeField] private SoundDef soundDefFootstep;

        [Header("Animation Parameters")]
        [Tooltip("Name of the Speed float parameter")]
        [SerializeField] private string speedParam = "Speed";
        [Tooltip("Name of the Grounded bool parameter")]
        [SerializeField] private string groundedParam = "Grounded";
        [Tooltip("Name of the Jump parameter (can be bool or trigger)")]
        [SerializeField] private string jumpParam = "Jump";
        [Tooltip("Name of the FreeFall bool parameter")]
        [SerializeField] private string freeFallParam = "FreeFall";
        [Tooltip("Name of the MotionSpeed float parameter")]
        [SerializeField] private string motionSpeedParam = "MotionSpeed";
        [Tooltip("Name of the Death bool parameter")]
        [SerializeField] private string deathParam = "die";
        
        [Tooltip("If true, treats the Jump parameter as a Trigger rather than a Bool.")]
        [SerializeField] private bool useJumpTrigger = false;

        private int m_AnimIDSpeed;
        private int m_AnimIDGrounded;
        private int m_AnimIDJump;
        private int m_AnimIDFreeFall;
        private int m_AnimIDMotionSpeed;
        private int m_AnimIDDeath;
        private CorePlayerState m_PlayerState;

        private bool m_HasSpeedParam;
        private bool m_HasGroundedParam;
        private bool m_HasJumpParam;
        private bool m_HasFreeFallParam;
        private bool m_HasMotionSpeedParam;
        private bool m_HasDeathParam;

        #endregion

        #region Unity & Network Lifecycle

        protected override void Awake()
        {
            base.Awake();
            if (coreMovement == null)
            {
                Debug.LogError("[Core Animator] needs a CoreMovement component.");
            }

            if (soundDefFootstep == null)
            {
                Debug.LogError("[Core Animator] Footstep SoundDef is not assigned.");
            }

            m_AnimIDSpeed = string.IsNullOrEmpty(speedParam) ? 0 : Animator.StringToHash(speedParam);
            m_AnimIDGrounded = string.IsNullOrEmpty(groundedParam) ? 0 : Animator.StringToHash(groundedParam);
            m_AnimIDJump = string.IsNullOrEmpty(jumpParam) ? 0 : Animator.StringToHash(jumpParam);
            m_AnimIDFreeFall = string.IsNullOrEmpty(freeFallParam) ? 0 : Animator.StringToHash(freeFallParam);
            m_AnimIDMotionSpeed = string.IsNullOrEmpty(motionSpeedParam) ? 0 : Animator.StringToHash(motionSpeedParam);
            m_AnimIDDeath = string.IsNullOrEmpty(deathParam) ? 0 : Animator.StringToHash(deathParam);

            m_PlayerState = GetComponent<CorePlayerState>();
        }

        private bool m_ParamsInitialized = false;

        private void InitializeParameters()
        {
            if (m_ParamsInitialized) return;
            if (Animator == null || Animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter param in Animator.parameters)
            {
                if (param.name == speedParam) m_HasSpeedParam = true;
                if (param.name == groundedParam) m_HasGroundedParam = true;
                if (param.name == jumpParam) m_HasJumpParam = true;
                if (param.name == freeFallParam) m_HasFreeFallParam = true;
                if (param.name == motionSpeedParam) m_HasMotionSpeedParam = true;
                if (param.name == deathParam) m_HasDeathParam = true;
            }
            m_ParamsInitialized = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (m_PlayerState != null)
            {
                m_PlayerState.OnLifeStateChanged += HandleLifeStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (m_PlayerState != null)
            {
                m_PlayerState.OnLifeStateChanged -= HandleLifeStateChanged;
            }
            base.OnNetworkDespawn();
        }

        private void HandleLifeStateChanged(PlayerLifeState newState)
        {
            bool isEliminated = (newState == PlayerLifeState.Eliminated);
            if (m_AnimIDDeath != 0 && m_HasDeathParam)
            {
                Animator.SetBool(m_AnimIDDeath, isEliminated);
            }
        }

        private void Update()
        {
            // We only want the owner to send animation state updates.
            // NetworkAnimator will handle propagating these changes to other clients.
            if (!IsOwner || coreMovement == null) return;

            UpdateLocomotionParameters();
        }

        #endregion

        #region Animation Events

        public void OnFootstepWalk(AnimationEvent animationEvent)
        {
            // >=0.5 - We want to be sure that if both walk and run weights are equal, that we only trigger one SFX.
            if (animationEvent.animatorClipInfo.weight >= 0.5f)
            {
                OnFootstep(animationEvent, 0, 0.75f, 0);
            }
        }

        public void OnFootstepRun(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                OnFootstep(animationEvent, 500, 1, 2000);
            }
        }

        /// <summary>
        /// This method is called by an AnimationEvent defined in the walk/run animation clips.
        /// It plays a random footstep sound.
        /// </summary>
        /// <param name="animationEvent">Data from the animation event.</param>
        /// <param name="walkRunPitchCents"></param>
        /// <param name="walkRunVolumeScale"></param>
        /// <param name="filterCutoffOffset"></param>
        public void OnFootstep(AnimationEvent animationEvent, float walkRunPitchCents, float walkRunVolumeScale, float filterCutoffOffset)
        {
            var overrideData = new SoundEmitter.SoundDefOverrideData
            {
                BasePitchInCents = walkRunPitchCents,
                VolumeScale = walkRunVolumeScale,
                BaseLowPassCutoff = filterCutoffOffset
            };

            CoreDirector.RequestAudio(soundDefFootstep)
                .AttachedTo(transform)
                .WithOverrides(overrideData)
                .AsReserved(SoundEmitter.ReservedInfo.ReservedEmitterAndAudioSources)
                .Play();
        }

        /// <summary>
        /// This method is called by an AnimationEvent defined in the landing animation clip.
        /// It plays the landing sound effect.
        /// </summary>
        /// <param name="animationEvent">Data from the animation event.</param>
        public void OnLand(AnimationEvent animationEvent)
        {
            CoreDirector.RequestAudio(soundDefFootstep)
                .AttachedTo(transform)
                .AsReserved(SoundEmitter.ReservedInfo.ReservedEmitterAndAudioSources)
                .Play();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads the current state from the CoreMovement component and updates the Animator parameters accordingly.
        /// </summary>
        private void UpdateLocomotionParameters()
        {
            InitializeParameters();

            bool isGrounded = coreMovement.IsGrounded;
            float verticalVelocity = coreMovement.VerticalVelocity;

            if (m_HasGroundedParam) Animator.SetBool(m_AnimIDGrounded, isGrounded);
            
            if (m_HasJumpParam)
            {
                if (useJumpTrigger)
                {
                    if (coreMovement.JumpRequested) base.SetTrigger(m_AnimIDJump);
                }
                else
                {
                    Animator.SetBool(m_AnimIDJump, !isGrounded && verticalVelocity > 0.1f);
                }
            }
            
            if (m_HasFreeFallParam) Animator.SetBool(m_AnimIDFreeFall, !isGrounded && verticalVelocity <= 0.1f);

            if (m_HasSpeedParam) Animator.SetFloat(m_AnimIDSpeed, coreMovement.CurrentSpeed);
            if (m_HasMotionSpeedParam) Animator.SetFloat(m_AnimIDMotionSpeed, coreMovement.InputMagnitude);
        }

        public void TurnInPlaceStart()
        {
            Animator.applyRootMotion = true;
        }

        public void TurnInPlaceEnd()
        {
            Animator.applyRootMotion = false;
        }

        #endregion
    }
}
