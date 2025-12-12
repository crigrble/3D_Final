using System.Diagnostics;
using UnityEngine;

namespace NekoLegends
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicWaterMark : MonoBehaviour
    {

        [Tooltip("The audio clip for the watermark (e.g., a 'meow' sound).")]
        public AudioClip[] watermarkClips;

        [Tooltip("Time interval (in seconds) between watermark sounds.")]
        public float watermarkInterval = 15f;

        private float timer;

        private void Update()
        {
           
            // Increment the timer while the music is playing
            timer += Time.deltaTime;

            if (timer >= watermarkInterval)
            {
                PlayWatermark();
                timer = 0f; // Reset the timer
            }
            
        }


        private void PlayWatermark()
        {
            if (DemoScenes.Instance.SFX == null)
            {
                DemoScenes.Instance.SFX = this.gameObject.GetComponent<AudioSource>();
            }

            if (DemoScenes.Instance.SFX != null && watermarkClips != null && watermarkClips.Length > 0)
            {
                DemoScenes.Instance.PlayRandomSFX(watermarkClips);
            }
        }
    }
}
