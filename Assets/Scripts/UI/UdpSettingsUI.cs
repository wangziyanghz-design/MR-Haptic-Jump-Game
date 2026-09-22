using System.Globalization;
using MRKnobJump.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class UdpSettingsUI : MonoBehaviour
    {
        [SerializeField] private InputField listenPortInput;
        [SerializeField] private InputField remoteIPInput;
        [SerializeField] private InputField outputPortInput;
        [SerializeField] private InputField sendRateInput;
        [SerializeField] private Text statusText;

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            listenPortInput.text = GameSettings.UdpListenPort.ToString(CultureInfo.InvariantCulture);
            remoteIPInput.text = GameSettings.UdpRemoteIP;
            outputPortInput.text = GameSettings.UdpOutputPort.ToString(CultureInfo.InvariantCulture);
            sendRateInput.text = GameSettings.UdpSendRate.ToString("0.##", CultureInfo.InvariantCulture);
            statusText.text = string.Empty;
        }

        public void Apply()
        {
            if (!int.TryParse(listenPortInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int listen) ||
                !int.TryParse(outputPortInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int output))
            {
                ShowStatus("Ports must be whole numbers between 1 and 65535.", false);
                return;
            }
            if (!float.TryParse(sendRateInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate))
            {
                ShowStatus("Enter a send rate between 1 and 100 Hz.", false);
                return;
            }
            if (!GameSettings.TryApplyUdpSettings(listen, remoteIPInput.text, output, rate, out string error))
            {
                ShowStatus(error, false);
                return;
            }
            Refresh();
            ShowStatus("Applied and saved. Active UDP connections updated.", true);
        }

        public void ResetDefaults()
        {
            GameSettings.TryApplyUdpSettings(GameSettings.DefaultListenPort, GameSettings.DefaultRemoteIP,
                GameSettings.DefaultOutputPort, GameSettings.DefaultSendRate, out _);
            Refresh();
            ShowStatus("Default UDP settings restored and saved.", true);
        }

        private void ShowStatus(string message, bool success)
        {
            statusText.text = message;
            statusText.color = success ? new Color(0.3f, 0.95f, 0.85f) : new Color(1f, 0.4f, 0.4f);
        }
    }
}
