using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    public enum VFXDestroyEffectType
    {
        None,
        Scale,
        Jitter
    }

    public class VFXSpawner : MonoBehaviour
    {
        [Tooltip("All prefabs should be inactive in the scene hierarchy.")]
        public List<GameObject> vfxPrefabs = new List<GameObject>();

        [Tooltip("Default VFX index to use when no index is specified.")]
        public int defaultIndex = 0;

        [Tooltip("How long the VFX instance should stay active before being destroyed.")]
        public float vfxDuration = 2f;

        [Tooltip("How the VFX behaves before destruction.")]
        public VFXDestroyEffectType destroyEffect = VFXDestroyEffectType.None;

        [Tooltip("Optional: parent container to spawn VFX under.")]
        public Transform spawnContainer;

        [Tooltip("Used for Grow or Shrink effect: starting scale.")]
        public Vector3 scaleStart = Vector3.one;

        [Tooltip("Used for Grow or Shrink effect: ending scale.")]
        public Vector3 scaleEnd = Vector3.zero;

        [Tooltip("Randomize rotation on X, Y, Z axes when spawning.")]
        public bool randomRotation = false;

        public void PlayVFX(int index = -1)
        {
            if (vfxPrefabs == null || vfxPrefabs.Count == 0)
            {
                Debug.LogWarning("VFXSpawner: No VFX prefabs assigned.");
                return;
            }

            if (index < 0 || index >= vfxPrefabs.Count)
                index = defaultIndex;

            GameObject prefab = vfxPrefabs[index];
            if (prefab == null)
            {
                Debug.LogWarning($"VFXSpawner: VFX prefab at index {index} is null.");
                return;
            }

            // Instantiate at prefab's position/rotation under the container
            GameObject instance = Instantiate(
                prefab,
                prefab.transform.position,
                prefab.transform.rotation,
                spawnContainer
            );

            // Apply random rotation if enabled
            if (randomRotation)
            {
                float rotX = Random.Range(0f, 360f);
                float rotY = Random.Range(0f, 360f);
                float rotZ = Random.Range(0f, 360f);
                instance.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            }

            instance.SetActive(true);
            prefab.SetActive(false);

            // Handle destruction effects
            switch (destroyEffect)
            {
                case VFXDestroyEffectType.Scale:
                    StartCoroutine(ScaleEffectAndDestroy(instance, vfxDuration, scaleStart, scaleEnd));
                    break;
                case VFXDestroyEffectType.Jitter:
                    StartCoroutine(JitterAndDestroy(instance, vfxDuration));
                    break;
                default:
                    Destroy(instance, vfxDuration);
                    break;
            }
        }

        private IEnumerator ScaleEffectAndDestroy(GameObject obj, float duration, Vector3 fromScale, Vector3 toScale)
        {
            float time = 0f;
            obj.transform.localScale = fromScale;

            while (time < duration)
            {
                float t = time / duration;
                float eased = EaseOutCubic(t);
                obj.transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                time += Time.deltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        private IEnumerator JitterAndDestroy(GameObject obj, float duration)
        {
            float time = 0f;
            Vector3 originalPos = obj.transform.position;

            while (time < duration)
            {
                float magnitude = 0.05f;
                obj.transform.position = originalPos + Random.insideUnitSphere * magnitude;
                time += Time.deltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        private float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }
    }
}
