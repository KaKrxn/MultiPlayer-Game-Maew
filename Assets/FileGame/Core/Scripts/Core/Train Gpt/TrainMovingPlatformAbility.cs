using UnityEngine;
using Blocks.Gameplay.Core;

namespace Game.Train
{
    /// <summary>
    /// IMovementAbility that makes the player stick to a moving Train by injecting platform delta
    /// into CoreMovement.FinalMoveCalculationOverride (template-friendly, avoids double Move()).
    /// </summary>
    public class TrainMovingPlatformAbility : MonoBehaviour, IMovementAbility
    {
        public int Priority => 5;
        public float StaminaCost => 0f;

        [Header("Detect Train")]
        [SerializeField] private string trainLayerName = "Train";
        [Tooltip("Optional. Leave empty to ignore tag check.")]
        [SerializeField] private string trainTag = "Train";

        private CoreMovement motor;
        private CharacterController controller;

        private bool isOnTrain;
        private Transform currentTrainRoot;
        private Vector3 lastTrainPos;

        // Chain any previous override (important if other systems also use it)
        private System.Func<Vector3, Vector3> previousOverride;

        private int trainLayer = -1;

        public void Initialize(CoreMovement motor)
        {
            this.motor = motor;
            controller = motor.GetComponent<CharacterController>();

            trainLayer = LayerMask.NameToLayer(trainLayerName);

            previousOverride = motor.FinalMoveCalculationOverride;
            motor.FinalMoveCalculationOverride = CalculateFinalMovementWithTrain;
        }

        public MovementModifier Process() => new MovementModifier();
        public bool TryActivate() => false;

        private void OnDestroy()
        {
            if (motor == null) return;
            if (motor.FinalMoveCalculationOverride == CalculateFinalMovementWithTrain)
                motor.FinalMoveCalculationOverride = previousOverride;
        }

        private Vector3 CalculateFinalMovementWithTrain(Vector3 playerMove)
        {
            // Let earlier override run first (if any)
            Vector3 baseMove = previousOverride != null ? previousOverride(playerMove) : playerMove;

            DetectTrain();

            if (isOnTrain && currentTrainRoot != null)
            {
                Vector3 trainDelta = currentTrainRoot.position - lastTrainPos;

                // Add platform delta to horizontal; keep vertical from baseMove (jump/gravity)
                Vector3 final = new Vector3(baseMove.x, 0f, baseMove.z) + trainDelta;
                final.y = baseMove.y;

                lastTrainPos = currentTrainRoot.position;
                return final;
            }

            return baseMove;
        }

        private void DetectTrain()
        {
            // SphereCast เหมือนของ template (นิ่งกว่า Raycast ตรงๆ)
            float checkDistance = (controller.height / 2f) - controller.radius + 0.1f;
            Vector3 origin = controller.center + transform.position;

            if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit,
                    checkDistance, motor.groundLayers, QueryTriggerInteraction.Ignore))
            {
                // Optional tag filter
                if (!string.IsNullOrEmpty(trainTag) && !hit.collider.CompareTag(trainTag))
                {
                    ClearTrain();
                    return;
                }

                Transform root = hit.collider.transform.root;

                // Layer check (Train layer)
                if (trainLayer >= 0)
                {
                    bool isTrainLayer = root.gameObject.layer == trainLayer || hit.collider.gameObject.layer == trainLayer;
                    if (!isTrainLayer)
                    {
                        ClearTrain();
                        return;
                    }
                }

                // Accept as train
                if (!isOnTrain || currentTrainRoot != root)
                {
                    isOnTrain = true;
                    currentTrainRoot = root;
                    lastTrainPos = currentTrainRoot.position;
                }

                return;
            }

            ClearTrain();
        }

        private void ClearTrain()
        {
            isOnTrain = false;
            currentTrainRoot = null;
        }
    }
}