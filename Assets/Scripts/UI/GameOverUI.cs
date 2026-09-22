using MRKnobJump.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class GameOverUI : MonoBehaviour
    {
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text successfulJumpsText;
        [SerializeField] private Text perfectJumpsText;
        [SerializeField] private Text bestComboText;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            if (livesManager == null || scoreManager == null || panel == null || finalScoreText == null ||
                successfulJumpsText == null || perfectJumpsText == null || bestComboText == null)
            {
                Debug.LogError("GameOverUI requires LivesManager, ScoreManager, panel and statistic Text references.", this);
                enabled = false;
                return;
            }
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!IsShown && livesManager.IsGameOver) ShowGameOver();
        }

        private void ShowGameOver()
        {
            IsShown = true;
            finalScoreText.text = $"Final Score  {scoreManager.Score}";
            successfulJumpsText.text = $"Successful Jumps  {scoreManager.SuccessfulJumps}";
            perfectJumpsText.text = $"Perfect Jumps  {scoreManager.PerfectJumps}";
            bestComboText.text = $"Best Combo  x{scoreManager.BestCombo}";
            panel.SetActive(true);
        }

        public void Restart() => SceneManager.LoadScene("GameScene");

        public void BackToMainMenu() => SceneManager.LoadScene("MainMenuScene");
    }
}
