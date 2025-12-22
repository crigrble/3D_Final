using UnityEngine;

public class CameraRotateToggle : MonoBehaviour
{
    public float toggleAngle = -90f;

    private bool isRotated = false;
    private float originalY;

    void Start()
    {
        originalY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isRotated = !isRotated;

            float targetY = isRotated ? toggleAngle : originalY;

            Vector3 euler = transform.eulerAngles;
            euler.y = targetY;
            transform.eulerAngles = euler;
        }
    }
}
