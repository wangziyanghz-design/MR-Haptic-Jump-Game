using MRKnobJump.Gameplay;
using MRKnobJump.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class LandingFeedbackUI : MonoBehaviour
    {
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private Transform player;
        [SerializeField] private CanvasGroup feedbackGroup;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Image screenFlash;
        [SerializeField] private ParticleSystem perfectParticles;
        [SerializeField, Min(0.1f)] private float displayDuration = 0.9f;

        private float remainingTime;
        private float initialTextScale = 1f;
        private float flashStrength;
        private Color flashColor = Color.white;

        public bool IsShowing => remainingTime > 0f;
        public LandingResult LastResult { get; private set; }

        private void Awake()
        {
            if (landingDetector == null || player == null || feedbackGroup == null ||
                feedbackText == null || screenFlash == null || perfectParticles == null)
            {
                Debug.LogError("LandingFeedbackUI requires landing detector, player, UI and particle references.", this);
                enabled = false;
                return;
            }

            HideImmediately();
            perfectParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnEnable()
        {
            if (landingDetector != null) landingDetector.LandingResolved += HandleLanding;
        }

        private void OnDisable()
        {
            if (landingDetector != null) landingDetector.LandingResolved -= HandleLanding;
            HideImmediately();
        }

        private void HandleLanding(LandingResult result)
        {
            LastResult = result;
            remainingTime = displayDuration;
            feedbackGroup.alpha = 1f;

            switch (result)
            {
                case LandingResult.Perfect:
                    feedbackText.text = "PERFECT!";
                    feedbackText.color = new Color(1f, 0.84f, 0.2f);
                    feedbackText.fontSize = 58;
                    initialTextScale = 1.35f;
                    flashStrength = 0.2f;
                    flashColor = new Color(1f, 0.82f, 0.25f);
                    perfectParticles.transform.position = player.position;
                    perfectParticles.Play(true);
                    break;
                case LandingResult.Success:
                    feedbackText.text = "NICE LANDING";
                    feedbackText.color = new Color(0.45f, 0.95f, 1f);
                    feedbackText.fontSize = 42;
                    initialTextScale = 1.12f;
                    flashStrength = 0.08f;
                    flashColor = new Color(0.35f, 0.9f, 1f);
                    break;
                default:
                    feedbackText.text = "MISSED!";
                    feedbackText.color = new Color(1f, 0.34f, 0.4f);
                    feedbackText.fontSize = 44;
                    initialTextScale = 1.08f;
                    flashStrength = 0.1f;
                    flashColor = new Color(1f, 0.2f, 0.28f);
                    break;
            }

            feedbackText.rectTransform.localScale = Vector3.one * initialTextScale;
            SetFlashAlpha(flashStrength);
        }

        private void Update()
        {
            if (remainingTime <= 0f) return;

            remainingTime = Mathf.Max(0f, remainingTime - Time.unscaledDeltaTime);
            float normalized = displayDuration > 0f ? remainingTime / displayDuration : 0f;
            feedbackGroup.alpha = Mathf.Clamp01(normalized * 2.2f);
            feedbackText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, initialTextScale, normalized);
            SetFlashAlpha(flashStrength * Mathf.Clamp01(normalized * 3f - 2f));

            if (remainingTime <= 0f) HideImmediately();
        }

        private void SetFlashAlpha(float alpha)
        {
            screenFlash.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
        }

        private void HideImmediately()
        {
            remainingTime = 0f;
            if (feedbackGroup != null) feedbackGroup.alpha = 0f;
            if (screenFlash != null) SetFlashAlpha(0f);
        }

        private void OnValidate()
        {
            displayDuration = Mathf.Max(0.1f, displayDuration);
        }
    }
}
