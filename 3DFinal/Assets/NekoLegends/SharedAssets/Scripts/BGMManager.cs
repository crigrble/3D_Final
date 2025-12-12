using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace NekoLegends
{
    [RequireComponent(typeof(AudioSource))]
    public class BGMManager : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> bgmClips; // List of BGM clips to switch between
        [SerializeField] private AudioSource audioSource;  // The AudioSource to play the BGM
        [SerializeField] private TextMeshProUGUI bgmNameText; // TextMeshPro UI element to display the current BGM name
        [SerializeField] private Button changeMusicButton; // Button to change the music
        [SerializeField] private Shake shakeMusicIcon;

        private int currentBGMIndex = 0;

        void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (bgmClips != null && bgmClips.Count > 0 && audioSource != null)
            {
                audioSource.clip = bgmClips[currentBGMIndex];
                audioSource.Play();
                UpdateBGMNameUI();
            }
            else
            {
                Debug.LogWarning("BGMManager: AudioSource or BGM clips are not set properly.");
            }

            if (changeMusicButton != null)
            {
                changeMusicButton.onClick.AddListener(ChangeBGM);
            }
            else
            {
                Debug.LogWarning("BGMManager: ChangeMusic button is not set properly.");
            }
        }

        // Method to be called when the ChangeBGM button is pressed
        public void ChangeBGM()
        {
            if (bgmClips == null || bgmClips.Count == 0 || audioSource == null)
            {
                Debug.LogWarning("BGMManager: Unable to change BGM due to missing references.");
                return;
            }

            currentBGMIndex = (currentBGMIndex + 1) % bgmClips.Count;
            audioSource.clip = bgmClips[currentBGMIndex];
            audioSource.Play();
            UpdateBGMNameUI();
            shakeMusicIcon.StartShake();
        }

        //used in end game GUI without changing in main menu
        public void ChangeBGM(TextMeshProUGUI newTextLocation)
        {
            if (bgmClips == null || bgmClips.Count == 0 || audioSource == null)
            {
                Debug.LogWarning("BGMManager: Unable to change BGM due to missing references.");
                return;
            }

            currentBGMIndex = (currentBGMIndex + 1) % bgmClips.Count;
            audioSource.clip = bgmClips[currentBGMIndex];
            audioSource.Play();
            newTextLocation.text = bgmClips[currentBGMIndex].name;
        }


        // Method to update the BGM name in the TextMeshPro UI
        private void UpdateBGMNameUI()
        {
            if (bgmNameText != null && bgmClips != null && currentBGMIndex < bgmClips.Count)
            {
                bgmNameText.text = bgmClips[currentBGMIndex].name;
            }
            else
            {
                Debug.LogWarning("BGMManager: Unable to update BGM name UI due to missing references.");
            }
        }
    }
}
