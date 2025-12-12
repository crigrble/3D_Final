using UnityEngine;


namespace NekoLegends
{
    using System.Collections.Generic;
    using UnityEngine;

    public class CloudRotator : MonoBehaviour
    {
        // A helper class to hold each cloud's transform and its current angle (in radians)
        [System.Serializable]
        public class Cloud
        {
            public Transform cloudTransform;
            [Tooltip("Optional: leave 0 to have it auto-set on Start based on the cloud's position.")]
            public float currentAngle;
        }

        [Header("Cloud Settings")]
        [Tooltip("List of cloud objects to rotate.")]
        public List<Cloud> clouds = new List<Cloud>();

        [Tooltip("The radius (distance from center) at which the clouds will orbit.")]
        public float radius = 50f;

        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 10f;

        [Header("Center Settings")]
        [Tooltip("If true, the orbit center will track the player's position.")]
        public bool trackPlayer = true;

        [Tooltip("Player transform to track. (Ignored if trackPlayer is false.)")]
        public Transform player;

        [Tooltip("Static center point used if trackPlayer is false.")]
        public Vector3 fixedCenter = Vector3.zero;

        void Start()
        {
            // Determine the starting center position based on tracking or a fixed point.
            Vector3 center = (trackPlayer && player != null) ? player.position : fixedCenter;

            // Initialize each cloud's angle based on its current position relative to the center.
            foreach (var cloud in clouds)
            {
                if (cloud.cloudTransform == null)
                    continue;

                Vector3 offset = cloud.cloudTransform.position - center;
                // Using Atan2 with (z, x) gives the angle on the XZ plane.
                cloud.currentAngle = Mathf.Atan2(offset.z, offset.x);
            }
        }

        void Update()
        {
            // Update the center position every frame.
            Vector3 center = (trackPlayer && player != null) ? player.position : fixedCenter;

            // Convert rotation speed from degrees to radians for calculation.
            float rotationSpeedRad = rotationSpeed * Mathf.Deg2Rad;

            // Update each cloud's position and rotation.
            foreach (var cloud in clouds)
            {
                if (cloud.cloudTransform == null)
                    continue;

                // Increment the angle based on time and rotation speed.
                cloud.currentAngle += rotationSpeedRad * Time.deltaTime;

                // Calculate the new position on the XZ plane.
                Vector3 newPos = new Vector3(
                    Mathf.Cos(cloud.currentAngle),
                    0f,
                    Mathf.Sin(cloud.currentAngle)
                ) * radius + center;

                // Optionally, preserve the cloud's current height (Y axis)
                newPos.y = cloud.cloudTransform.position.y;

                // Apply the new position.
                cloud.cloudTransform.position = newPos;

                // Make the cloud face the center.
                // If you want a pure billboard effect (rotating only around Y), set Y separately.
                Vector3 lookAtTarget = new Vector3(center.x, newPos.y, center.z);
                cloud.cloudTransform.LookAt(lookAtTarget);
            }
        }

        // (Optional) Draw the orbit circle in the Scene view for easier tuning.
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = (trackPlayer && player != null) ? player.position : fixedCenter;
            int segments = 40;
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previousPoint, newPoint);
                previousPoint = newPoint;
            }
        }
    }

}
