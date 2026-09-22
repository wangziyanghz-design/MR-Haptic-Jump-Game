using System;
using MRKnobJump.Gameplay;
using MRKnobJump.Input;
using MRKnobJump.Platform;
using UnityEngine;

namespace MRKnobJump.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerJumpController : MonoBehaviour
    {
        [SerializeField] private CompressionController compressionController;
        [SerializeField] private JumpPhysics jumpPhysics;
        [SerializeField] private ReleaseInput releaseInput;
        [SerializeField] private KeyboardChargeInput keyboardChargeInput;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private ElasticPlatform currentPlatform;
        [SerializeField, Min(0f)] private float jumpScale = 16f;
        [SerializeField, Min(0f)] private float minimumCompression = 0.01f;

        public bool IsAirborne { get; private set; }
        public bool JumpEnabled { get; private set; } = true;
        public float LatchedCompression { get; private set; }
        public Vector2 LastLaunchVelocity { get; private set; }
        public ElasticPlatform CurrentPlatform => currentPlatform;
        public float JumpScale => jumpScale;
        public event Action JumpStarted;

        private void Awake()
        {
            if (compressionController == null || jumpPhysics == null ||
                releaseInput == null || keyboardChargeInput == null ||
                playerBody == null || currentPlatform == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerJumpController)} requires CompressionController, " +
                    "JumpPhysics, ReleaseInput, KeyboardChargeInput, Rigidbody2D, " +
                    "and current platform references.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (releaseInput != null)
            {
                releaseInput.ReleasePressed += HandleReleasePressed;
            }
            if (keyboardChargeInput != null)
            {
                keyboardChargeInput.ChargeReleased += HandleReleasePressed;
                keyboardChargeInput.SetInputEnabled(JumpEnabled && !IsAirborne);
            }
        }

        private void OnDisable()
        {
            if (releaseInput != null)
            {
                releaseInput.ReleasePressed -= HandleReleasePressed;
            }
            if (keyboardChargeInput != null)
            {
                keyboardChargeInput.ChargeReleased -= HandleReleasePressed;
            }
        }

        private void HandleReleasePressed()
        {
            if (!isActiveAndEnabled || !JumpEnabled || IsAirborne)
            {
                return;
            }

            float compression = compressionController.Compression;
            if (compression <= minimumCompression)
            {
                return;
            }

            LatchedCompression = compression;
            LastLaunchVelocity = jumpPhysics.CalculateLaunchVelocity(
                LatchedCompression,
                currentPlatform.PhysicsStiffness,
                jumpScale);

            playerBody.velocity = LastLaunchVelocity;
            IsAirborne = true;
            keyboardChargeInput.SetInputEnabled(false);
            JumpStarted?.Invoke();
        }

        public void CompleteJump()
        {
            IsAirborne = false;
            keyboardChargeInput.SetInputEnabled(JumpEnabled);
        }

        public void ReturnToCurrentPlatform()
        {
            if (!IsAirborne || !JumpEnabled)
                return;

            // Keep the physical landing and current pair; reset only this attempt.
            keyboardChargeInput.ResetCharge();
            compressionController.SetCurrentAsZero();
            CompleteJump();
        }

        public void SetJumpEnabled(bool value)
        {
            JumpEnabled = value;
            keyboardChargeInput.SetInputEnabled(value && !IsAirborne);
        }

        public void SetCurrentPlatform(ElasticPlatform platform)
        {
            if (platform == null)
            {
                Debug.LogError("Cannot assign a null current platform.", this);
                return;
            }

            currentPlatform = platform;
        }

        private void OnValidate()
        {
            jumpScale = Mathf.Max(0f, jumpScale);
            minimumCompression = Mathf.Max(0f, minimumCompression);
        }
    }
}
