using System;
using System.Net;
using UnityEngine;

namespace MRKnobJump.Core
{
    public enum InputMode
    {
        Knob,
        Keyboard
    }

    public enum HardwareMode
    {
        RealHardware,
        Simulation
    }

    [CreateAssetMenu(fileName = "GameSettings", menuName = "MRKnobJump/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        private const string GameModeKey = "MRKnobJump.GameMode";
        private const string InputModeKey = "MRKnobJump.InputMode";
        private const string HardwareModeKey = "MRKnobJump.HardwareMode";
        private const string HapticKey = "MRKnobJump.HapticEnabled";
        private const string ListenPortKey = "MRKnobJump.Udp.ListenPort";
        private const string RemoteIPKey = "MRKnobJump.Udp.RemoteIP";
        private const string OutputPortKey = "MRKnobJump.Udp.OutputPort";
        private const string SendRateKey = "MRKnobJump.Udp.SendRate";

        public const int DefaultListenPort = 5005;
        public const string DefaultRemoteIP = "127.0.0.1";
        public const int DefaultOutputPort = 5006;
        public const float DefaultSendRate = 50f;
        public static int UdpListenPort => Mathf.Clamp(PlayerPrefs.GetInt(ListenPortKey, DefaultListenPort), 1, 65535);
        public static string UdpRemoteIP => IPAddress.TryParse(PlayerPrefs.GetString(RemoteIPKey, DefaultRemoteIP), out var ip)
            ? ip.ToString() : DefaultRemoteIP;
        public static int UdpOutputPort => Mathf.Clamp(PlayerPrefs.GetInt(OutputPortKey, DefaultOutputPort), 1, 65535);
        public static float UdpSendRate
        {
            get
            {
                float value = PlayerPrefs.GetFloat(SendRateKey, DefaultSendRate);
                return float.IsNaN(value) || float.IsInfinity(value) ? DefaultSendRate : Mathf.Clamp(value, 1f, 100f);
            }
        }

        public static event Action UdpSettingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetUdpSubscribers() => UdpSettingsChanged = null;

        public static bool TryApplyUdpSettings(int listenPort, string remoteIP, int outputPort, float sendRate, out string error)
        {
            if (listenPort < 1 || listenPort > 65535 || outputPort < 1 || outputPort > 65535)
            {
                error = "Ports must be between 1 and 65535.";
                return false;
            }
            if (!IPAddress.TryParse(remoteIP?.Trim(), out IPAddress address))
            {
                error = "Enter a valid IP address. Previous settings were kept.";
                return false;
            }
            if (float.IsNaN(sendRate) || float.IsInfinity(sendRate) || sendRate < 1f || sendRate > 100f)
            {
                error = "Send rate must be between 1 and 100 Hz.";
                return false;
            }

            // Validate the entire form before saving or notifying live sockets.
            PlayerPrefs.SetInt(ListenPortKey, listenPort);
            PlayerPrefs.SetString(RemoteIPKey, address.ToString());
            PlayerPrefs.SetInt(OutputPortKey, outputPort);
            PlayerPrefs.SetFloat(SendRateKey, sendRate);
            PlayerPrefs.Save();
            UdpSettingsChanged?.Invoke();
            error = string.Empty;
            return true;
        }

        public static GameMode SelectedGameMode => (GameMode)PlayerPrefs.GetInt(GameModeKey, (int)GameMode.Experience);
        public static InputMode SelectedInputMode => (InputMode)PlayerPrefs.GetInt(InputModeKey, (int)InputMode.Knob);
        public static HardwareMode SelectedHardwareMode => (HardwareMode)PlayerPrefs.GetInt(
            HardwareModeKey, (int)HardwareMode.RealHardware);
        public static bool HapticFeedbackEnabled => SelectedInputMode == InputMode.Knob && PlayerPrefs.GetInt(HapticKey, 1) != 0;

        public static void SelectGameMode(GameMode mode)
        {
            PlayerPrefs.SetInt(GameModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        public static void SelectInputMode(InputMode mode)
        {
            PlayerPrefs.SetInt(InputModeKey, (int)mode);
            if (mode == InputMode.Keyboard) PlayerPrefs.SetInt(HapticKey, 0);
            PlayerPrefs.Save();
        }

        public static void SetHapticFeedback(bool enabled)
        {
            PlayerPrefs.SetInt(HapticKey, enabled && SelectedInputMode == InputMode.Knob ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SelectHardwareMode(HardwareMode mode)
        {
            PlayerPrefs.SetInt(HardwareModeKey, (int)mode);
            PlayerPrefs.Save();
        }

        [Header("Knob")]
        [SerializeField, Min(1)] private int encoderCountsPerRevolution = 8000;
        [SerializeField, Min(1)] private int maxCompressionCounts = 4000;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpScale = 1f;
        [SerializeField, Range(0f, 90f)] private float jumpAngleDegrees = 45f;

        [Header("Session")]
        [SerializeField, Min(1)] private int startingLives = 3;
        [SerializeField, Range(0f, 1f)] private float perfectLandingRatio = 0.2f;

        public int EncoderCountsPerRevolution => encoderCountsPerRevolution;
        public int MaxCompressionCounts => maxCompressionCounts;
        public float JumpScale => jumpScale;
        public float JumpAngleDegrees => jumpAngleDegrees;
        public int StartingLives => startingLives;
        public float PerfectLandingRatio => perfectLandingRatio;
    }
}
