using MRKnobJump.Player;
using MRKnobJump.Gameplay;
using UnityEngine;

namespace MRKnobJump.Platform
{
    public enum DistanceLevel
    {
        Distance1,
        Distance2,
        Distance3,
        Distance4,
        Distance5
    }

    [DisallowMultipleComponent]
    public sealed class PlatformGenerator : MonoBehaviour
    {
        private const int LevelCount = 5;
        [Header("Mode (select before Play)")]
        [SerializeField] private MRKnobJump.Core.GameMode gameMode = MRKnobJump.Core.GameMode.Experience;
        public MRKnobJump.Core.GameMode SelectedMode => gameMode;

        [Header("Platform Slots")]
        [SerializeField] private ElasticPlatform currentPlatform;
        [SerializeField] private ElasticPlatform targetPlatform;
        [SerializeField] private PlayerJumpController playerJumpController;
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private PlayerRespawn playerRespawn;
        [SerializeField] private CompressionController compressionController;

        private bool awaitingLanding;
        private Rigidbody2D playerBody;
        private float? distanceOverride;
        private StiffnessLevel controlledCurrentStiffness;
        private StiffnessLevel controlledNextStiffness;
        private float controlledDistance;
        private bool hasControlledPair;
        private StiffnessLevel? debugStiffnessOverride;
        private float? debugDistanceOverride;
        public event System.Action PlatformsAdvanced;

        [Header("Manual Selection")]
        [SerializeField] private StiffnessLevel stiffnessLevel = StiffnessLevel.K1;
        [SerializeField] private DistanceLevel distanceLevel = DistanceLevel.Distance1;

        [Header("Adjustable Values")]
        [SerializeField] private float[] stiffnessValues = { 1f, 1.25f, 1.5f, 1.75f, 2f };
        [SerializeField] private float[] targetDistances = { 3f, 4f, 5f, 6f, 7f };
        [SerializeField, Min(0.1f)] private float platformWidth = 2.5f;

        public ElasticPlatform CurrentPlatform => currentPlatform;
        public ElasticPlatform TargetPlatform => targetPlatform;
        public StiffnessLevel SelectedStiffnessLevel => stiffnessLevel;
        public DistanceLevel SelectedDistanceLevel => distanceLevel;
        public float SelectedDistance => debugDistanceOverride ??
            (distanceOverride ?? GetSelectedValue(targetDistances, distanceLevel));
        public bool DebugOverrideActive => debugStiffnessOverride.HasValue || debugDistanceOverride.HasValue;

        public float GetPhysicsStiffness(StiffnessLevel level)
        {
            return GetSelectedValue(stiffnessValues, level);
        }

        public float GetTargetDistance(DistanceLevel level)
        {
            return GetSelectedValue(targetDistances, level);
        }

        private void Awake()
        {
            gameMode = MRKnobJump.Core.GameSettings.SelectedGameMode;
            if (!HasValidConfiguration())
            {
                enabled = false;
                return;
            }

            GeneratePlatforms();
            playerBody = playerJumpController.GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            if (landingDetector != null) landingDetector.LandingResolved += HandleLanding;
            if (playerJumpController != null) playerJumpController.JumpStarted += HandleJumpStarted;
        }

        private void OnDisable()
        {
            if (landingDetector != null) landingDetector.LandingResolved -= HandleLanding;
            if (playerJumpController != null) playerJumpController.JumpStarted -= HandleJumpStarted;
        }

        private void HandleJumpStarted() => awaitingLanding = true;

        private void HandleLanding(LandingResult result)
        {
            if (!awaitingLanding) return;
            awaitingLanding = false;
            if (result == LandingResult.Fail) return;

            // Two scene slots rotate roles; no unbounded platform accumulation.
            ElasticPlatform recycled = currentPlatform;
            var recycledCompressor = recycled.GetComponent<PlatformCompressor>();
            recycledCompressor.enabled = false;
            recycledCompressor.RestoreRestPose();

            currentPlatform = targetPlatform;
            targetPlatform = recycled;
            compressionController.SetCurrentAsZero();
            var currentCompressor = currentPlatform.GetComponent<PlatformCompressor>();
            currentCompressor.CaptureRestPose();
            currentCompressor.enabled = true;
            playerJumpController.SetCurrentPlatform(currentPlatform);
            playerBody.velocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerRespawn.CaptureCurrentPlatformSpawn();

            targetPlatform.transform.position = currentPlatform.transform.position +
                Vector3.right * SelectedDistance;
            SetPlatformWidth(targetPlatform.transform);
            StiffnessLevel generatedStiffness = debugStiffnessOverride ?? stiffnessLevel;
            targetPlatform.Configure(generatedStiffness, GetPhysicsStiffness(generatedStiffness));
            targetPlatform.GetComponent<SpriteRenderer>().color = Color.white;
            recycledCompressor.CaptureRestPose();
            landingDetector.SetTargetPlatform(targetPlatform);
            playerJumpController.CompleteJump();

            if (hasControlledPair)
            {
                controlledCurrentStiffness = controlledNextStiffness;
                controlledNextStiffness = stiffnessLevel;
                controlledDistance = distanceOverride ?? GetSelectedValue(targetDistances, distanceLevel);
            }

            PlatformsAdvanced?.Invoke();
        }

        public void ConfigureCurrentPair(StiffnessLevel currentLevel, StiffnessLevel nextLevel, float distance)
        {
            distanceOverride = Mathf.Max(0.1f, distance);
            controlledCurrentStiffness = currentLevel;
            controlledNextStiffness = nextLevel;
            controlledDistance = distanceOverride.Value;
            hasControlledPair = true;
            ApplyEffectivePair();
        }

