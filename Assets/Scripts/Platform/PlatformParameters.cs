using System;
using UnityEngine;

namespace MRKnobJump.Platform
{
    public enum StiffnessLevel
    {
        K1,
        K2,
        K3,
        K4,
        K5
    }

    [Serializable]
    public sealed class PlatformParameters
    {
        [SerializeField] private StiffnessLevel stiffnessLevel = StiffnessLevel.K1;
        [SerializeField, Min(0.0001f)] private float physicsStiffness = 1f;

        public StiffnessLevel StiffnessLevel => stiffnessLevel;
        public float PhysicsStiffness => physicsStiffness;

        public void Configure(StiffnessLevel level, float stiffness)
        {
            stiffnessLevel = level;
            physicsStiffness = Mathf.Max(0.0001f, stiffness);
        }
    }
}
