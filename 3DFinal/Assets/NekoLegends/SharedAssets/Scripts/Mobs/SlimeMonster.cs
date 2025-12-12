using UnityEngine;

namespace NekoLegends.Mobs
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SlimeMonster : MonoBehaviour
    {
        [Header("Expand Blendshape")]
        public bool animateExpand = false;
        [Tooltip("Speed of the expand animation (cycles per second)")]
        [Range(0f, 5f)]
        public float expandSpeed = 1f;

        [Header("Wiggle Legs Blendshape")]
        public bool animateWiggleLegs = false;
        [Tooltip("Speed of the wiggle legs animation (cycles per second)")]
        [Range(0f, 5f)]
        public float wiggleSpeed = 1f;

        private SkinnedMeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SkinnedMeshRenderer>();
            if (_renderer == null)
                Debug.LogError("SlimeMonster requires a SkinnedMeshRenderer on the same GameObject.");
        }

        private void Update()
        {
            if (_renderer == null) return;

            // Animate expand blendshape (index 0)
            if (animateExpand)
            {
                float weight = Mathf.PingPong(Time.time * expandSpeed, 1f) * 100f;
                _renderer.SetBlendShapeWeight(0, weight);
            }
            else
            {
                _renderer.SetBlendShapeWeight(0, 0f);
            }

            // Animate wiggle legs blendshape (index 1)
            if (animateWiggleLegs)
            {
                float weight = Mathf.PingPong(Time.time * wiggleSpeed, 1f) * 100f;
                _renderer.SetBlendShapeWeight(1, weight);
            }
            else
            {
                _renderer.SetBlendShapeWeight(1, 0f);
            }
        }
    }
}
