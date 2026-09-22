using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MRKnobJump.Core;
using UnityEngine;

namespace MRKnobJump.Input
{
    [DisallowMultipleComponent]
    public sealed class UdpKnobInputReceiver : MonoBehaviour, IKnobInputSource
    {
        [SerializeField, Min(1)] private int receivePort = 5005;
        [SerializeField, Min(1)] private int countsPerRevolution = 8000;
        [SerializeField, Min(0.05f)] private float receivingTimeout = 1f;

        private UdpClient receiver;
        private float lastPacketTime = float.NegativeInfinity;

        public int RawEncoder { get; private set; }
        public int LastRawEncoder => RawEncoder;
        public bool HasReceivedData => PacketsReceived > 0;
        public bool IsListening => receiver != null;
        public bool IsReceiving => IsListening &&
            Time.realtimeSinceStartup - lastPacketTime <= receivingTimeout;
        public long PacketsReceived { get; private set; }
        public int ListenPort => receivePort;

        private void OnEnable()
        {
            GameSettings.UdpSettingsChanged += ApplySavedSettings;
            ApplySavedSettings();
        }

        private void ApplySavedSettings()
        {
            StopReceiver();
            receivePort = GameSettings.UdpListenPort;
            lastPacketTime = float.NegativeInfinity;
            if (GameSettings.SelectedInputMode == InputMode.Knob &&
                GameSettings.SelectedHardwareMode == HardwareMode.RealHardware)
            {
                StartReceiver();
            }
        }

        private void Update()
        {
            if (receiver == null)
            {
                return;
            }

            // Bound work per frame so a packet burst cannot stall gameplay.
            for (int packetIndex = 0; packetIndex < 64; packetIndex++)
            {
                try
                {
                    if (receiver.Available == 0) return;
                    IPEndPoint sender = null;
                    byte[] bytes = receiver.Receive(ref sender);
                    TryAcceptPacket(bytes);
                }
                catch (SocketException exception)
                {
                    if (exception.SocketErrorCode != SocketError.WouldBlock)
                    {
                        StopReceiver();
                    }
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        private void StartReceiver()
        {
            StopReceiver();
            try
            {
                receiver = new UdpClient(receivePort);
                receiver.Client.Blocking = false;
            }
            catch (SocketException exception)
            {
                receiver?.Close();
                receiver = null;
                Debug.LogWarning($"UDP knob input could not listen on port {receivePort}: {exception.Message}", this);
            }
        }

        private void TryAcceptPacket(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            string payload = Encoding.ASCII.GetString(bytes).Trim();
            if (!int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ||
                value < 0 || value >= countsPerRevolution)
            {
                return;
            }

            RawEncoder = value;
            PacketsReceived++;
            lastPacketTime = Time.realtimeSinceStartup;
        }

        private void StopReceiver()
        {
            if (receiver == null)
            {
                return;
            }

            receiver.Close();
            receiver = null;
        }

        private void OnDisable()
        {
            GameSettings.UdpSettingsChanged -= ApplySavedSettings;
            StopReceiver();
        }
        private void OnDestroy() => StopReceiver();
        private void OnApplicationQuit() => StopReceiver();

        private void OnValidate()
        {
            receivePort = Mathf.Clamp(receivePort, 1, 65535);
            countsPerRevolution = Mathf.Max(1, countsPerRevolution);
            receivingTimeout = Mathf.Max(0.05f, receivingTimeout);
        }
    }
}
