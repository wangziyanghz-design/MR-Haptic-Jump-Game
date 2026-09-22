using MRKnobJump.Player;
using UnityEngine;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LivesManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int startingLives = 3;
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private PlayerRespawn playerRespawn;

        private bool awaitingResult;
        public int CurrentLives { get; private set; }
        public bool IsGameOver => CurrentLives == 0;

        private void Awake()
        {
            if (landingDetector == null || jumpController == null || playerRespawn == null)
            {
                Debug.LogError("LivesManager requires landing detector, jump controller and respawn references.", this);
                enabled = false;
                return;
            }
            ResetLives();
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
        }

        private void HandleJumpStarted() => awaitingResult = true;

        private void HandleLanding(LandingResult result)
        {
            if (!awaitingResult || IsGameOver) return;
            awaitingResult = false;
            if (result != LandingResult.Fail) return;

            CurrentLives--;
            Debug.Log($"LIVES: {CurrentLives}", this);
            if (IsGameOver)
            {
                jumpController.SetJumpEnabled(false);
                Debug.Log("GAME OVER", this);
            }
            else
            {
                playerRespawn.Respawn();
            }
        }

        // Resets the life budget and its lock; placement is owned by PlayerRespawn.
        public void ResetLives()
        {
            CurrentLives = Mathf.Max(1, startingLives);
            awaitingResult = jumpController != null && jumpController.IsAirborne;
            if (jumpController != null) jumpController.SetJumpEnabled(true);
            Debug.Log($"LIVES: {CurrentLives}", this);
        }

        private void OnValidate() => startingLives = Mathf.Max(1, startingLives);
    }
}
