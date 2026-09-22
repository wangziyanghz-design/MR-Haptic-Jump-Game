using UnityEngine;

namespace MRKnobJump.Platform
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ElasticPlatform : MonoBehaviour
    {
        [SerializeField] private PlatformParameters parameters = new PlatformParameters();

        public PlatformParameters Parameters => parameters;
        public StiffnessLevel StiffnessLevel => parameters.StiffnessLevel;
        public float PhysicsStiffness => parameters.PhysicsStiffness;

        public void Configure(StiffnessLevel level, float physicsStiffness)
        {
            parameters.Configure(level, physicsStiffness);
        }
    }
}
