using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    public class SpawnItemsFromScene : MonoBehaviour
    {
        [Header("Prefab Setup")]
        [SerializeField] List<GameObject> prefabs = new();
        [SerializeField] bool autoIncrement = false;

        [Header("Continuous Spawn Settings")]
        [SerializeField] bool continuousSpawn = false;
        [SerializeField] float spawnInterval = 0.5f;

        [Header("Randomization Settings")]
        [SerializeField] bool randomizePosition = false;
        [Tooltip("Max offset on each axis from prefab's original position.")]
        [SerializeField] Vector3 positionRange = Vector3.zero;
        [SerializeField] bool randomizeRotation = false;
        [Tooltip("Min Euler angles for random rotation.")]
        [SerializeField] Vector3 minRotation = Vector3.zero;
        [Tooltip("Max Euler angles for random rotation.")]
        [SerializeField] Vector3 maxRotation = new Vector3(360, 360, 360);

        [Header("Audio Setup")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] List<AudioClip> spawnSFXClips = new();

        int currentIndex;
        Coroutine _spawnLoop;
        List<GameObject> spawnedObjects = new List<GameObject>();

        void Awake()
        {
            foreach (var p in prefabs)
                p.SetActive(false);
        }

        void OnDestroy()
        {
            CleanUp();
        }

        public void Spawn()
        {
            if (prefabs.Count == 0) return;

            if (continuousSpawn)
            {
                if (_spawnLoop == null)
                    _spawnLoop = StartCoroutine(SpawnLoop());
            }
            else
            {
                SpawnOne();
            }
        }

        public void StopSpawning()
        {
            if (_spawnLoop != null)
            {
                StopCoroutine(_spawnLoop);
                _spawnLoop = null;
            }
        }

        public void ToggleContinuousSpawn()
        {
            if (prefabs.Count == 0) return;

            continuousSpawn = !continuousSpawn;
            if (continuousSpawn && _spawnLoop == null)
                _spawnLoop = StartCoroutine(SpawnLoop());
            else if (!continuousSpawn && _spawnLoop != null)
            {
                StopCoroutine(_spawnLoop);
                _spawnLoop = null;
            }
        }

        IEnumerator SpawnLoop()
        {
            while (true)
            {
                SpawnOne();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        void SpawnOne()
        {
            // Determine spawn position
            Vector3 spawnPos = prefabs[currentIndex].transform.position;
            if (randomizePosition)
                spawnPos += new Vector3(
                    Random.Range(-positionRange.x, positionRange.x),
                    Random.Range(-positionRange.y, positionRange.y),
                    Random.Range(-positionRange.z, positionRange.z)
                );

            // Determine spawn rotation
            Quaternion spawnRot = prefabs[currentIndex].transform.rotation;
            if (randomizeRotation)
            {
                Vector3 randomEuler = new Vector3(
                    Random.Range(minRotation.x, maxRotation.x),
                    Random.Range(minRotation.y, maxRotation.y),
                    Random.Range(minRotation.z, maxRotation.z)
                );
                spawnRot = Quaternion.Euler(randomEuler);
            }

            // Instantiate prefab
            var obj = Instantiate(prefabs[currentIndex], spawnPos, spawnRot);
            obj.SetActive(true);
            spawnedObjects.Add(obj);

            // Play random SFX
            if (audioSource != null && spawnSFXClips.Count > 0)
            {
                AudioClip clip = spawnSFXClips[Random.Range(0, spawnSFXClips.Count)];
                audioSource.PlayOneShot(clip);
            }

            // Handle auto-increment index and UI update
            if (autoIncrement)
            {
                currentIndex = (currentIndex + 1) % prefabs.Count;
                if (DemoScenes.Instance != null)
                    DemoScenes.Instance.SetDescriptionText(prefabs[currentIndex].name);
            }
        }

        /// <summary>
        /// Destroy all spawned objects, ignoring already-destroyed ones.
        /// </summary>
        public void CleanUp()
        {
            // Stop continuous spawn to prevent new objects during cleanup
            StopSpawning();

            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                var obj = spawnedObjects[i];
                if (obj != null)
                    Destroy(obj);
                spawnedObjects.RemoveAt(i);
            }
        }

        public void Next()
        {
            if (prefabs.Count == 0) return;
            currentIndex = (currentIndex + 1) % prefabs.Count;
            if (DemoScenes.Instance != null)
                DemoScenes.Instance.SetDescriptionText(prefabs[currentIndex].name);
        }

        public void Prev()
        {
            if (prefabs.Count == 0) return;
            currentIndex = (currentIndex - 1 + prefabs.Count) % prefabs.Count;
            if (DemoScenes.Instance != null)
                DemoScenes.Instance.SetDescriptionText(prefabs[currentIndex].name);
        }

        /// <summary>
        /// Spawn the prefab at the specified index immediately.
        /// </summary>
        public void SpawnIndex(int index)
        {
            if (prefabs.Count == 0 || index < 0 || index >= prefabs.Count)
                return;

            currentIndex = index;
            SpawnOne();
        }
    }
}
