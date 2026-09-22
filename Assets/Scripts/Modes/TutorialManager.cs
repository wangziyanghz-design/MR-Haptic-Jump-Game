using MRKnobJump.Platform;
using UnityEngine;

namespace MRKnobJump.Modes
{
    [DisallowMultipleComponent]
    public sealed class TutorialManager : MonoBehaviour
    {
        [SerializeField] private PlatformGenerator platformGenerator;

        private static readonly StiffnessLevel[] Sequence =
        {
            StiffnessLevel.K1, StiffnessLevel.K5, StiffnessLevel.K2,
            StiffnessLevel.K3, StiffnessLevel.K4
        };
        // Indexed by K1 through K5, independent of the learning order.
        private static readonly Color[] Colors =
        {
            new Color(0.2f, 0.9f, 0.35f), new Color(0.15f, 0.75f, 1f),
            new Color(1f, 0.85f, 0.1f), new Color(0.8f, 0.3f, 1f),
            new Color(1f, 0.2f, 0.2f)
        };
        private static readonly string[] Labels = { "Soft", "Medium Soft", "Medium", "Medium Hard", "Hard" };
        private int completedJumps;
        private bool started;
        public event System.Action Completed;
        public bool TutorialCompleted => completedJumps >= Sequence.Length;
        public int CurrentTutorialStep => Mathf.Min(completedJumps + 1, Sequence.Length);

        private void Awake()
        {
            if (platformGenerator == null)
            {
                Debug.LogError("TutorialManager requires PlatformGenerator.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (platformGenerator != null) platformGenerator.PlatformsAdvanced += HandlePlatformsAdvanced;
        }

        private void OnDisable()
        {
            if (platformGenerator != null) platformGenerator.PlatformsAdvanced -= HandlePlatformsAdvanced;
        }

        public void BeginTutorial()
        {
            if (started || !isActiveAndEnabled) return;
            started = true;
            ApplyStep();
        }

        private void HandlePlatformsAdvanced()
        {
            // Generator emits only once after a successful landing and completed handoff.
            if (!started || TutorialCompleted) return;
            completedJumps++;
            if (TutorialCompleted)
            {
                platformGenerator.ClearDistanceOverride();
                SetColor(platformGenerator.CurrentPlatform, Color.white);
                SetColor(platformGenerator.TargetPlatform, Color.white);
                Debug.Log("TutorialCompleted", this);
                Completed?.Invoke();
                return;
            }
            ApplyStep();
        }

        private void ApplyStep()
        {
            StiffnessLevel current = Sequence[completedJumps];
            StiffnessLevel next = completedJumps + 1 < Sequence.Length
                ? Sequence[completedJumps + 1] : platformGenerator.SelectedStiffnessLevel;
            platformGenerator.ConfigureCurrentPair(current, next, 4f);
            SetColor(platformGenerator.CurrentPlatform, Colors[(int)current]);
            SetColor(platformGenerator.TargetPlatform, completedJumps + 1 < Sequence.Length
                ? Colors[(int)next] : Color.white);
            Debug.Log($"Tutorial {CurrentTutorialStep}/5 - {Labels[(int)current]}", this);
        }

        private static void SetColor(ElasticPlatform platform, Color color)
        {
            platform.GetComponent<SpriteRenderer>().color = color;
        }
    }
}
