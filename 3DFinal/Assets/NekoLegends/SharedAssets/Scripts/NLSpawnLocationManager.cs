using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    public class NLSpawnLocationManager : MonoBehaviour
    {
        [Tooltip("Parent container holding all tracked objects as children")]
        public Transform lilypadContainer;

        [Tooltip("Minimum threshold boundary before an object gets recycled")]
        public Vector3 thresholdMin = new Vector3(-10f, -10f, -10f);

        [Tooltip("Maximum threshold boundary before an object gets recycled")]
        public Vector3 thresholdMax = new Vector3(10f, 10f, 10f);

        [Tooltip("Minimum spawn range for respawned objects")]
        public Vector3 spawnRangeMin = new Vector3(-5f, 0f, -5f);

        [Tooltip("Maximum spawn range for respawned objects")]
        public Vector3 spawnRangeMax = new Vector3(5f, 0f, 5f);

        [Tooltip("Randomize Y rotation when respawning objects")]
        public bool randomizeYRotation = false;

        [Tooltip("Minimum speed applied to PositionAnimator (if constant speed is used)")]
        public float minMovementSpeed = 0.5f;

        [Tooltip("Maximum speed applied to PositionAnimator (if constant speed is used)")]
        public float maxMovementSpeed = 2f;

        [Tooltip("Minimum distance between spawned objects")]
        public float minSpawnDistance = 1f;

        // tracked instances
        private List<Transform> objectsToTrack = new List<Transform>();

        private void Start()
        {
            if (lilypadContainer == null)
            {
                Debug.LogWarning("NLSpawnLocationManager: lilypadContainer not assigned!", this);
                return;
            }

            // populate list once
            objectsToTrack.Clear();
            foreach (Transform child in lilypadContainer)
            {
                if (child != null)
                {
                    objectsToTrack.Add(child);

                    var animator = child.GetComponent<NekoLegends.PositionAnimator>();
                    if (animator != null)
                    {
                        animator.playOnAwake = true;
                        animator.movementSpeed = Random.Range(minMovementSpeed, maxMovementSpeed);
                        DoRandomizeYRotation(child);
                    }
                }
            }
        }

        private void DoRandomizeYRotation(Transform obj)
        {
            if (randomizeYRotation)
            {
                Vector3 euler = obj.eulerAngles;
                euler.y = Random.Range(0f, 360f);
                obj.eulerAngles = euler;
            }
        }

        private Vector3 GetValidSpawnPosition()
        {
            int maxAttempts = 30; // Prevent infinite loops
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(spawnRangeMin.x, spawnRangeMax.x),
                    Random.Range(spawnRangeMin.y, spawnRangeMax.y),
                    Random.Range(spawnRangeMin.z, spawnRangeMax.z)
                );

                bool isValid = true;
                foreach (Transform obj in objectsToTrack)
                {
                    if (obj == null) continue;
                    float distance = Vector3.Distance(randomPos, obj.position);
                    if (distance < minSpawnDistance)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                    return randomPos;
            }

            // Fallback: Return a random position if no valid position is found
            Debug.LogWarning("Could not find valid spawn position after max attempts, using fallback position.");
            return new Vector3(
                Random.Range(spawnRangeMin.x, spawnRangeMax.x),
                Random.Range(spawnRangeMin.y, spawnRangeMax.y),
                Random.Range(spawnRangeMin.z, spawnRangeMax.z)
            );
        }

        private void LateUpdate()
        {
            for (int i = 0; i < objectsToTrack.Count; i++)
            {
                Transform obj = objectsToTrack[i];
                if (obj == null) continue;

                Vector3 pos = obj.position;
                // Check if object exits the threshold bounds in any direction
                if (pos.x < thresholdMin.x || pos.x > thresholdMax.x ||
                    pos.y < thresholdMin.y || pos.y > thresholdMax.y ||
                    pos.z < thresholdMin.z || pos.z > thresholdMax.z)
                {
                    // respawn
                    obj.position = GetValidSpawnPosition();

                    DoRandomizeYRotation(obj);

                    var animator = obj.GetComponent<NekoLegends.PositionAnimator>();
                    if (animator != null && animator.useConstantSpeed)
                    {
                        animator.movementSpeed = Random.Range(minMovementSpeed, maxMovementSpeed);
                        animator.RestartFromCurrentPosition();
                    }
                }
            }
        }
    }
}