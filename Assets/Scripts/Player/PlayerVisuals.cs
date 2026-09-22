using MRKnobJump.Gameplay;
using UnityEngine;

namespace MRKnobJump.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float poseSpeed = 14f;
        [SerializeField, Range(0f, 25f)] private float airborneTilt = 12f;
        [SerializeField, Min(0f)] private float launchPoseDuration = 0.16f;
        [SerializeField, Min(0f)] private float landingPoseDuration = 0.24f;

        private Vector3 restScale;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Color restColor;
        private float launchTimer;
        private float landingTimer;
        private Color feedbackColor = Color.white;

        private void Awake()
        {
            if (jumpController == null || landingDetector == null || playerBody == null ||
                visualRoot == null || bodyRenderer == null)
            {
                Debug.LogError("PlayerVisuals requires jump, landing, Rigidbody2D, visual root and body renderer references.", this);
                enabled = false;
                return;
            }

            restScale = visualRoot.localScale;
            restPosition = visualRoot.localPosition;
            restRotation = visualRoot.localRotation;
            restColor = bodyRenderer.color;
        }

        private void OnEnable()
        {
            if (jumpController != null) jumpController.JumpStarted += HandleJumpStarted;
            if (landingDetector != null) landingDetector.LandingResolved += HandleLanding;
        }

        private void OnDisable()
        {
            if (jumpController != null) jumpController.JumpStarted -= HandleJumpStarted;
            if (landingDetector != null) landingDetector.LandingResolved -= HandleLanding;

            if (visualRoot != null)
            {
                visualRoot.localScale = restScale;
                visualRoot.localPosition = restPosition;
                visualRoot.localRotation = restRotation;
            }
            if (bodyRenderer != null) bodyRenderer.color = restColor;
        }

        private void HandleJumpStarted()
        {
            launchTimer = launchPoseDuration;
            landingTimer = 0f;
        }

        private void HandleLanding(LandingResult result)
        {
            landingTimer = landingPoseDuration;
            launchTimer = 0f;
            feedbackColor = result == LandingResult.Perfect
                ? new Color(1f, 0.86f, 0.24f)
                : result == LandingResult.Success
                    ? new Color(0.35f, 0.95f, 1f)
                    : new Color(1f, 0.32f, 0.38f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            launchTimer = Mathf.Max(0f, launchTimer - dt);
            landingTimer = Mathf.Max(0f, landingTimer - dt);

            Vector3 targetScale = restScale;
            Vector3 targetPosition = restPosition;
            float targetAngle = 0f;

            if (launchTimer > 0f)
            {
                float phase = launchPoseDuration > 0f ? launchTimer / launchPoseDuration : 0f;
                float stretch = Mathf.Sin(phase * Mathf.PI);
                targetScale = Vector3.Scale(restScale, new Vector3(0.78f, 1.24f, 1f));
                targetPosition += Vector3.up * (0.06f * stretch);
            }
            else if (landingTimer > 0f)
            {
                float phase = landingPoseDuration > 0f ? landingTimer / landingPoseDuration : 0f;
                float squash = Mathf.Sin(phase * Mathf.PI);
                targetScale = Vector3.Scale(restScale, new Vector3(1f + 0.22f * squash, 1f - 0.2f * squash, 1f));
                targetPosition += Vector3.down * (0.06f * squash);
            }
            else if (jumpController.IsAirborne)
            {
                float vertical = Mathf.Clamp(playerBody.velocity.y / 8f, -1f, 1f);
                float stretch = Mathf.Abs(vertical);
                targetScale = Vector3.Scale(restScale, new Vector3(1f - 0.12f * stretch, 1f + 0.16f * stretch, 1f));
                targetAngle = -vertical * airborneTilt;
            }
            else
            {
                float breathe = Mathf.Sin(Time.time * 3.2f) * 0.018f;
                targetScale = Vector3.Scale(restScale, new Vector3(1f - breathe, 1f + breathe, 1f));
            }

            float blend = 1f - Mathf.Exp(-poseSpeed * dt);
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, blend);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, blend);
            visualRoot.localRotation = Quaternion.Lerp(
                visualRoot.localRotation,
                restRotation * Quaternion.Euler(0f, 0f, targetAngle),
                blend);

            Color targetColor = landingTimer > 0f ? feedbackColor : restColor;
            bodyRenderer.color = Color.Lerp(bodyRenderer.color, targetColor, blend);
        }

        private void OnValidate()
        {
            poseSpeed = Mathf.Max(0f, poseSpeed);
            launchPoseDuration = Mathf.Max(0f, launchPoseDuration);
            landingPoseDuration = Mathf.Max(0f, landingPoseDuration);
        }
    }
}
