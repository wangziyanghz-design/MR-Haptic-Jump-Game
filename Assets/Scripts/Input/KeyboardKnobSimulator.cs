using System;
using UnityEngine;

namespace MRKnobJump.Input
{
    public sealed class KeyboardKnobSimulator : MonoBehaviour, IKnobInputSource
    {
        [SerializeField, Min(1)] private int countsPerRevolution = 8000;
        [SerializeField, Min(0f)] private float countsPerSecond = 2400f;
        [SerializeField] private int startingRawEncoder;

        private double encoderAccumulator;
        private int rawEncoder;

        public int RawEncoder => rawEncoder;

        private void Awake()
        {
            ResetEncoder(startingRawEncoder);
        }

        private void Update()
        {
            int direction = GetKeyboardDirection();
            if (direction == 0)
            {
                return;
            }

            encoderAccumulator = Wrap(
                encoderAccumulator + direction * (double)countsPerSecond * Time.deltaTime);
            rawEncoder = (int)encoderAccumulator;
        }

        public void ResetEncoder(int value = 0)
        {
            encoderAccumulator = Wrap(value);
            rawEncoder = (int)encoderAccumulator;
        }

        private int GetKeyboardDirection()
        {
            bool increase = UnityEngine.Input.GetKey(KeyCode.D) ||
                            UnityEngine.Input.GetKey(KeyCode.RightArrow);
            bool decrease = UnityEngine.Input.GetKey(KeyCode.A) ||
                            UnityEngine.Input.GetKey(KeyCode.LeftArrow);

            return (increase ? 1 : 0) - (decrease ? 1 : 0);
        }

        private double Wrap(double value)
        {
            double wrapped = value % countsPerRevolution;
            return wrapped < 0d ? wrapped + countsPerRevolution : wrapped;
        }

        private void OnValidate()
        {
            countsPerRevolution = Math.Max(1, countsPerRevolution);
            countsPerSecond = Mathf.Max(0f, countsPerSecond);
            startingRawEncoder = (int)Wrap(startingRawEncoder);

            if (Application.isPlaying)
            {
                encoderAccumulator = Wrap(encoderAccumulator);
                rawEncoder = (int)encoderAccumulator;
            }
        }
    }
}
