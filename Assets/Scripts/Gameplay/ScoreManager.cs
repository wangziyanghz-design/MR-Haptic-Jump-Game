using MRKnobJump.Player;
using UnityEngine;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ScoreManager : MonoBehaviour
    {
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private LivesManager livesManager;

        private bool awaitingResult;
        public long Score { get; private set; }
        public int PerfectCombo { get; private set; }
        public int BestCombo { get; private set; }
        // Includes both Success and Perfect landings.
        public long SuccessfulJumps { get; private set; }
        public long PerfectJumps { get; private set; }

        private void Awake()
        {
            if (landingDetector == null || jumpController == null || livesManager == null)
            {
                Debug.LogError("ScoreManager requires landing detector, jump controller and lives references.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (landingDetector != null) landingDetector.LandingResolved += HandleLanding;
            if (jumpController != null) jumpController.JumpStarted += HandleJumpStarted;
        }

        private void OnDisable()
        {
            if (landingDetector != null) landingDetector.LandingResolved -= HandleLanding;
            if (jumpController != null) jumpController.JumpStarted -= HandleJumpStarted;
            awaitingResult = false;
        }

        private void HandleJumpStarted() => awaitingResult = !livesManager.IsGameOver;

        private void HandleLanding(LandingResult result)
        {
            if (!awaitingResult) return;
            awaitingResult = false;

            // The last Fail must break the combo even if LivesManager ran first.
            if (result == LandingResult.Fail || livesManager.IsGameOver)
            {
                PerfectCombo = 0;
                Debug.Log($"SCORE: {Score} | Perfect Combo: x{PerfectCombo}", this);
                return;
            }

            if (result == LandingResult.Perfect)
            {
                PerfectCombo = Mathf.Min(PerfectCombo + 1, 5);
                BestCombo = Mathf.Max(BestCombo, PerfectCombo);
                PerfectJumps++;
                Score += 200L * PerfectCombo;
            }
            else if (result == LandingResult.Success)
            {
                PerfectCombo = 0;
                Score += 100;
            }
            else return;

            SuccessfulJumps++;
            Debug.Log($"SCORE: {Score} | Perfect Combo: x{PerfectCombo} | Best: x{BestCombo}", this);
        }
    }
}
