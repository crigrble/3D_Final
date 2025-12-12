using System;
using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    // Enum to define the possible rotation axes
    public enum Axis { X, Y, Z }

    // Struct to hold each rotation step's angle and axis
    [Serializable]
    public struct RotationStep
    {
        public float angle; // Rotation angle in degrees
        public Axis axis;   // Axis to rotate around (X, Y, or Z)
    }

    // Class to handle rotation of multiple GameObjects through a sequence of rotations
    public class RotateDemoItems : MonoBehaviour
    {
        // List of rotations to cycle through, editable in the Inspector
        public List<RotationStep> rotationList;

        // List of GameObjects to rotate, editable in the Inspector
        public List<GameObject> targetObjects;

        // Checkbox to determine if rotation is appended or set, editable in the Inspector
        [Tooltip("If true, rotation is added to current rotation; if false, rotation is set to the specified angle.")]
        public bool appendRotation = false;

        // Index to track the current rotation step
        private int currentIndex = 0;

        // Update is called once per frame
        void Update()
        {
            // Check if the spacebar is pressed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RotateNext();
            }
        }

        // Function to apply the next rotation in the list to all target GameObjects
        public void RotateNext()
        {
            // Ensure there are rotations and target objects to rotate
            if (rotationList.Count > 0 && targetObjects != null && targetObjects.Count > 0)
            {
                // Get the current rotation step
                RotationStep step = rotationList[currentIndex];

                // Convert the axis to a Vector3
                Vector3 axisVector = GetAxisVector(step.axis);

                // Apply the rotation to each target GameObject
                foreach (GameObject obj in targetObjects)
                {
                    if (obj != null)
                    {
                        if (appendRotation)
                        {
                            // Append rotation to the current rotation
                            obj.transform.Rotate(axisVector, step.angle, Space.Self);
                        }
                        else
                        {
                            // Set rotation to the specified angle around the chosen axis
                            // Create a quaternion for the desired rotation
                            Quaternion targetRotation = Quaternion.AngleAxis(step.angle, axisVector);
                            // Apply in local space by combining with parent's world rotation
                            obj.transform.localRotation = targetRotation;
                        }
                    }
                }

                // Move to the next index, looping back to 0 if at the end
                currentIndex = (currentIndex + 1) % rotationList.Count;
            }
        }

        // Helper function to convert Axis enum to Vector3
        private Vector3 GetAxisVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return Vector3.right;   // Local x-axis (1, 0, 0)
                case Axis.Y: return Vector3.up;      // Local y-axis (0, 1, 0)
                case Axis.Z: return Vector3.forward; // Local z-axis (0, 0, 1)
                default: return Vector3.zero;        // Fallback, should not occur
            }
        }
    }
}