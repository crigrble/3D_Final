using UnityEngine;

namespace NekoLegends
{
    public class DelayedShowTarget : MonoBehaviour
    {

        [Tooltip("Time in seconds to wait before showing the GameObject.")]
        public float delay = 2.0f;
        public GameObject gameObjectToEnable;
     

        void OnEnable()
        {
            if (gameObjectToEnable != null)
            {
               
                 gameObjectToEnable.SetActive(false);
                 StartCoroutine(ShowAfterDelay());
            }
        }

      

        private System.Collections.IEnumerator ShowAfterDelay()
        {
            // Wait for the specified delay
            yield return new WaitForSeconds(delay);

            // Check if the GameObject is not null before showing it
            if (gameObjectToEnable != null)
            {
                gameObjectToEnable.SetActive(true);
            }
        }
    }
}