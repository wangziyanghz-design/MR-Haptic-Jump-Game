using System;
using MRKnobJump.Gameplay;
using MRKnobJump.Platform;
using UnityEngine;

namespace MRKnobJump.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLandingDetector : MonoBehaviour
    {
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private ElasticPlatform targetPlatform;
        [SerializeField] private LandingEvaluator landingEvaluator;
        [SerializeField] private float failYThreshold = -5f;

        private Collider2D targetCollider;
        private Collider2D launchPlatformCollider;
        private bool launchStepPending;
        private bool returnArmed;
        public bool HasResult { get; private set; }
        public bool HasReturned { get; private set; }
        public LandingResult LastResult { get; private set; }
        public event Action<LandingResult> LandingResolved;

        private void Awake()
        {
            if (targetPlatform != null)
                targetCollider = targetPlatform.GetComponent<BoxCollider2D>();

            if (jumpController == null || playerBody == null ||
                targetCollider == null || landingEvaluator == null)
            {
                Debug.LogError("PlayerLandingDetector requires jump controller, body, target platform and evaluator references.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (jumpController != null)
                jumpController.JumpStarted += HandleJumpStarted;
        }

        private void OnDisable()
        {
            if (jumpController != null)
                jumpController.JumpStarted -= HandleJumpStarted;
        }

        private void HandleJumpStarted()
        {
            HasResult = false;
            HasReturned = false;
            launchPlatformCollider = jumpController.CurrentPlatform.GetComponent<Collider2D>();
            launchStepPending = true;
            returnArmed = false;
        }

        private void FixedUpdate()
        {
            if (!jumpController.IsAirborne)
                return;

            // Never accept the supporting contact carried over from the launch step.
            if (launchStepPending)
            {
                launchStepPending = false;
                return;
            }

            // Read before the solver: a landing contact may already have zeroed Y speed.
            // Very small hops may never produce an Exit callback, so separation alone
            // cannot be required. A later non-rising step plus top support is sufficient.
            if (playerBody.velocity.y <= 0f)
                returnArmed = true;
        }

        public void SetTargetPlatform(ElasticPlatform platform)
        {
            if (platform == null)
            {
                Debug.LogError("Cannot assign a null landing target.", this);
                return;
            }
            targetPlatform = platform;
            targetCollider = platform.GetComponent<BoxCollider2D>();
            // Keep the result latch until the next actual jump starts.
        }

        public bool ForceFail()
        {
            if (!isActiveAndEnabled || HasResult || !jumpController.IsAirborne)
            {
                return false;
            }

            Resolve(LandingResult.Fail);
            return true;
        }

        private void Update()
        {
            if (!HasResult && jumpController.IsAirborne && playerBody.position.y < failYThreshold)
                Resolve(LandingResult.Fail);
        }

        private void OnCollisionEnter2D(Collision2D collision) => CheckLanding(collision);
        private void OnCollisionStay2D(Collision2D collision) => CheckLanding(collision);

        private void CheckLanding(Collision2D collision)
        {
            if (!enabled || HasResult || !jumpController.IsAirborne ||
                playerBody.velocity.y > 0.1f)
                return;

            bool isTarget = collision.collider == targetCollider;
            bool isReturn = collision.collider == launchPlatformCollider && returnArmed;
            if (!isTarget && !isReturn)
                return;

            // Only a top-facing supporting contact is a landing, not a side or underside hit.
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y <= 0.5f)
                    continue;

                if (isTarget)
                {
                    Bounds bounds = targetCollider.bounds;
                    Resolve(landingEvaluator.Evaluate(playerBody.position.x, bounds.center.x, bounds.size.x));
                }
                else
                {
                    HasReturned = true;
                    jumpController.ReturnToCurrentPlatform();
                    // Deliberately not a LandingResolved result: no score, life or
                    // progression subscriber should process this neutral retry.
                    Debug.Log("RETURN / SHORT JUMP", this);
                }
                return;
            }
        }

        private void Resolve(LandingResult result)
        {
            HasResult = true;
            LastResult = result;
            jumpController.CompleteJump();
            Debug.Log(result == LandingResult.Perfect ? "PERFECT" :
                result == LandingResult.Success ? "SUCCESS" : "FAIL", this);
            LandingResolved?.Invoke(result);
        }
    }
}
