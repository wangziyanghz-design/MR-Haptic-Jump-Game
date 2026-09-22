using MRKnobJump.Core;
using MRKnobJump.Gameplay;
using MRKnobJump.Player;
using UnityEngine;

namespace MRKnobJump.Haptics
{
    [DisallowMultipleComponent]
    public sealed class HapticController : MonoBehaviour
    {
        [SerializeField] private HapticSettings settings = new HapticSettings();
        [SerializeField] private CompressionController compressionController;
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private LivesManager livesManager;

        public float CurrentDamping { get; private set; }
        public HapticSettings Settings => settings;
        public float EffectiveDmaxUser => Mathf.Max(
            settings.DMin,
            HapticCalibration.HasCalibration ? HapticCalibration.DmaxUser : settings.DMaxUser);

        private void Awake()
        {
            if (settings == null || compressionController == null ||
                jumpController == null || livesManager == null)
            {
                Debug.LogError(
                    $"{nameof(HapticController)} requires HapticSettings, CompressionController, " +
                    "PlayerJumpController, and LivesManager references.",
                    this);
                enabled = false;
                return;
            }

            settings.Validate();
            CurrentDamping = settings.DMin;
        }

        private void LateUpdate()
        {
            CurrentDamping = CalculateDamping();
        }

        private float CalculateDamping()
        {
            if (GameSettings.SelectedInputMode != InputMode.Knob ||
                !GameSettings.HapticFeedbackEnabled ||
                livesManager.IsGameOver ||
                !jumpController.JumpEnabled)
            {
                return settings.DMin;
            }

            if (jumpController.IsAirborne)
            {
                return EffectiveDmaxUser;
            }

            float dMaxUser = EffectiveDmaxUser;
            float compression = Mathf.Clamp01(compressionController.Compression);
            float hapticStiffness = settings.GetHapticStiffness(
                jumpController.CurrentPlatform.StiffnessLevel);
            float damping = settings.DMin +
                (dMaxUser - settings.DMin) * hapticStiffness * compression;
            return Mathf.Clamp(damping, settings.DMin, dMaxUser);
        }

        private void OnValidate()
        {
            if (settings == null)
            {
                settings = new HapticSettings();
            }

            settings.Validate();
        }
    }
}