        public void SetDebugStiffnessOverride(StiffnessLevel level)
        {
            EnsureControlledPair();
            debugStiffnessOverride = level;
            ApplyEffectivePair();
        }

        public void SetDebugDistanceOverride(float distance)
        {
            EnsureControlledPair();
            debugDistanceOverride = Mathf.Max(0.1f, distance);
            ApplyEffectivePair();
        }

        public void ClearDebugOverride()
        {
            if (!DebugOverrideActive)
            {
                return;
            }

            debugStiffnessOverride = null;
            debugDistanceOverride = null;
            ApplyEffectivePair();
        }

        private void ApplyEffectivePair()
        {
            if (!hasControlledPair)
            {
                return;
            }

            StiffnessLevel currentLevel = debugStiffnessOverride ?? controlledCurrentStiffness;
            StiffnessLevel nextLevel = debugStiffnessOverride ?? controlledNextStiffness;
            float distance = debugDistanceOverride ?? controlledDistance;

            currentPlatform.Configure(currentLevel, GetPhysicsStiffness(currentLevel));
            targetPlatform.Configure(nextLevel, GetPhysicsStiffness(nextLevel));
            var compressor = targetPlatform.GetComponent<PlatformCompressor>();
            compressor.RestoreRestPose();
            targetPlatform.transform.position = currentPlatform.transform.position + Vector3.right * distance;
            compressor.CaptureRestPose();
            playerJumpController.SetCurrentPlatform(currentPlatform);
            landingDetector.SetTargetPlatform(targetPlatform);
        }

        private void EnsureControlledPair()
        {
            if (hasControlledPair)
            {
                return;
            }

            controlledCurrentStiffness = currentPlatform.StiffnessLevel;
            controlledNextStiffness = targetPlatform.StiffnessLevel;
            controlledDistance = distanceOverride ?? GetSelectedValue(targetDistances, distanceLevel);
            hasControlledPair = true;
        }

        public void ClearDistanceOverride()
        {
            distanceOverride = null;
            EnsureControlledPair();
            controlledDistance = GetSelectedValue(targetDistances, distanceLevel);
            ApplyEffectivePair();
        }

        public void UseUniformColors()
        {
            currentPlatform.GetComponent<SpriteRenderer>().color = Color.white;
            targetPlatform.GetComponent<SpriteRenderer>().color = Color.white;
        }

        public void GeneratePlatforms()
        {
            if (!HasValidConfiguration())
            {
                return;
            }

            float physicsStiffness = GetSelectedValue(stiffnessValues, stiffnessLevel);
            float distance = GetSelectedValue(targetDistances, distanceLevel);

            controlledCurrentStiffness = stiffnessLevel;
            controlledNextStiffness = stiffnessLevel;
            controlledDistance = distance;
            hasControlledPair = true;

            currentPlatform.Configure(stiffnessLevel, physicsStiffness);
            targetPlatform.Configure(stiffnessLevel, physicsStiffness);
            SetPlatformWidth(currentPlatform.transform);
            SetPlatformWidth(targetPlatform.transform);

            Vector3 currentPosition = currentPlatform.transform.position;
            targetPlatform.transform.position = new Vector3(
                currentPosition.x + distance,
                currentPosition.y,
                currentPosition.z);

            playerJumpController.SetCurrentPlatform(currentPlatform);
        }

        public void SetManualSelection(StiffnessLevel stiffness, DistanceLevel distance)
        {
            stiffnessLevel = stiffness;
            distanceLevel = distance;
            GeneratePlatforms();
        }

        private bool HasValidConfiguration()
        {
            if (currentPlatform == null || targetPlatform == null || playerJumpController == null ||
                landingDetector == null || playerRespawn == null || compressionController == null ||
                currentPlatform.GetComponent<PlatformCompressor>() == null ||
                targetPlatform.GetComponent<PlatformCompressor>() == null)
            {
                Debug.LogError(
                    $"{nameof(PlatformGenerator)} requires current platform, target platform, " +
                    "player, landing, respawn, compression references and both platform compressors.",
                    this);
                return false;
            }

            if (!HasFivePositiveValues(stiffnessValues) || !HasFivePositiveValues(targetDistances))
            {
                Debug.LogError(
                    $"{nameof(PlatformGenerator)} requires exactly five positive stiffness " +
                    "values and five positive target distances.",
                    this);
                return false;
            }

            return true;
        }

        private static bool HasFivePositiveValues(float[] values)
        {
            if (values == null || values.Length != LevelCount)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0f)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetSelectedValue<TLevel>(float[] values, TLevel level)
            where TLevel : System.Enum
        {
            return values[System.Convert.ToInt32(level)];
        }

        private void SetPlatformWidth(Transform platformTransform)
        {
            Vector3 scale = platformTransform.localScale;
            scale.x = platformWidth;
            platformTransform.localScale = scale;
        }

        private void OnValidate()
        {
            platformWidth = Mathf.Max(0.1f, platformWidth);

            // During play, changed selections apply to the next generated target, not this attempt.
            if (Application.isPlaying) return;

            if (currentPlatform != null && targetPlatform != null &&
                playerJumpController != null && landingDetector != null &&
                playerRespawn != null && compressionController != null &&
                currentPlatform.GetComponent<PlatformCompressor>() != null &&
                targetPlatform.GetComponent<PlatformCompressor>() != null &&
                HasFivePositiveValues(stiffnessValues) &&
                HasFivePositiveValues(targetDistances))
            {
                GeneratePlatforms();
            }
        }
    }
}
