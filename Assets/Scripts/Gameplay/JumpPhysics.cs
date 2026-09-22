using UnityEngine;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class JumpPhysics : MonoBehaviour
    {
        private const float LaunchAngleDegrees = 45f;

        public Vector2 CalculateLaunchVelocity(
            float compression,
            float stiffness,
            float jumpScale)
        {
            float safeCompression = Mathf.Clamp01(compression);
            float safeStiffness = Mathf.Max(0f, stiffness);
            float safeJumpScale = Mathf.Max(0f, jumpScale);
            float launchSpeed = safeJumpScale * safeCompression * Mathf.Sqrt(safeStiffness);

            float angleRadians = LaunchAngleDegrees * Mathf.Deg2Rad;
            return new Vector2(
                launchSpeed * Mathf.Cos(angleRadians),
                launchSpeed * Mathf.Sin(angleRadians));
        }
    }
}
