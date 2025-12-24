using UnityEngine;
using System.Collections;

public class CameraSwitch : MonoBehaviour
{
    [Header("Target")]
    public Transform mainCamera;     // �� Main Camera �� Transform

    [Header("Angles (degrees)")]
    public float angleA = 0f;        // �_�l��
    public float angleB = -90f;      // ������

    [Header("Smooth")]
    public float duration = 0.25f;   // 0 = ����F>0 = ����

    bool toggled = false;            // false => angleA, true => angleB
    bool isRotating = false;

    [Header("Mode")]
    [SerializeField] private bool inOffice = true; // true = office (no waterScene)
    // Public accessor for other systems to know current camera mode
    public static bool IsInOffice { get; private set; } = true;

    [Header("Audio")]
    [SerializeField] private AudioClip waterSceneSfx = null;
    [SerializeField] [Range(0f,1f)] private float musicVolume = 0.5f;
    [SerializeField] private AudioClip waterBgSfx = null;
    [SerializeField] [Range(0f,1f)] private float bgMusicVolume = 0.5f;
    [SerializeField] private float musicFadeSpeed = 1.0f;
    private AudioSource _waterAudioSource;
    private AudioSource _waterBgAudioSource;
    private Coroutine _musicFadeCoroutineScene;
    private Coroutine _musicFadeCoroutineBg;

    void Reset()
    {
        // Ensure default main camera assigned in editor
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void Start()
    {
        if (mainCamera == null && Camera.main != null) mainCamera = Camera.main.transform;

        // initialize public static state
        IsInOffice = inOffice;

        if (waterSceneSfx != null)
        {
            _waterAudioSource = gameObject.AddComponent<AudioSource>();
            _waterAudioSource.clip = waterSceneSfx;
            _waterAudioSource.loop = true;
            _waterAudioSource.playOnAwake = false;
            _waterAudioSource.spatialBlend = 0f; // 2D music
            _waterAudioSource.volume = 0f;
        }

        if (waterBgSfx != null)
        {
            _waterBgAudioSource = gameObject.AddComponent<AudioSource>();
            _waterBgAudioSource.clip = waterBgSfx;
            _waterBgAudioSource.loop = true;
            _waterBgAudioSource.playOnAwake = false;
            _waterBgAudioSource.spatialBlend = 0f;
            _waterBgAudioSource.volume = 0f;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (!mainCamera || isRotating) return;

        toggled = !toggled;
        inOffice = !inOffice; // flip office mode when toggling camera
        // update public static state for other scripts
        IsInOffice = inOffice;
        UpdateMusic();

        float targetYaw = toggled ? angleB : angleA;

        if (duration <= 0f)
        {
            SetYawInstant(targetYaw);
        }
        else
        {
            StartCoroutine(RotateYawSmooth(targetYaw));
        }
    }

    void SetYawInstant(float targetYaw)
    {
        Vector3 e = mainCamera.eulerAngles;
        mainCamera.rotation = Quaternion.Euler(e.x, targetYaw, e.z);
    }

    IEnumerator RotateYawSmooth(float targetYaw)
    {
        isRotating = true;

        Quaternion start = mainCamera.rotation;
        Quaternion end = Quaternion.Euler(
            mainCamera.eulerAngles.x,
            targetYaw,
            mainCamera.eulerAngles.z
        );

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.0001f);
            mainCamera.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        mainCamera.rotation = end;
        isRotating = false;
    }

    private void UpdateMusic()
    {
        // Scene music
        if (_waterAudioSource != null)
        {
            float target = inOffice ? 0f : musicVolume;
            if (target > 0f && !_waterAudioSource.isPlaying)
            {
                _waterAudioSource.Play();
            }
            if (_musicFadeCoroutineScene != null) StopCoroutine(_musicFadeCoroutineScene);
            _musicFadeCoroutineScene = StartCoroutine(FadeMusicTo(_waterAudioSource, target, () => _musicFadeCoroutineScene = null));
        }

        // Background music
        if (_waterBgAudioSource != null)
        {
            float targetBg = inOffice ? 0f : bgMusicVolume;
            if (targetBg > 0f && !_waterBgAudioSource.isPlaying)
            {
                _waterBgAudioSource.Play();
            }
            if (_musicFadeCoroutineBg != null) StopCoroutine(_musicFadeCoroutineBg);
            _musicFadeCoroutineBg = StartCoroutine(FadeMusicTo(_waterBgAudioSource, targetBg, () => _musicFadeCoroutineBg = null));
        }
    }

    private IEnumerator FadeMusicTo(AudioSource source, float targetVolume, System.Action onComplete)
    {
        if (source == null) yield break;
        while (!Mathf.Approximately(source.volume, targetVolume))
        {
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, musicFadeSpeed * Time.deltaTime);
            yield return null;
        }
        if (Mathf.Approximately(source.volume, 0f) && source.isPlaying)
        {
            source.Stop();
        }
        onComplete?.Invoke();
    }
}
