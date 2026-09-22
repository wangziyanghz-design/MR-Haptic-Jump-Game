using UnityEngine;

namespace MRKnobJump.Gameplay
{
    public enum LandingResult
    {
        Success,
        Perfect,
        Fail
    }

    [DisallowMultipleComponent]
    public sealed class LandingEvaluator : MonoBehaviour
    {
        public LandingResult Evaluate(float playerLandingX, float targetCenterX, float platformWidth)
        {
            float error = Mathf.Abs(playerLandingX - targetCenterX);
            if (!(platformWidth > 0f) || float.IsInfinity(platformWidth) ||
                float.IsNaN(error) || error > platformWidth * 0.5f)
            {
                return LandingResult.Fail;
            }

            // Double precision avoids an extended float product widening the strict boundary.
            return (double)error < (double)platformWidth * 0.1d
                ? LandingResult.Perfect
                : LandingResult.Success;
        }
    }
}
