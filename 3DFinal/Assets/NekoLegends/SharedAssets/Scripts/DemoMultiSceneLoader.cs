using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekoLegends
{
    public class DemoMultiSceneLoader : MonoBehaviour
    {
        public static DemoMultiSceneLoader Instance;

        [Tooltip("List of scene names to load in sequence.")]
        public List<string> sceneNames = new List<string>();

        [Tooltip("Time in seconds before switching to the next scene.")]
        public float sceneSwitchInterval = 5f;

        private int currentSceneIndex = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Register event for detecting when a scene is fully loaded
                SceneManager.sceneLoaded += OnSceneLoaded;

                LoadFirstScene();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadFirstScene()
        {
            if (sceneNames.Count > 0)
            {
                string firstScene = sceneNames[0];

                if (SceneExists(firstScene))
                {
                    SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
                    currentSceneIndex = 0;
                    // Start looping scene changes
                    StartCoroutine(ScheduleNextScene());
                }
                else
                {
                    Debug.LogWarning($"Scene '{firstScene}' not found in Build Settings!");
                }
            }
            else
            {
                Debug.LogWarning("No scenes assigned in DemoMultiSceneLoader!");
            }
        }

        private IEnumerator ScheduleNextScene()
        {
            while (true)
            {
                yield return new WaitForSeconds(sceneSwitchInterval);

                // Move to the next scene in the list
                currentSceneIndex++;
                if (currentSceneIndex >= sceneNames.Count)
                {
                    currentSceneIndex = 0;
                }

                string nextScene = sceneNames[currentSceneIndex];
                if (SceneExists(nextScene))
                {
                    SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogWarning($"Scene '{nextScene}' not found! Stopping scene cycle.");
                    yield break;
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Wait one frame to ensure all objects are initialized before updating description
            StartCoroutine(UpdateSceneDescription(scene.name));
        }

        private IEnumerator UpdateSceneDescription(string sceneName)
        {
            yield return null; // Wait one frame
            if (DemoScenes.Instance != null)
            {
                DemoScenes.Instance.SetDescriptionText($"Now Playing: {sceneName}");
            }
            else
            {
                Debug.LogWarning("DemoScenes.Instance is null. Ensure DemoScenes is in the scene.");
            }
        }

        private bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (scenePath.Contains(sceneName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
