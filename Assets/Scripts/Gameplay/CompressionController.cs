using System.Collections;
using MRKnobJump.Core;
using MRKnobJump.Input;
using UnityEngine;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CompressionController : MonoBehaviour
    {
        [SerializeField] private EncoderUnwrapper encoderUnwrapper;
        [SerializeField] private KeyboardKnobSimulator keyboardKnobSimulator;
        [SerializeField] private UdpKnobInputReceiver udpKnobInputReceiver;
        [SerializeField] private KeyboardChargeInput keyboardChargeInput;
        [SerializeField, Min(1)] private int maxCompressionCounts = 4000;

        private long zeroEncoder;
        private bool hasZero;
        private float encoderCompression;
        private InputMode activeInputMode;
        private HardwareMode activeHardwareMode;

        public float Compression => activeInputMode == InputMode.Keyboard
            ? keyboardChargeInput.Compression
            : encoderCompression;
        public long ZeroEncoder => zeroEncoder;
        public InputMode ActiveInputMode => activeInputMode;
        public HardwareMode ActiveHardwareMode => activeHardwareMode;

        private void Awake()
        {
            if (encoderUnwrapper == null || keyboardKnobSimulator == null ||
                udpKnobInputReceiver == null || keyboardChargeInput == null)
            {
                Debug.LogError(
                    $"{nameof(CompressionController)} requires EncoderUnwrapper, " +
                    "KeyboardKnobSimulator, UdpKnobInputReceiver, and KeyboardChargeInput references.",
                    this);
                enabled = false;
                return;
            }

            activeInputMode = GameSettings.SelectedInputMode;
            activeHardwareMode = GameSettings.SelectedHardwareMode;
            bool keyboardMode = activeInputMode == InputMode.Keyboard;
            bool realHardware = !keyboardMode && activeHardwareMode == HardwareMode.RealHardware;
            bool simulation = !keyboardMode && activeHardwareMode == HardwareMode.Simulation;
            encoderUnwrapper.SetInputSource(realHardware
                ? (MonoBehaviour)udpKnobInputReceiver
                : keyboardKnobSimulator);
            keyboardKnobSimulator.enabled = simulation;
            udpKnobInputReceiver.enabled = realHardware;
            encoderUnwrapper.enabled = !keyboardMode;
            keyboardChargeInput.enabled = keyboardMode;
        }

        private IEnumerator Start()
        {
            if (!enabled)
            {
                yield break;
            }

            // Let the input components initialize before capturing this jump's zero.
            yield return null;

            if (activeInputMode == InputMode.Knob &&
                encoderUnwrapper.InputSource is UdpKnobInputReceiver udpInput)
            {
                while (!udpInput.HasReceivedData)
                {
                    yield return null;
                }

                // The unwrapper consumes the first UDP sample on the following Update.
                yield return null;
            }

            SetCurrentAsZero();
        }

        private void Update()
        {
            if (activeInputMode != InputMode.Knob || !hasZero)
            {
                return;
            }

            long deltaEncoder = encoderUnwrapper.UnwrappedEncoder - zeroEncoder;
            encoderCompression = Mathf.Clamp01(
                (float)((double)deltaEncoder / maxCompressionCounts));
        }

        public void SetCurrentAsZero()
        {
            encoderCompression = 0f;

            if (activeInputMode == InputMode.Keyboard)
            {
                keyboardChargeInput.ResetCharge();
                hasZero = true;
                return;
            }

            zeroEncoder = encoderUnwrapper.UnwrappedEncoder;
            hasZero = true;
        }

        private void OnValidate()
        {
            maxCompressionCounts = Mathf.Max(1, maxCompressionCounts);
        }
    }
}
