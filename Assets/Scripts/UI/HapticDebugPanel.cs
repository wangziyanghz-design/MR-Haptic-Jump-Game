using System.Text;
using MRKnobJump.Core;
using MRKnobJump.Gameplay;
using MRKnobJump.Haptics;
using MRKnobJump.Input;
using MRKnobJump.Platform;
using MRKnobJump.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class HapticDebugPanel : MonoBehaviour
    {
        private const double CountsPerRevolution = 8000d;

        [Header("Data")]
        [SerializeField] private EncoderUnwrapper encoderUnwrapper;
        [SerializeField] private UdpKnobInputReceiver udpInputReceiver;
        [SerializeField] private CompressionController compressionController;
        [SerializeField] private PlatformGenerator platformGenerator;
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private PlayerLandingDetector landingDetector;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private HapticController hapticController;
        [SerializeField] private UdpHapticOutput udpHapticOutput;

        [Header("View")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private Text overrideText;
        [SerializeField] private bool startsVisible;

        private readonly StringBuilder statusBuilder = new StringBuilder(512);

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (encoderUnwrapper == null || udpInputReceiver == null || compressionController == null ||
                platformGenerator == null || jumpController == null || landingDetector == null ||
                livesManager == null || hapticController == null || udpHapticOutput == null || panelRoot == null ||
                statusText == null || overrideText == null)
            {
                Debug.LogError($"{nameof(HapticDebugPanel)} requires all data and view references.", this);
                enabled = false;
                return;
            }

            SetPanelVisible(startsVisible);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                SetPanelVisible(!panelRoot.activeSelf);
            }
        }

        private void LateUpdate()
        {
            if (panelRoot.activeSelf)
            {
                Refresh();
            }
        }

        public void SetPanelVisible(bool visible)
        {
            if (!visible)
            {
                platformGenerator.ClearDebugOverride();
            }

            panelRoot.SetActive(visible);
            if (visible)
            {
                Refresh();
            }
        }

        public void SetK1() => SetStiffness(StiffnessLevel.K1);
        public void SetK2() => SetStiffness(StiffnessLevel.K2);
        public void SetK3() => SetStiffness(StiffnessLevel.K3);
        public void SetK4() => SetStiffness(StiffnessLevel.K4);
        public void SetK5() => SetStiffness(StiffnessLevel.K5);

        public void SetDistance3() => SetDistance(3f);
        public void SetDistance4() => SetDistance(4f);
        public void SetDistance5() => SetDistance(5f);
        public void SetDistance6() => SetDistance(6f);
        public void SetDistance7() => SetDistance(7f);

        public void ResetZero() => compressionController.SetCurrentAsZero();
        public void Recalibrate() => HapticCalibration.Recalibrate();
        public void ForceFail() => landingDetector.ForceFail();
        public void ClearOverride() => platformGenerator.ClearDebugOverride();

        private void SetStiffness(StiffnessLevel level)
        {
            platformGenerator.SetDebugStiffnessOverride(level);
            Refresh();
        }

        private void SetDistance(float distance)
        {
            platformGenerator.SetDebugDistanceOverride(distance);
            Refresh();
        }

        private void Refresh()
        {
            bool knobMode = GameSettings.SelectedInputMode == InputMode.Knob;
            long deltaEncoder = encoderUnwrapper.UnwrappedEncoder - compressionController.ZeroEncoder;
            double angle = deltaEncoder / CountsPerRevolution * 360d;
            ElasticPlatform currentPlatform = jumpController.CurrentPlatform;
            StiffnessLevel stiffnessLevel = currentPlatform.StiffnessLevel;
            float hapticStiffness = hapticController.Settings.GetHapticStiffness(stiffnessLevel);

            statusBuilder.Length = 0;
            AppendEncoderValue("Raw Encoder", knobMode, encoderUnwrapper.RawEncoder);
            AppendEncoderValue("Unwrapped Encoder", knobMode, encoderUnwrapper.UnwrappedEncoder);
            AppendEncoderValue("Zero Encoder", knobMode, compressionController.ZeroEncoder);
            AppendEncoderValue("Delta Encoder", knobMode, deltaEncoder);
            statusBuilder.Append("Angle: ").Append(knobMode ? angle.ToString("0.0") + "°" : "N/A").AppendLine();
            statusBuilder.Append("Compression: ").Append(compressionController.Compression.ToString("0.000")).AppendLine();
            statusBuilder.Append("Physics K / K Level: ")
                .Append(currentPlatform.PhysicsStiffness.ToString("0.###")).Append(" / ").Append(stiffnessLevel).AppendLine();
            statusBuilder.Append("Haptic kh: ").Append(hapticStiffness.ToString("0.0")).AppendLine();
            statusBuilder.Append("Current Damping: ").Append(hapticController.CurrentDamping.ToString("0.0")).AppendLine();
            statusBuilder.Append("Dmin: ").Append(hapticController.Settings.DMin.ToString("0.0")).AppendLine();
            statusBuilder.Append("DmaxUser: ").Append(hapticController.EffectiveDmaxUser.ToString("0.0")).AppendLine();
            statusBuilder.Append("Target Distance: ").Append(platformGenerator.SelectedDistance.ToString("0.0")).AppendLine();
            statusBuilder.Append("Current Game State: ").Append(GetCurrentGameState()).AppendLine();
            statusBuilder.Append("InputMode: ").Append(GameSettings.SelectedInputMode).AppendLine();
            statusBuilder.Append("Haptic: ").Append(GameSettings.HapticFeedbackEnabled ? "ON" : "OFF");
            statusBuilder.AppendLine();
            statusBuilder.Append("Hardware Mode: ").Append(GetHardwareMode()).AppendLine();
            statusBuilder.Append("Knob Source: ").Append(GetKnobSource()).AppendLine();
            statusBuilder.Append("UDP Receiving: ").Append(GetUdpInputStatus()).AppendLine();
            statusBuilder.Append("Last Raw Encoder: ").Append(udpInputReceiver.LastRawEncoder).AppendLine();
            statusBuilder.Append("UDP Output: ").Append(udpHapticOutput.IsSending ? "Sending" : "Stopped").AppendLine();
            statusBuilder.Append("Last Damping Sent: ").Append(udpHapticOutput.LastDampingSent.ToString("0"));

            string status = statusBuilder.ToString();
            if (statusText.text != status)
            {
                statusText.text = status;
            }

            string debugStatus = platformGenerator.DebugOverrideActive
                ? "DEBUG OVERRIDE ACTIVE\nHide panel or CLEAR OVERRIDE to restore mode control."
                : "Mode control active";
            if (overrideText.text != debugStatus)
            {
                overrideText.text = debugStatus;
            }
        }

        private void AppendEncoderValue(string label, bool available, long value)
        {
            statusBuilder.Append(label).Append(": ");
            statusBuilder.Append(available ? value.ToString() : "N/A").AppendLine();
        }

        private GameState GetCurrentGameState()
        {
            if (livesManager.IsGameOver)
            {
                return GameState.GameOver;
            }

            if (jumpController.IsAirborne)
            {
                return GameState.Airborne;
            }

            return compressionController.Compression > 0f
                ? GameState.Compressing
                : GameState.Ready;
        }

        private string GetUdpInputStatus()
        {
            if (GameSettings.SelectedInputMode != InputMode.Knob ||
                GameSettings.SelectedHardwareMode != HardwareMode.RealHardware)
            {
                return "Disabled";
            }

            if (udpInputReceiver.IsReceiving)
            {
                return "Receiving";
            }

            return udpInputReceiver.IsListening ? "Waiting / No Data" : "Stopped";
        }

        private static string GetHardwareMode()
        {
            return GameSettings.SelectedInputMode == InputMode.Knob
                ? GameSettings.SelectedHardwareMode.ToString()
                : "N/A";
        }

        private static string GetKnobSource()
        {
            if (GameSettings.SelectedInputMode == InputMode.Keyboard)
            {
                return "Keyboard Charge";
            }

            return GameSettings.SelectedHardwareMode == HardwareMode.RealHardware
                ? "UDP"
                : "Keyboard Knob Simulator";
        }
    }
}
