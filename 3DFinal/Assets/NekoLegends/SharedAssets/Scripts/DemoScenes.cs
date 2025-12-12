using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System.Reflection; // Added for reflection handling

namespace NekoLegends
{
    public class DemoScenes : MonoBehaviour
    {
        [SerializeField] protected Light directionalLight;
        [SerializeField] protected List<CameraDOFData> CameraDOFDatas;
        [SerializeField] protected List<CameraData> CameraDatas;
        [SerializeField] protected Transform BGTransform;

        [SerializeField] protected List<Transform> TargetPositions;
        [SerializeField] protected Button GlobalVolumnBtn;
        [SerializeField] protected Volume GlobalVolume;
        [SerializeField] protected Button LogoBtn;
        [SerializeField] public TextMeshProUGUI DescriptionText;
        [SerializeField] protected GameObject DemoUI;
        [SerializeField] protected bool hideMouse = false;
        [SerializeField] public AudioSource BGMSource, SFX;
        [SerializeField] protected bool continuousFlowMotion = false;

        private int currentIndex = 0;
        private bool isAnimating = false;
        protected float transitionSpeed = 1.0f;
        protected int currentCameraIndex = 0;
        protected bool isTransitioning = false;

        private DepthOfField _depthOfField;
        protected Dictionary<Button, UnityAction> buttonActions = new Dictionary<Button, UnityAction>();
        private Coroutine dofTransitionCoroutine;
        public bool isShowOutlines;
        private bool isManualDOFActive = false; // New flag to track manual DOF mode

        protected const string publisherSite = "https://assetstore.unity.com/publishers/82927";
        protected const string WebsiteURL = "https://nekolegends.com";
        protected int score;

        [System.Serializable]
        public struct CameraDOFData
        {
            public Transform CameraAngle;
            public float FocusDistance;
            public float Aperture;
            public float BackgroundScale;
        }

        #region Singleton
        public static DemoScenes Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType(typeof(DemoScenes)) as DemoScenes;

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        private static DemoScenes _instance;
        #endregion

        // Input system detection
        private bool useNewInputSystem = false;
        private Type keyboardType;

