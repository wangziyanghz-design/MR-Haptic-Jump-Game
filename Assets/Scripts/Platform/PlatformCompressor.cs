using MRKnobJump.Gameplay;
using UnityEngine;

namespace MRKnobJump.Platform
{
    [DisallowMultipleComponent]
    public sealed class PlatformCompressor : MonoBehaviour
    {
        [SerializeField] private CompressionController compressionController;
        [SerializeField, Range(0.01f, 1f)] private float fullyCompressedScaleY = 0.65f;

        private Renderer platformRenderer;
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private float originalBottomY;

        private void Awake()
        {
            if (compressionController == null)
            {
                Debug.LogError(
                    $"{nameof(PlatformCompressor)} requires a {nameof(CompressionController)} reference.",
                    this);
                enabled = false;
                return;
            }

            platformRenderer = GetComponent<Renderer>();
            if (platformRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(PlatformCompressor)} requires a Renderer on the same GameObject.",
                    this);
                enabled = false;
                return;
            }

            CaptureRestPose();
        }

        public void CaptureRestPose()
        {
            if (platformRenderer == null) platformRenderer = GetComponent<Renderer>();
            originalLocalPosition = transform.localPosition;
            originalLocalScale = transform.localScale;
            originalBottomY = platformRenderer.bounds.min.y;
        }

        public void RestoreRestPose()
        {
            transform.localPosition = originalLocalPosition;
            transform.localScale = originalLocalScale;
        }

        private void LateUpdate()
        {
            float scaleFactor = Mathf.Lerp(
                1f,
                fullyCompressedScaleY,
                compressionController.Compression);

            transform.localPosition = originalLocalPosition;
            transform.localScale = new Vector3(
                originalLocalScale.x,
                originalLocalScale.y * scaleFactor,
                originalLocalScale.z);

            Vector3 worldPosition = transform.position;
            worldPosition.y += originalBottomY - platformRenderer.bounds.min.y;
            transform.position = worldPosition;
        }

        private void OnValidate()
        {
            fullyCompressedScaleY = Mathf.Clamp(fullyCompressedScaleY, 0.01f, 1f);
        }
    }
}
