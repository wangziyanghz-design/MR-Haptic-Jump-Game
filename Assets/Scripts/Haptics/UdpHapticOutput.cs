using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MRKnobJump.Core;
using UnityEngine;

namespace MRKnobJump.Haptics
{
    [DisallowMultipleComponent]
    public sealed class UdpHapticOutput : MonoBehaviour
    {
        [SerializeField] private HapticController hapticController;
        [SerializeField] private string remoteIP = "127.0.0.1";
        [SerializeField, Min(1)] private int sendPort = 5006;
        [SerializeField, Min(1f)] private float sendRate = 50f;

        private UdpClient sender;
        private float elapsedSendTime;
        private bool shuttingDown;

        public bool IsSending => sender != null;
        public float LastDampingSent { get; private set; }
        public long PacketsSent { get; private set; }
        public string RemoteIP => remoteIP;
        public int OutputPort => sendPort;
        public float SendRate => sendRate;

        private void Awake()
        {
            if (hapticController == null)
            {
                Debug.LogError($"{nameof(UdpHapticOutput)} requires HapticController.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GameSettings.UdpSettingsChanged += ApplySavedSettings;
            ApplySavedSettings();
        }

        private void ApplySavedSettings()
        {
            // Send Dmin to the old endpoint before replacing its socket.
            Shutdown(true);
            remoteIP = GameSettings.UdpRemoteIP;
            sendPort = GameSettings.UdpOutputPort;
            sendRate = GameSettings.UdpSendRate;
            shuttingDown = false;
            if (ShouldSendToHardware())
            {
                StartSender();
            }
        }

        private void Update()
        {
            if (!ShouldSendToHardware())
            {
                if (sender != null)
                {
                    Shutdown(true);
                }
                return;
            }

            if (sender == null || hapticController == null)
            {
                return;
            }

            float interval = 1f / sendRate;
            elapsedSendTime += Time.unscaledDeltaTime;
            if (elapsedSendTime < interval)
            {
                return;
            }

            elapsedSendTime %= interval;
            TrySend(hapticController.CurrentDamping);
        }

        private void StartSender()
        {
            Shutdown(false);
            shuttingDown = false;
            try
            {
                IPAddress address = IPAddress.Parse(remoteIP);
                sender = new UdpClient(address.AddressFamily);
                sender.Connect(address, sendPort);
                elapsedSendTime = 0f;
            }
            catch (Exception exception) when (exception is SocketException || exception is ArgumentException)
            {
                sender?.Close();
                sender = null;
                Debug.LogWarning($"UDP haptic output could not start for {remoteIP}:{sendPort}: {exception.Message}", this);
            }
        }

        private bool TrySend(float damping)
        {
            if (sender == null)
            {
                return false;
            }

            int roundedDamping = Mathf.RoundToInt(damping);
            byte[] bytes = Encoding.ASCII.GetBytes(
                roundedDamping.ToString(CultureInfo.InvariantCulture));
            try
            {
                sender.Send(bytes, bytes.Length);
                LastDampingSent = roundedDamping;
                PacketsSent++;
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private void Shutdown(bool sendSafeMinimum)
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            if (sendSafeMinimum && sender != null && hapticController != null)
            {
                TrySend(hapticController.Settings.DMin);
            }

            sender?.Close();
            sender = null;
        }

        private void OnDisable()
        {
            GameSettings.UdpSettingsChanged -= ApplySavedSettings;
            Shutdown(true);
        }
        private void OnDestroy() => Shutdown(true);
        private void OnApplicationQuit() => Shutdown(true);

        private static bool ShouldSendToHardware()
        {
            return MRKnobJump.Core.GameSettings.SelectedInputMode == MRKnobJump.Core.InputMode.Knob &&
                MRKnobJump.Core.GameSettings.SelectedHardwareMode == MRKnobJump.Core.HardwareMode.RealHardware;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(remoteIP))
            {
                remoteIP = "127.0.0.1";
            }
            sendPort = Mathf.Clamp(sendPort, 1, 65535);
            sendRate = Mathf.Clamp(sendRate, 1f, 100f);
        }
    }
}
