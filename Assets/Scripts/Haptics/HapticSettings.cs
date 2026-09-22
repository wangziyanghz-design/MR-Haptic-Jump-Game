using System;
using MRKnobJump.Platform;
using UnityEngine;

namespace MRKnobJump.Haptics
{
    [Serializable]
    public sealed class HapticSettings
    {
        [SerializeField, Min(0f)] private float dMin = 20f;
        [SerializeField, Min(0f)] private float dMaxUser = 80f;

        [Header("Haptic Stiffness")]
        [SerializeField, Range(0f, 1f)] private float k1 = 0.2f;
        [SerializeField, Range(0f, 1f)] private float k2 = 0.4f;
        [SerializeField, Range(0f, 1f)] private float k3 = 0.6f;
        [SerializeField, Range(0f, 1f)] private float k4 = 0.8f;
        [SerializeField, Range(0f, 1f)] private float k5 = 1f;

        public float DMin => dMin;
        public float DMaxUser => dMaxUser;

        public float GetHapticStiffness(StiffnessLevel level)
        {
            switch (level)
            {
                case StiffnessLevel.K1: return k1;
                case StiffnessLevel.K2: return k2;
                case StiffnessLevel.K3: return k3;
                case StiffnessLevel.K4: return k4;
                case StiffnessLevel.K5: return k5;
                default: throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public void Validate()
        {
            dMin = Mathf.Max(0f, dMin);
            dMaxUser = Mathf.Max(dMin, dMaxUser);
            k1 = Mathf.Clamp01(k1);
            k2 = Mathf.Clamp01(k2);
            k3 = Mathf.Clamp01(k3);
            k4 = Mathf.Clamp01(k4);
            k5 = Mathf.Clamp01(k5);
        }
    }
}
