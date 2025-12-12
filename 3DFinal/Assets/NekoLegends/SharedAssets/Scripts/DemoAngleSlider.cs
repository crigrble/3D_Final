using UnityEngine;
using UnityEngine.UI;

namespace NekoLegends
{
    public class DemoAngleSlider : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Slider sliderX;
        [SerializeField] private Slider sliderY;
        [SerializeField] private Slider sliderZ;

        private void Start()
        {
            if (sliderX != null) sliderX.onValueChanged.AddListener(UpdateRotation);
            if (sliderY != null) sliderY.onValueChanged.AddListener(UpdateRotation);
            if (sliderZ != null) sliderZ.onValueChanged.AddListener(UpdateRotation);

            if (targetTransform != null)
            {
                Vector3 currentRot = targetTransform.eulerAngles;
                if (sliderX != null) sliderX.value = currentRot.x;
                if (sliderY != null) sliderY.value = currentRot.y;
                if (sliderZ != null) sliderZ.value = currentRot.z;
            }
        }

        private void UpdateRotation(float value)
        {
            if (targetTransform == null) return;

            Vector3 newRot = targetTransform.eulerAngles;
            if (sliderX != null) newRot.x = sliderX.value;
            if (sliderY != null) newRot.y = sliderY.value;
            if (sliderZ != null) newRot.z = sliderZ.value;

            targetTransform.eulerAngles = newRot;
        }
    }
}