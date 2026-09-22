using MRKnobJump.Haptics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class CalibrationUI : MonoBehaviour
    {
        [SerializeField] private HapticCalibration calibration;
        [SerializeField] private Text currentResistanceText;

        private void Awake()
        {
            if (calibration == null || currentResistanceText == null)
            {
                Debug.LogError(
                    $"{nameof(CalibrationUI)} requires HapticCalibration and resistance Text references.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (calibration == null) return;
            calibration.DampingChanged += HandleDampingChanged;
            calibration.CalibrationCompleted += HandleCalibrationCompleted;
        }

        private void Start()
        {
            if (calibration != null)
            {
                HandleDampingChanged(calibration.CurrentDamping);
            }
        }

        private void OnDisable()
        {
            if (calibration == null) return;
            calibration.DampingChanged -= HandleDampingChanged;
            calibration.CalibrationCompleted -= HandleCalibrationCompleted;
        }

        public void CancelAndBack()
        {
            HapticCalibration.CancelPendingGameLaunch();
            SceneManager.LoadScene("MainMenuScene");
        }

        private void HandleDampingChanged(float damping)
        {
            currentResistanceText.text = $"Current Resistance: {damping:0}";
        }

        private void HandleCalibrationCompleted(float damping)
        {
            SceneManager.LoadScene(HapticCalibration.ConsumeCompletionScene());
        }
    }
}
