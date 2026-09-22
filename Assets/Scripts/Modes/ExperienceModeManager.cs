using MRKnobJump.Core;
using MRKnobJump.Platform;
using MRKnobJump.Player;
using UnityEngine;

namespace MRKnobJump.Modes
{
    [DisallowMultipleComponent]
    public sealed class ExperienceModeManager : MonoBehaviour
    {
        [SerializeField] private PlatformGenerator platformGenerator;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private PlayerJumpController jumpController;

        private readonly int[] combinations = new int[25];
        private bool active;
        public bool IsActive => active;
        public bool FormalStageStarted { get; private set; }
        public int CompletedFormalJumps { get; private set; }
        public bool ExperienceCompleted => CompletedFormalJumps == combinations.Length;

        private void Awake()
        {
            if (platformGenerator == null || tutorialManager == null || jumpController == null)
            {
                Debug.LogError("ExperienceModeManager requires generator, tutorial and player references.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (tutorialManager != null) tutorialManager.Completed += BeginFormalStage;
            if (FormalStageStarted && platformGenerator != null)
                platformGenerator.PlatformsAdvanced += HandleAdvanced;
        }

        private void OnDisable()
        {
            if (tutorialManager != null) tutorialManager.Completed -= BeginFormalStage;
            if (platformGenerator != null) platformGenerator.PlatformsAdvanced -= HandleAdvanced;
        }

        private void Start()
        {
            active = platformGenerator.SelectedMode == GameMode.Experience;
            if (!active) return;
            for (int i = 0; i < combinations.Length; i++) combinations[i] = i;
            // Fisher-Yates: every K/distance pair appears exactly once.
            for (int i = combinations.Length - 1; i > 0; i--)
            {
                int other = Random.Range(0, i + 1);
                int value = combinations[i];
                combinations[i] = combinations[other];
                combinations[other] = value;
            }
            tutorialManager.BeginTutorial();
        }

        private void BeginFormalStage()
        {
            if (!active || FormalStageStarted) return;
            FormalStageStarted = true;
            ApplyCurrentCombination();
            // Subscribe after the fifth tutorial handoff began: that event must not consume pair 1.
            platformGenerator.PlatformsAdvanced += HandleAdvanced;
        }

        private void HandleAdvanced()
        {
            if (!active || ExperienceCompleted) return;
            CompletedFormalJumps++;
            if (ExperienceCompleted)
            {
                jumpController.SetJumpEnabled(false);
                Debug.Log("ExperienceCompleted", this);
                return;
            }
            ApplyCurrentCombination();
        }

        private void ApplyCurrentCombination()
        {
            int pair = combinations[CompletedFormalJumps];
            int next = combinations[Mathf.Min(CompletedFormalJumps + 1, combinations.Length - 1)];
            var stiffness = (StiffnessLevel)(pair / 5);
            var distance = (DistanceLevel)(pair % 5);
            platformGenerator.ConfigureCurrentPair(stiffness, (StiffnessLevel)(next / 5),
                platformGenerator.GetTargetDistance(distance));
            platformGenerator.UseUniformColors();
            Debug.Log($"Experience {CompletedFormalJumps + 1}/25 - {stiffness}, Distance {platformGenerator.SelectedDistance}", this);
        }
    }
}
