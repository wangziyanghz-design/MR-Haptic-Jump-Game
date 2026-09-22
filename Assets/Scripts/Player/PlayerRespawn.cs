using MRKnobJump.Gameplay;
using UnityEngine;

namespace MRKnobJump.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private PlayerJumpController jumpController;
        [SerializeField] private CompressionController compressionController;
        [SerializeField, Min(0f)] private float spawnClearance = 0.03f;

        private Vector2 spawnPosition;
        private float spawnRotation;
        private bool hasSpawn;

        private void Start()
        {
            if (playerBody == null || playerCollider == null || jumpController == null ||
                compressionController == null || jumpController.CurrentPlatform == null)
            {
                Debug.LogError("PlayerRespawn requires body, collider, jump controller, compression and current platform references.", this);
                enabled = false;
                return;
            }

            CaptureCurrentPlatformSpawn();
        }

        public void CaptureCurrentPlatformSpawn()
        {
            // Called on startup and promotion, before this platform is compressed.
            Bounds platformBounds = jumpController.CurrentPlatform.GetComponent<BoxCollider2D>().bounds;
            float bodyToFeet = playerBody.position.y - playerCollider.bounds.min.y;
            spawnPosition = new Vector2(platformBounds.center.x,
                platformBounds.max.y + bodyToFeet + spawnClearance);
            spawnRotation = playerBody.rotation;
            hasSpawn = true;
        }

        public void Respawn()
        {
            if (!isActiveAndEnabled || !hasSpawn) return;

            playerBody.velocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.position = spawnPosition;
            playerBody.rotation = spawnRotation;
            jumpController.CompleteJump();
            compressionController.SetCurrentAsZero();
            // JumpEnabled remains owned by LivesManager, so respawn cannot bypass Game Over.
        }
    }
}
