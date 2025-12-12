using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    [DisallowMultipleComponent]
    public class RandomAudioPlayer : MonoBehaviour
    {
        [Header("Audio Clips")]
        [Tooltip("List of audio clips to choose from randomly.")]
        [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();

        [Header("Audio Source")]
        [Tooltip("AudioSource component to play sounds. If null, will find or create one.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Playback Settings")]
        [Tooltip("Minimum time delay between consecutive plays (seconds).")]
        [SerializeField] private float minDelay = 1f;

        [Tooltip("Maximum time delay between consecutive plays (seconds).")]
        [SerializeField] private float maxDelay = 5f;

        [Tooltip("Play on start automatically.")]
        [SerializeField] private bool playOnStart = false;

        [Tooltip("Loop the random playback sequence.")]
        [SerializeField] private bool loopSequence = false;

        [Range(0f, 1f)]
        [Tooltip("Volume for all played clips (0-1).")]
        [SerializeField] private float volume = 1f;

        [Tooltip("Play clips in random order without repeating until all have been played.")]
        [SerializeField] private bool shuffleMode = false;

        [Header("Random Pitch")]
        [Tooltip("Enable random pitch variation for each clip.")]
        [SerializeField] private bool randomPitch = false;

        [Range(0f, 2f)]
        [Tooltip("Maximum pitch variance from normal (1.0). Example: 0.1 = ±0.1 pitch range (0.9-1.1).")]
        [SerializeField] private float pitchVariance = 0.1f;

        [Header("3D Audio Settings")]
        [Tooltip("If true, this acts as a 3D sound source.")]
        [SerializeField] private bool is3D = true;

        [Tooltip("3D sound settings when is3D is enabled.")]
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [SerializeField] private float minDistance = 1f;

        [SerializeField] private float maxDistance = 50f;

        [Header("Logging")]
        [Tooltip("Log debug messages to console.")]
        [SerializeField] private bool logDebug = false;

        // Private variables
        private List<AudioClip> shuffledClips = new List<AudioClip>();
        private int currentShuffleIndex = 0;
        private Coroutine playbackCoroutine;
        private bool isPlaying = false;

        void Start()
        {
            InitializeAudioSource();
            
            if (playOnStart)
            {
                StartPlayback();
            }
        }

        /// <summary>
        /// Initialize or find AudioSource component
        /// </summary>
        private void InitializeAudioSource()
        {
            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.volume = volume;
                
                if (is3D)
                {
                    audioSource.spatialBlend = 1f;
                    audioSource.rolloffMode = rolloffMode;
                    audioSource.minDistance = minDistance;
                    audioSource.maxDistance = maxDistance;
                }
                else
                {
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        /// <summary>
        /// Start the random audio playback sequence
        /// </summary>
        [ContextMenu("Start Random Playback")]
        public void StartPlayback()
        {
            if (isPlaying)
            {
                if (logDebug) Debug.Log("[RandomAudioPlayer] Already playing, stopping current sequence first.");
                StopPlayback();
            }

            if (audioClips.Count == 0)
            {
                Debug.LogWarning("[RandomAudioPlayer] No audio clips assigned!");
                return;
            }

            isPlaying = true;
            InitializeShuffleIfNeeded();
            playbackCoroutine = StartCoroutine(PlaybackSequence());
        }

        /// <summary>
        /// Stop the random audio playback sequence
        /// </summary>
        [ContextMenu("Stop Random Playback")]
        public void StopPlayback()
        {
            isPlaying = false;
            
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }

            if (audioSource && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (logDebug) Debug.Log("[RandomAudioPlayer] Stopped playback sequence.");
        }

        /// <summary>
        /// Play a single random audio clip immediately
        /// </summary>
        [ContextMenu("Play Random Clip")]
        public void PlayRandomClip()
        {
            if (audioClips.Count == 0)
            {
                Debug.LogWarning("[RandomAudioPlayer] No audio clips assigned!");
                return;
            }

            AudioClip clipToPlay = GetRandomClip();
            PlayClip(clipToPlay);
        }

        /// <summary>
        /// Public method that can be called from Unity events (like footstep events)
        /// </summary>
        public void PlayRandomFootstepSound()
        {
            PlayRandomClip();
        }

        /// <summary>
        /// Add a new audio clip to the list
        /// </summary>
        /// <param name="clip">Audio clip to add</param>
        public void AddAudioClip(AudioClip clip)
        {
            if (clip != null && !audioClips.Contains(clip))
            {
                audioClips.Add(clip);
                if (logDebug) Debug.Log($"[RandomAudioPlayer] Added clip: {clip.name}");
            }
        }

        /// <summary>
        /// Remove an audio clip from the list
        /// </summary>
        /// <param name="clip">Audio clip to remove</param>
        public void RemoveAudioClip(AudioClip clip)
        {
            if (audioClips.Remove(clip))
            {
                if (logDebug) Debug.Log($"[RandomAudioPlayer] Removed clip: {clip.name}");
            }
        }

        /// <summary>
        /// Clear all audio clips from the list
        /// </summary>
        [ContextMenu("Clear All Clips")]
        public void ClearAudioClips()
        {
            audioClips.Clear();
            shuffledClips.Clear();
            currentShuffleIndex = 0;
            if (logDebug) Debug.Log("[RandomAudioPlayer] Cleared all audio clips.");
        }

        // ---- Private Methods ----

        private System.Collections.IEnumerator PlaybackSequence()
        {
            if (logDebug) Debug.Log("[RandomAudioPlayer] Starting playback sequence.");

            do
            {
                AudioClip clip = GetRandomClip();
                PlayClip(clip);
                
                // Wait for the clip to finish playing
                yield return new WaitForSeconds(clip.length);
                
                // Wait for random delay
                float delay = Random.Range(minDelay, maxDelay);
                yield return new WaitForSeconds(delay);
                
            } while (isPlaying && loopSequence);

            isPlaying = false;
            if (logDebug) Debug.Log("[RandomAudioPlayer] Playback sequence finished.");
        }

        private void PlayClip(AudioClip clip)
        {
            if (!clip) return;

            // Calculate pitch before playing
            float finalPitch = 1f;
            if (randomPitch && pitchVariance > 0f)
            {
                finalPitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            }

            // For footstep sounds and immediate playback, create a temporary AudioSource
            // This allows overlapping sounds and proper pitch control
            GameObject tempAudioObject = new GameObject("TemporaryFootstepSound");
            tempAudioObject.transform.position = transform.position;
            
            AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
            
            // Configure the temporary AudioSource for proper footstep behavior
            tempAudioSource.clip = clip;
            tempAudioSource.volume = volume;
            tempAudioSource.pitch = finalPitch;
            tempAudioSource.spatialBlend = is3D ? 1f : 0f;
            
            if (is3D)
            {
                tempAudioSource.rolloffMode = rolloffMode;
                tempAudioSource.minDistance = minDistance;
                tempAudioSource.maxDistance = maxDistance;
            }
            
            tempAudioSource.playOnAwake = false;
            tempAudioSource.loop = false;
            
            // Play the sound immediately
            tempAudioSource.Play();
            
            // Clean up after the sound finishes
            StartCoroutine(CleanupTemporaryAudioSource(tempAudioObject, clip.length + 0.1f));

            if (logDebug) Debug.Log($"[RandomAudioPlayer] Playing clip: {clip.name} with pitch {finalPitch:F2}");
        }

        private System.Collections.IEnumerator CleanupTemporaryAudioSource(GameObject audioObject, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (audioObject != null)
            {
                Destroy(audioObject);
            }
        }

        private AudioClip GetRandomClip()
        {
            if (shuffleMode)
            {
                return GetShuffledClip();
            }
            else
            {
                return audioClips[Random.Range(0, audioClips.Count)];
            }
        }

        private void InitializeShuffleIfNeeded()
        {
            if (shuffleMode)
            {
                shuffledClips = new List<AudioClip>(audioClips);
                ShuffleList(shuffledClips);
                currentShuffleIndex = 0;
            }
        }

        private AudioClip GetShuffledClip()
        {
            if (currentShuffleIndex >= shuffledClips.Count)
            {
                // Reset shuffle when we've played all clips
                ShuffleList(shuffledClips);
                currentShuffleIndex = 0;
            }

            AudioClip clip = shuffledClips[currentShuffleIndex];
            currentShuffleIndex++;
            return clip;
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // ---- Gizmos ----

        void OnDrawGizmosSelected()
        {
            if (is3D && audioSource)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, minDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, maxDistance);
            }
        }
    }
}