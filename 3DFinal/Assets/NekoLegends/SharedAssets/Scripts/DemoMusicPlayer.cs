using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for EventTrigger
using TMPro;
using System.Collections.Generic;

namespace NekoLegends
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Audio Clips")]
        [Tooltip("List of audio clips for the player to cycle through.")]
        public List<AudioClip> audioClips = new List<AudioClip>();

        [Header("UI References")]
        public Button playPauseButton;
        public Button nextButton;
        public Button previousButton;
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI durationText;
        public Slider progressSlider;

        [Tooltip("Assign Transforms (3D GameObjects) for the bars you want to animate.")]
        public Transform[] barVisuals;

        public float visualScale = 1f; //1 for web version

        private AudioSource audioSource;
        private int currentTrackIndex = 0;
        private bool isPlaying = false;
        private bool isDragging = false; // True while user is dragging the slider

        // You can tweak these gains for each frequency "band"
        private float[] barGains = { 0.07f, 0.07f, .35f, 3f, 7f, 13f, 25f, .2f, .4f, .8f };
        
        // We'll use 512 samples for our FFT. You can adjust (e.g. 1024, 2048, etc.).
        private int sampleSize = 512;

        // For splitting frequency ranges into 10 bars, we define boundaries.
        // The last element in 'boundaries' is one past the final index so it must match 'sampleSize - 1' or so.
        private int[] boundaries = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 384 ,512 };

        private void Awake()
        {
            // Get or add AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            // Slider setup
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.onValueChanged.AddListener(OnSliderValueChanged);

                // --- Add EventTrigger entries in code so we don't have to set them in the Inspector ---
                EventTrigger eventTrigger = progressSlider.gameObject.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = progressSlider.gameObject.AddComponent<EventTrigger>();

                // PointerDown -> StartDragging()
                EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
                pointerDownEntry.eventID = EventTriggerType.PointerDown;
                pointerDownEntry.callback.AddListener((data) => { StartDragging(); });
                eventTrigger.triggers.Add(pointerDownEntry);

                // PointerUp -> EndDragging()
                EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
                pointerUpEntry.eventID = EventTriggerType.PointerUp;
                pointerUpEntry.callback.AddListener((data) => { EndDragging(); });
                eventTrigger.triggers.Add(pointerUpEntry);
            }

            // Hook up button listeners
            if (playPauseButton != null) playPauseButton.onClick.AddListener(TogglePlayPause);
            if (nextButton != null) nextButton.onClick.AddListener(NextTrack);
            if (previousButton != null) previousButton.onClick.AddListener(PreviousTrack);
        }

        private void OnDisable()
        {
            if (progressSlider != null)
            {
                progressSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }

            if (playPauseButton != null) playPauseButton.onClick.RemoveListener(TogglePlayPause);
            if (nextButton != null) nextButton.onClick.RemoveListener(NextTrack);
            if (previousButton != null) previousButton.onClick.RemoveListener(PreviousTrack);
        }

        private void Start()
        {
            // If we have at least one clip, start playing from the first track
            if (audioClips.Count > 0)
            {
                LoadTrack(currentTrackIndex);
            }

            UpdateUI();
        }

        private void Update()
        {
            // If there's a clip and we're not dragging, update the slider & duration
            if (audioSource.clip != null && !isDragging)
            {
                float currentTime = audioSource.time;
                float totalTime = audioSource.clip.length;

                // Update slider
                if (progressSlider != null && totalTime > 0f)
                {
                    float sliderValue = currentTime / totalTime;
                    // Use SetValueWithoutNotify so it doesn't call OnSliderValueChanged
                    progressSlider.SetValueWithoutNotify(sliderValue);
                }

                // Update duration text
                if (durationText != null)
                {
                    durationText.text = FormatTime(currentTime) + " / " + FormatTime(totalTime);
                }

                // If the clip finished, automatically go to the next track
                if (!audioSource.isPlaying && currentTime >= totalTime)
                {
                    NextTrack();
                }
            }

            //hack fix to prevent webgl build from lowering volume
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f; // ensure 2D
        }

        private void LoadTrack(int index)
        {
            if (index < 0 || index >= audioClips.Count) return;

            audioSource.clip = audioClips[index];
            audioSource.Stop();
            audioSource.Play();
            isPlaying = true;

            if (songTitleText != null)
                songTitleText.text = audioSource.clip.name;

            UpdateUI();
        }

        private void TogglePlayPause()
        {
            if (!audioSource.clip) return;

            if (isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
            }
            else
            {
                audioSource.UnPause();
                isPlaying = true;
            }

            UpdateUI();
        }

        public void NextTrack()
        {
            if (audioClips.Count == 0) return;
            currentTrackIndex = (currentTrackIndex + 1) % audioClips.Count;
            LoadTrack(currentTrackIndex);
        }

        public void PreviousTrack()
        {
            if (audioClips.Count == 0) return;
            currentTrackIndex--;
            if (currentTrackIndex < 0)
                currentTrackIndex = audioClips.Count - 1;

            LoadTrack(currentTrackIndex);
        }

        /// <summary>
        /// Called whenever the slider's value changes (both user drag and code).
        /// Only seek in audio if the user is actively dragging.
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            if (audioSource.clip == null) return;

            if (isDragging)
            {
                float newTime = value * audioSource.clip.length;
                audioSource.time = newTime;

                // Optionally, resume playback if paused:
                if (!isPlaying)
                {
                    audioSource.Play();
                    isPlaying = true;
                    UpdateUI();
                }
            }
        }

        private void UpdateUI()
        {
            if (playPauseButton != null)
            {
                TextMeshProUGUI btnText = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = isPlaying ? "||" : ">";
                }
            }
        }

        private string FormatTime(float timeSeconds)
        {
            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeSeconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Called when the user starts dragging the slider handle.
        /// </summary>
        private void StartDragging()
        {
            isDragging = true;
        }

        /// <summary>
        /// Called when the user releases the slider handle.
        /// </summary>
        private void EndDragging()
        {
            isDragging = false;
        }

        /// <summary>
        /// Reads a block of time-domain samples from the AudioClip, 
        /// runs an FFT, then uses the frequency magnitudes to update the bar visuals.
        /// </summary>
        private void UpdateVisualizer()
        {
            // Safety checks
            if (barVisuals == null || barVisuals.Length == 0) return;
            if (!audioSource.isPlaying) return;
            if (audioSource.clip == null) return;
            if (sampleSize <= 0) return;
            if (boundaries.Length != barVisuals.Length + 1)
            {
                Debug.LogWarning("Boundaries array length must be barVisuals.Length + 1.");
                return;
            }

            // We'll read 'sampleSize' samples around the current play position.
            // Convert timeSamples to a position in the clip. 
            int startIndex = audioSource.timeSamples;
            // Make sure we don't read beyond the clip length.
            if (startIndex + sampleSize > audioSource.clip.samples)
            {
                // If we're near the end, just clamp to avoid going out of range.
                startIndex = Mathf.Max(0, audioSource.clip.samples - sampleSize);
            }

            // Get time-domain samples
            float[] samples = new float[sampleSize];
            audioSource.clip.GetData(samples, startIndex);

            // Apply a simple window (optional but recommended for FFT). E.g. Hanning:
            for (int i = 0; i < sampleSize; i++)
            {
                // Hanning window: w(n) = 0.5 * (1 - cos(2pi*n/(N-1)))
                float window = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (sampleSize - 1)));
                samples[i] *= window;
            }

            // Perform FFT on these samples
            float[] freqSpectrum = ComputeFFT(samples);

            // Now freqSpectrum[i] holds the magnitude of frequency bin i.
            // We'll sum bins according to 'boundaries' just like we did with GetSpectrumData.

            for (int barIndex = 0; barIndex < barVisuals.Length; barIndex++)
            {
                float sum = 0f;
                int startB = boundaries[barIndex];
                int endB = boundaries[barIndex + 1];

                // Clamp endB so we don't go out of freqSpectrum range
                endB = Mathf.Min(endB, freqSpectrum.Length - 1);

                for (int j = startB; j <= endB; j++)
                {
                    sum += freqSpectrum[j];
                }

                float avg = sum / (endB - startB + 1);
                
                // You can convert to decibels if you like, or just use avg directly
                // For demonstration, let's scale by barGains + a visualScale factor
                float height = avg * visualScale *barGains[barIndex];
                float clampedHeight = Mathf.Clamp(height, 0.5f, 20f);

                // Smooth the transition
                float oldHeight = barVisuals[barIndex].localScale.y;
                float newHeight = Mathf.Lerp(oldHeight, clampedHeight, Time.deltaTime * 8f);

                barVisuals[barIndex].localScale = new Vector3(1f, newHeight, 1f);
            }
        }

        /// <summary>
        /// Computes the FFT of the given time-domain samples (float[]).
        /// Returns an array of magnitudes (frequency domain).
        /// </summary>
        private float[] ComputeFFT(float[] data)
        {
            int n = data.Length;
            // Real and Imaginary arrays
            float[] real = new float[n];
            float[] imag = new float[n];

            // Copy data into real part
            for (int i = 0; i < n; i++)
            {
                real[i] = data[i];
                imag[i] = 0f;
            }

            // 1) Bit-reversal
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (i < j)
                {
                    // Swap real[i], real[j]
                    float tempReal = real[i];
                    real[i] = real[j];
                    real[j] = tempReal;
                    // Swap imag[i], imag[j]
                    float tempImag = imag[i];
                    imag[i] = imag[j];
                    imag[j] = tempImag;
                }
                int m = n >> 1;
                while ((j >= m) && (m > 0))
                {
                    j -= m;
                    m >>= 1;
                }
                j += m;
            }

            // 2) Cooley-Tukey
            for (int size = 2; size <= n; size <<= 1)
            {
                int halfSize = size >> 1;
                float phaseStep = -2f * Mathf.PI / size;
                float cosStep = Mathf.Cos(phaseStep);
                float sinStep = Mathf.Sin(phaseStep);

                for (int group = 0; group < n; group += size)
                {
                    float currentCos = 1f;
                    float currentSin = 0f;

                    for (int pair = 0; pair < halfSize; pair++)
                    {
                        int match = group + pair + halfSize;

                        // temp real/imag
                        float tReal = (real[match] * currentCos) - (imag[match] * currentSin);
                        float tImag = (real[match] * currentSin) + (imag[match] * currentCos);

                        real[match] = real[group + pair] - tReal;
                        imag[match] = imag[group + pair] - tImag;

                        real[group + pair] += tReal;
                        imag[group + pair] += tImag;

                        // Advance the phase
                        float oldCos = currentCos;
                        float oldSin = currentSin;
                        currentCos = (oldCos * cosStep) - (oldSin * sinStep);
                        currentSin = (oldCos * sinStep) + (oldSin * cosStep);
                    }
                }
            }

            // 3) Compute magnitudes
            float[] magnitudes = new float[n];
            for (int i = 0; i < n; i++)
            {
                float mag = Mathf.Sqrt(real[i] * real[i] + imag[i] * imag[i]);
                magnitudes[i] = mag;
            }

            return magnitudes;
        }
    }
}
