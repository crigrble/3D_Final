using UnityEngine;

public class RuntimeWebCamInput : MonoBehaviour
{
    [Header("Device")]
    [Tooltip("Leave empty to use the first available camera.")]
    public string deviceName = "";

    [Header("Request")]
    public int requestedWidth = 640;
    public int requestedHeight = 480;
    public int requestedFPS = 30;

    [Header("Debug")]
    public bool log = true;

    private WebCamTexture camTex;

    public WebCamTexture CamTexture => camTex;
    public Texture Texture => camTex;
    public bool IsPlaying => camTex != null && camTex.isPlaying;
    public int Width => camTex != null ? camTex.width : 0;
    public int Height => camTex != null ? camTex.height : 0;

    void OnEnable()
    {
        StartCamera();
    }

    void OnDisable()
    {
        StopCamera();
    }

    public void StartCamera()
    {
        if (camTex != null && camTex.isPlaying) return;

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("[RuntimeWebCamInput] No webcam found.");
            return;
        }

        string use = deviceName;
        if (string.IsNullOrEmpty(use))
            use = devices[0].name;

        camTex = new WebCamTexture(use, requestedWidth, requestedHeight, requestedFPS);
        camTex.Play();

        if (log)
            Debug.Log($"[RuntimeWebCamInput] Play device='{use}' req={requestedWidth}x{requestedHeight}@{requestedFPS}");
    }

    public void StopCamera()
    {
        if (camTex == null) return;
        if (camTex.isPlaying) camTex.Stop();
        camTex = null;
    }
}
