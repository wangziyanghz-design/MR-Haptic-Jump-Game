using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MRKnobJump.Haptics
{
    [DisallowMultipleComponent]
    public sealed class HapticCalibration : MonoBehaviour
    {
        private const string DmaxUserKey = "MRKnobJump.DmaxUser";
        private const float DefaultDmaxUser = 80f;

        [SerializeField, Min(0f)] private float calibrationStart = 20f;
        [SerializeField, Min(0.01f)] private float calibrationStep = 5f;
        [SerializeField, Min(0.01f)] private float stepInterval = 1f;
        [SerializeField, Min(0f)] private float hardwareMax = 130f;

        private static bool continueToGameAfterCalibration;
        private float elapsedStepTime;

        public static bool HasCalibration => PlayerPrefs.HasKey(DmaxUserKey);
        public static float DmaxUser => PlayerPrefs.GetFloat(DmaxUserKey, DefaultDmaxUser);
        public float CurrentDamping { get; private set; }

        public event Action<float> DampingChanged;
        public event Action<float> CalibrationCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            continueToGameAfterCalibration = false;
        }

        private void Awake()
        {
            ValidateSettings();
            CurrentDamping = calibrationStart;
            elapsedStepTime = 0f;
        }

        private void Update()
        {
            AdvanceCalibration(Time.deltaTime);

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmCalibration();
            }
        }

        public void ConfirmCalibration()
        {
            float calibratedValue = Mathf.Clamp(CurrentDamping, calibrationStart, hardwareMax);
            PlayerPrefs.SetFloat(DmaxUserKey, calibratedValue);
            PlayerPrefs.Save();
            CalibrationCompleted?.Invoke(calibratedValue);
        }

        public static void ClearCalibration()
        {
            PlayerPrefs.DeleteKey(DmaxUserKey);
            PlayerPrefs.Save();
        }

        public static void BeginCalibration(bool continueToGame)
        {
            continueToGameAfterCalibration = continueToGame;
            SceneManager.LoadScene("CalibrationScene");
        }

        public static void Recalibrate()
        {
            BeginCalibration(false);
        }

        public static string ConsumeCompletionScene()
        {
            string sceneName = continueToGameAfterCalibration ? "GameScene" : "MainMenuScene";
            continueToGameAfterCalibration = false;
            return sceneName;
        }

        public static void CancelPendingGameLaunch()
        {
            continueToGameAfterCalibration = false;
        }

        private void AdvanceCalibration(float deltaTime)
        {
            if (CurrentDamping >= hardwareMax)
            {
                CurrentDamping = hardwareMax;
                return;
            }

            elapsedStepTime += Mathf.Max(0f, deltaTime);
            while (elapsedStepTime >= stepInterval && CurrentDamping < hardwareMax)
            {
                elapsedStepTime -= stepInterval;
                CurrentDamping = Mathf.Min(CurrentDamping + calibrationStep, hardwareMax);
                DampingChanged?.Invoke(CurrentDamping);
            }
        }

        private void ValidateSettings()
        {
            calibrationStart = Mathf.Max(0f, calibrationStart);
            calibrationStep = Mathf.Max(0.01f, calibrationStep);
            stepInterval = Mathf.Max(0.01f, stepInterval);
            hardwareMax = Mathf.Max(calibrationStart, hardwareMax);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}
