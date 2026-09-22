using UnityEngine;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float followOffsetX = 2f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
        [SerializeField] private float minX = 0f;

        private float fixedY;
        private float fixedZ;
        private float velocityX;

        private void Awake()
        {
            if (player == null)
            {
                Debug.LogError("CameraFollow2D requires a Player reference.", this);
                enabled = false;
                return;
            }
            fixedY = transform.position.y;
            fixedZ = transform.position.z;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            float currentX = transform.position.x;
            float desiredX = Mathf.Max(minX, player.position.x + followOffsetX);
            if (desiredX <= currentX)
            {
                // A respawn can move the player left; discard smoothing momentum and hold.
                velocityX = 0f;
                transform.position = new Vector3(currentX, fixedY, fixedZ);
                return;
            }

            float nextX = Mathf.SmoothDamp(currentX, desiredX, ref velocityX,
                Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.deltaTime);
            transform.position = new Vector3(Mathf.Max(currentX, nextX), fixedY, fixedZ);
        }

        private void OnValidate() => smoothTime = Mathf.Max(0.01f, smoothTime);
    }
}
