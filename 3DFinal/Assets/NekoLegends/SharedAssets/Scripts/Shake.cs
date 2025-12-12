using System.Collections;
using UnityEngine;

namespace NekoLegends
{
    public class Shake : MonoBehaviour
    {
        [Header("Shake Settings")]

        [Range(0.1f, 5f)] // Adjust the range as needed
        public float shakeAmount = 0.5f; // Magnitude of shake
        public float shakeDuration = 0.5f; // Duration of shake

        private void Start()
        {
            StartShake();
        }

        public void StartShake()
        {
            StartCoroutine(ShakeSelf());
        }

        private IEnumerator ShakeSelf()
        {
            Vector3 originalLocalPosition = transform.localPosition;

            float elapsed = 0.0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeAmount;
                float y = Random.Range(-1f, 1f) * shakeAmount;

                transform.localPosition = new Vector3(
                    originalLocalPosition.x + x,
                    originalLocalPosition.y + y,
                    originalLocalPosition.z
                );

                elapsed += Time.deltaTime;

                yield return null;
            }

            transform.localPosition = originalLocalPosition;
        }
    }
}
