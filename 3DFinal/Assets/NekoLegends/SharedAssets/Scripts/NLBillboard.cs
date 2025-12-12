using UnityEngine;

namespace NekoLegends
{
    public class Billboard : MonoBehaviour
    {
        public enum Orientation
        {
            Vertical,   // default: mesh’s +Z faces camera
            Horizontal  // mesh’s +Y (up) faces camera
        }

        [Header("Billboard Settings")]
        public bool enabledBillboard = true;
        public Orientation orientation = Orientation.Vertical;
        [Tooltip("Rotate 180° around up-axis after orienting toward camera")]
        public bool flip = false;

        void LateUpdate()
        {
            if (!enabledBillboard) return;

            // world-space dir from this object to camera
            Vector3 dirToCam = transform.position - Camera.main.transform.position;

            // base look rotation: +Z points at camera
            Quaternion lookRot = Quaternion.LookRotation(dirToCam);

            // if this is a horizontal plane (normals up), tilt by -90° around X
            if (orientation == Orientation.Horizontal)
                lookRot *= Quaternion.Euler(-90f, 0f, 0f);

            // apply optional 180° flip around the local up axis
            if (flip)
                lookRot *= Quaternion.Euler(0f, 180f, 0f);

            transform.rotation = lookRot;
        }
    }
}
