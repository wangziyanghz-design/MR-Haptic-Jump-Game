using MRKnobJump.Core;
using MRKnobJump.Gameplay;
using MRKnobJump.Modes;
using UnityEngine;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private ExperienceModeManager experienceModeManager;
        [SerializeField] private EndlessModeManager endlessModeManager;

        [Header("Views")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text progressText;

        public GameMode CurrentMode => endlessModeManager.IsActive ? GameMode.Endless : GameMode.Experience;

        private void Awake()
        {
            if (scoreManager == null || livesManager == null || tutorialManager == null ||
                experienceModeManager == null || endlessModeManager == null || scoreText == null ||
                livesText == null || comboText == null || modeText == null || progressText == null)
            {
                Debug.LogError("GameHUD requires all data and Text references.", this);
                enabled = false;
            }
        }

        private void Start() => Refresh();

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            SetText(scoreText, $"Score  {scoreManager.Score}");
            SetText(livesText, "Lives  " + Hearts(livesManager.CurrentLives));

            bool showCombo = scoreManager.PerfectCombo > 0;
            comboText.gameObject.SetActive(showCombo);
            if (showCombo) SetText(comboText, $"Perfect Combo  x{scoreManager.PerfectCombo}");

            GameMode mode = CurrentMode;
            SetText(modeText, mode == GameMode.Experience ? "EXPERIENCE" : "ENDLESS");
            if (mode == GameMode.Endless)
            {
                SetText(progressText, $"Successful Jumps  {endlessModeManager.CompletedJumps}");
            }
            else if (!experienceModeManager.FormalStageStarted)
            {
                SetText(progressText, $"Tutorial  {tutorialManager.CurrentTutorialStep}/5");
            }
            else
            {
                SetText(progressText, $"Experience  {experienceModeManager.CompletedFormalJumps}/25");
            }
        }

        private static string Hearts(int lives)
        {
            if (lives >= 3) return "♥  ♥  ♥";
            if (lives == 2) return "♥  ♥";
            if (lives == 1) return "♥";
            return string.Empty;
        }

        private static void SetText(Text view, string value)
        {
            if (view.text != value) view.text = value;
        }
    }
}
