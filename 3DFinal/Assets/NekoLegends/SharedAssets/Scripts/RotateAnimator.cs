using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class RotateAnimator : MonoBehaviour
    {
        [SerializeField] public bool _isEnabled = false; // Made serialized for Inspector
        [SerializeField] public float _speed = 50f;      // Already serialized
        [SerializeField] public float _angle;            // Made serialized for Inspector

        [Header("Advanced Settings")]
        [SerializeField] private bool UseAdvancedSettings;

        [SerializeField] private bool lockXAxis = false;
        [SerializeField] private bool lockYAxis = false;
        [SerializeField] private bool lockZAxis = false;

        void Update()
        {
            if (_isEnabled)
            {
                if (UseAdvancedSettings)
                {
                    RotateObjectAdvanced();
                }
                else
                {
                    // Desired orientation based on the current angle, only rotating around z-axis
                    Quaternion targetRotation = Quaternion.Euler(0, 0, _angle + _speed * Time.deltaTime);

                    // Smoothly interpolate towards the desired orientation
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * _speed);
                }
            }
        }

        private void RotateObjectAdvanced()
        {
            float angle = _speed * Time.deltaTime;

            // Calculate the incremental rotation
            Vector3 rotationIncrement = new Vector3(
                lockXAxis ? 0 : angle,
                lockYAxis ? 0 : angle,
                lockZAxis ? 0 : angle
            );

            // Apply the rotation
            transform.Rotate(rotationIncrement, Space.Self);
        }

        // Called from GUI button event
        public void ToggleRotation()
        {
            _isEnabled = !_isEnabled;
        }
    }
}