        protected virtual void Start()
        {
            if (!directionalLight)
            {
                Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }
            Cursor.visible = !hideMouse;
            if (GlobalVolume)
                GlobalVolume.profile.TryGet<DepthOfField>(out _depthOfField);

            // Detect input system
            keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (keyboardType != null)
            {
                PropertyInfo currentProp = keyboardType.GetProperty("current");
                if (currentProp != null)
                {
                    object keyboardInstance = currentProp.GetValue(null);
                    useNewInputSystem = keyboardInstance != null;
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (GlobalVolumnBtn)
                GlobalVolumnBtn.onClick.AddListener(GlobalVolumnBtnClicked);

            if (LogoBtn)
                LogoBtn.onClick.AddListener(LogoBtnClicked);

            foreach (var pair in buttonActions)
            {
                pair.Key.onClick.AddListener(pair.Value);
            }
        }

        protected virtual void OnDisable()
        {
            if (GlobalVolumnBtn)
                GlobalVolumnBtn.onClick.RemoveListener(GlobalVolumnBtnClicked);
            if (LogoBtn)
                LogoBtn.onClick.RemoveListener(LogoBtnClicked);

            foreach (var pair in buttonActions)
            {
                pair.Key.onClick.RemoveListener(pair.Value);
            }
        }

        protected void LogoBtnClicked()
        {
            DemoUI.SetActive(!DemoUI.activeSelf);
        }

        protected void GlobalVolumnBtnClicked()
        {
            GlobalVolume.enabled = !GlobalVolume.enabled;
        }

        protected void FlyToNextCameraHandler()
        {
            if (isTransitioning || isManualDOFActive) return; // Skip if manual DOF is active

            int nextCameraIndex = (currentCameraIndex + 1) % CameraDOFDatas.Count;
            CameraDOFData nextCameraData = CameraDOFDatas[nextCameraIndex];

            if (nextCameraData.FocusDistance != 0f || nextCameraData.Aperture != 0f)
            {
                SetDOF(nextCameraData.FocusDistance, nextCameraData.Aperture);
            }

            if (BGTransform)
            {
                float targetScale = (nextCameraData.BackgroundScale != 0) ? nextCameraData.BackgroundScale : 1f;
                SetBackgroundScale(targetScale);
            }

            StartCoroutine(TransitionToNextCameraAngle(CameraDOFDatas[currentCameraIndex].CameraAngle, nextCameraData.CameraAngle));
            currentCameraIndex = nextCameraIndex;
        }

        public void SetDOFImmediate(float in_focusDistance, float in_aperture)
        {
            if (_depthOfField == null) return;

            // Stop any ongoing DOF transition to ensure immediate update
            if (dofTransitionCoroutine != null)
            {
                StopCoroutine(dofTransitionCoroutine);
                dofTransitionCoroutine = null;
            }

            _depthOfField.focusDistance.value = in_focusDistance;
            _depthOfField.aperture.value = in_aperture;
            isManualDOFActive = true; // Mark as manual DOF active
        }

        public void SetDOF(float targetValue, float targetAperture, float delay = 0f)
        {
            if (_depthOfField == null || isManualDOFActive) return; // Skip if manual DOF is active

            float currentFocusDistance = _depthOfField.focusDistance.value;
            float currentAperture = _depthOfField.aperture.value;

            if (dofTransitionCoroutine != null)
            {
                StopCoroutine(dofTransitionCoroutine);
            }

            if (delay > 0f)
            {
                dofTransitionCoroutine = StartCoroutine(TransitionDOF(currentFocusDistance, currentAperture, targetValue, targetAperture, delay));
            }
            else
            {
                SetDOFImmediate(targetValue, targetAperture);
            }
        }

        public void SetDOFFromDataIndex(int index, float delay = 1f)
        {
            if (isManualDOFActive || CameraDOFDatas == null || CameraDOFDatas.Count == 0) return; // Skip if manual DOF is active

            float currentFocusDistance = _depthOfField.focusDistance.value;
            float currentAperture = _depthOfField.aperture.value;
            StartCoroutine(TransitionDOF(currentFocusDistance, currentAperture, CameraDOFDatas[index].FocusDistance, CameraDOFDatas[index].Aperture, delay));
        }

        // New method to allow DemoCameraController to signal manual DOF state
        public void SetManualDOFState(bool isManual)
        {
            isManualDOFActive = isManual;
        }

        protected void SetBackgroundScale(float targetScale)
        {
            float currentScale = BGTransform.transform.localScale.x;
            StartCoroutine(TransitionBackgroundScale(currentScale, targetScale));
        }

        public void ToggleLight()
        {

            if (directionalLight)
            {
                directionalLight.enabled = !directionalLight.enabled;
            }
            else
            {
                Debug.LogWarning("Directional Light not found!");
            }
        }

        protected IEnumerator TransitionToNextCameraAngle(Transform fromAngle, Transform toAngle)
        {
            isTransitioning = true;
            float timeElapsed = 0;
            Vector3 startPosition = fromAngle.position;
            Quaternion startRotation = fromAngle.rotation;
            Vector3 endPosition = toAngle.position;
            Quaternion endRotation = toAngle.rotation;

            while (timeElapsed < transitionSpeed)
            {
                float t = timeElapsed / transitionSpeed;
                t = continuousFlowMotion ? ContinuousEaseInOut(t) : Mathf.SmoothStep(0f, 1f, t);
                Camera.main.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                Camera.main.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.transform.position = endPosition;
            Camera.main.transform.rotation = endRotation;
            isTransitioning = false;
        }

        private float ContinuousEaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        protected IEnumerator TransitionDOF(float startValue, float startAperture, float endValue, float endAperture, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (isManualDOFActive) // Exit transition if manual DOF is activated
                {
                    dofTransitionCoroutine = null;
                    yield break;
                }

                float t = elapsedTime / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                _depthOfField.focusDistance.value = Mathf.Lerp(startValue, endValue, t);
                _depthOfField.aperture.value = Mathf.Lerp(startAperture, endAperture, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _depthOfField.focusDistance.value = endValue;
            _depthOfField.aperture.value = endAperture;

            dofTransitionCoroutine = null;
        }

        protected IEnumerator TransitionBackgroundScale(float startScale, float endScale)
        {
            float timeElapsed = 0;

            while (timeElapsed < transitionSpeed)
            {
                float t = timeElapsed / transitionSpeed;

                float currentScale = Mathf.Lerp(startScale, endScale, t);
                BGTransform.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            BGTransform.transform.localScale = new Vector3(endScale, endScale, endScale);
        }

        protected void AnimToNextDestination(Transform in_itemToMove)
        {
            if (!isAnimating)
            {
                int nextIndex = (currentIndex + 1) % TargetPositions.Count;
                StartCoroutine(MoveToTarget(in_itemToMove, TargetPositions[nextIndex].position));
                currentIndex = nextIndex;
            }
        }

        public void SetDOFToTarget(Transform target, Vector3 cameraEndPosition, float targetAperture, float duration)
        {
            if (_depthOfField == null || target == null || isManualDOFActive) return;

            float startFocusDistance = _depthOfField.focusDistance.value;
            float startAperture = _depthOfField.aperture.value;
            float endFocusDistance = Vector3.Distance(cameraEndPosition, target.position);

            if (dofTransitionCoroutine != null)
            {
                StopCoroutine(dofTransitionCoroutine);
            }

            if (duration > 0f)
            {
                dofTransitionCoroutine = StartCoroutine(TransitionDOFToTarget(
                    startFocusDistance, endFocusDistance, startAperture, targetAperture, duration));
            }
            else
            {
                SetDOFImmediate(endFocusDistance, targetAperture);
            }
        }

        protected IEnumerator TransitionDOFToTarget(
            float startFocusDistance, float endFocusDistance, float startAperture, float endAperture, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (isManualDOFActive) // Exit transition if manual DOF is activated
                {
                    dofTransitionCoroutine = null;
                    yield break;
                }

                float t = elapsedTime / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                float currentFocusDistance = Mathf.Lerp(startFocusDistance, endFocusDistance, t);
                float currentAperture = Mathf.Lerp(startAperture, endAperture, t);

                _depthOfField.focusDistance.value = currentFocusDistance;
                _depthOfField.aperture.value = currentAperture;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _depthOfField.focusDistance.value = endFocusDistance;
            _depthOfField.aperture.value = endAperture;

            dofTransitionCoroutine = null;
        }

        IEnumerator MoveToTarget(Transform itemToMove, Vector3 endPosition)
        {
            isAnimating = true;
            float duration = 1f;
            float elapsedTime = 0f;

            Vector3 startPosition = itemToMove.position;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float smoothedT = Mathf.SmoothStep(0.0f, 1.0f, t);

                itemToMove.position = Vector3.Lerp(startPosition, endPosition, smoothedT);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            itemToMove.position = endPosition;
            isAnimating = false;
        }

        public void SetDescriptionText(string inText)
        {
            if (DescriptionText)
                DescriptionText.SetText(inText);
        }

        protected void RegisterButtonAction(Button button, UnityAction action)
        {
            buttonActions[button] = action;
        }

        protected void HideObjects(List<GameObject> in_OBJ)
        {
            foreach (var item in in_OBJ)
            {
                item.SetActive(false);
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            SFX.clip = clip;
            SFX.Play();
        }

        public void PlayRandomSFX(AudioClip[] clip)
        {
            int randomIndex = UnityEngine.Random.Range(0, clip.Length);
            SFX.clip = clip[randomIndex];
            SFX.Play();
        }

        public virtual void AssetBtnHandler()
        {
            Application.OpenURL(publisherSite);
        }

        public virtual void LogoBtnHandler()
        {
            Application.OpenURL(WebsiteURL);
        }

        public virtual void LaunchURL(string in_url)
        {
            Application.OpenURL(in_url);
        }

        public virtual void ToggleBackgroundCanvas()
        {
            BGTransform.gameObject.SetActive(!BGTransform.gameObject.activeSelf);
        }

        public virtual void StartGameBtnHandler() { }

        public virtual void LoadMainMenu()
        {
            DemoUI.SetActive(true);
        }

        protected virtual void Update()
        {
            if (useNewInputSystem)
            {
                if (IsEscapePressedNew())
                {
                    QuitApplication();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    QuitApplication();
                }
            }
        }

        private bool IsEscapePressedNew()
        {
            if (keyboardType == null) return false;

            PropertyInfo currentProp = keyboardType.GetProperty("current");
            object keyboardInstance = currentProp.GetValue(null);
            if (keyboardInstance == null) return false;

            PropertyInfo escapeKeyProp = keyboardType.GetProperty("escapeKey");
            object escapeKey = escapeKeyProp.GetValue(keyboardInstance);

            PropertyInfo wasPressedThisFrameProp = escapeKey.GetType().GetProperty("wasPressedThisFrame");
            return (bool)wasPressedThisFrameProp.GetValue(escapeKey);
        }

        private void OnApplicationQuit()
        {
            QuitApplication();
        }

        public void HideUI()
        {
            DemoUI.SetActive(!DemoUI.activeSelf);
        }

        private void QuitApplication()
        {
            StopAllCoroutines();
            Resources.UnloadUnusedAssets();
            Application.Quit();
        }
    }
}