using MRKnobJump.Core;
using MRKnobJump.Platform;
using UnityEngine;

namespace MRKnobJump.Modes
{
    [DisallowMultipleComponent]
    public sealed class EndlessModeManager : MonoBehaviour
    {
        [SerializeField] private PlatformGenerator platformGenerator;
        private bool active;
        public bool IsActive => active;
        private StiffnessLevel nextStiffness;
        public long CompletedJumps { get; private set; }

        private void Awake()
        {
            if (platformGenerator == null)
            {
                Debug.LogError("EndlessModeManager requires PlatformGenerator.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (platformGenerator != null) platformGenerator.PlatformsAdvanced += HandleAdvanced;
        }

        private void OnDisable()
        {
            if (platformGenerator != null) platformGenerator.PlatformsAdvanced -= HandleAdvanced;
        }

        private void Start()
        {
            active = platformGenerator.SelectedMode == GameMode.Endless;
            if (!active) return;
            nextStiffness = (StiffnessLevel)Random.Range(0, 5);
            ApplyNextCombination();
        }

        private void HandleAdvanced()
        {
            if (!active) return;
            CompletedJumps++;
            ApplyNextCombination();
        }

        private void ApplyNextCombination()
        {
            StiffnessLevel current = nextStiffness;
            nextStiffness = (StiffnessLevel)Random.Range(0, 5);
            var distance = (DistanceLevel)Random.Range(0, 5);
            platformGenerator.ConfigureCurrentPair(current, nextStiffness, platformGenerator.GetTargetDistance(distance));
            platformGenerator.UseUniformColors();
            Debug.Log($"Endless {CompletedJumps + 1} - {current}, Distance {platformGenerator.SelectedDistance}", this);
        }
    }
}
