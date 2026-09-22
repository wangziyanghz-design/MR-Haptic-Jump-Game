using System;
using UnityEngine;

namespace MRKnobJump.Input
{
    [DisallowMultipleComponent]
    public sealed class EncoderUnwrapper : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField, Min(1)] private int countsPerRevolution = 8000;

        private IKnobInputSource knobInputSource;
        private int previousRawEncoder;
        private bool hasReference;

        public int RawEncoder { get; private set; }
        public long UnwrappedEncoder { get; private set; }
        public MonoBehaviour InputSource => inputSource;

        public bool SetInputSource(MonoBehaviour source)
        {
            if (!(source is IKnobInputSource resolvedSource))
            {
                Debug.LogError($"Input source must implement {nameof(IKnobInputSource)}.", this);
                return false;
            }

            inputSource = source;
            knobInputSource = resolvedSource;
            hasReference = false;
            RawEncoder = 0;
            UnwrappedEncoder = 0;
            return true;
        }

        private void Awake()
        {
            if (!TryResolveInputSource())
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (enabled && CanInitializeFromCurrentInput())
            {
                InitializeFromCurrentRaw();
            }
        }

        private void Update()
        {
            if (knobInputSource == null && !TryResolveInputSource())
            {
                enabled = false;
                return;
            }

            if (!hasReference)
            {
                if (CanInitializeFromCurrentInput())
                {
                    InitializeFromCurrentRaw();
                }
                return;
            }

            int currentRawEncoder = NormalizeRaw(knobInputSource.RawEncoder);
            int delta = currentRawEncoder - previousRawEncoder;
            int halfRevolution = countsPerRevolution / 2;

            if (delta > halfRevolution)
            {
                delta -= countsPerRevolution;
            }
            else if (delta < -halfRevolution)
            {
                delta += countsPerRevolution;
            }

            RawEncoder = currentRawEncoder;
            UnwrappedEncoder += delta;
            previousRawEncoder = currentRawEncoder;
        }

        public void ResetUnwrapped(long value = 0)
        {
            if (!TryResolveInputSource())
            {
                return;
            }

            RawEncoder = NormalizeRaw(knobInputSource.RawEncoder);
            UnwrappedEncoder = value;
            previousRawEncoder = RawEncoder;
            hasReference = true;
        }

        private void InitializeFromCurrentRaw()
        {
            RawEncoder = NormalizeRaw(knobInputSource.RawEncoder);
            UnwrappedEncoder = RawEncoder;
            previousRawEncoder = RawEncoder;
            hasReference = true;
        }

        private bool TryResolveInputSource()
        {
            knobInputSource = inputSource as IKnobInputSource;
            if (knobInputSource != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(EncoderUnwrapper)} requires an inputSource that implements " +
                $"{nameof(IKnobInputSource)}.",
                this);
            return false;
        }

        private bool CanInitializeFromCurrentInput()
        {
            return !(knobInputSource is UdpKnobInputReceiver udpInput) || udpInput.HasReceivedData;
        }

        private int NormalizeRaw(int value)
        {
            int normalized = value % countsPerRevolution;
            return normalized < 0 ? normalized + countsPerRevolution : normalized;
        }

        private void OnValidate()
        {
            countsPerRevolution = Math.Max(1, countsPerRevolution);

            if (inputSource != null && !(inputSource is IKnobInputSource))
            {
                Debug.LogWarning(
                    $"Assigned inputSource must implement {nameof(IKnobInputSource)}.",
                    this);
            }
        }
    }
}
