using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

namespace NekoLegends
{
    [DisallowMultipleComponent]
    public class ToggleShaderFeature : MonoBehaviour
    {
        [Header("Search Scope")]
        [Tooltip("Root to search under. If null, uses this GameObject.")]
        public Transform searchRoot;

        [Tooltip("Include inactive objects when searching.")]
        public bool includeInactive = true;

        [Header("Target")]
        [Tooltip("Shader name to match.")]
        public string shaderName = "Neko Legends/Card Depth Parallax";

        [Tooltip("Master on/off property on that shader.")]
        public string masterProperty = "_FXEnabled";

        [Header("Permanent Material Change")]
        [Tooltip("If ON, edits shared materials (global change). If OFF, edits per-renderer instances.")]
        public bool editMaterialPermanent = false;

        [Header("Status UI (TextMeshPro)")]
        [Tooltip("Optional: A TMP_Text (e.g., TextMeshProUGUI) to show ON/OFF status.")]
        [SerializeField] public TextMeshProUGUI statusLabel;
        public string onText = "Feature ON";
        public string offText = "Feature OFF";
        public Color onColor = new Color(0.25f, 1f, 0.5f);
        public Color offColor = new Color(1f, 0.4f, 0.4f);
        [Tooltip("Update the label once on Start based on current detected state.")]
        public bool updateLabelOnStart = true;

        [Header("Logging")]
        public bool logChanges = false;

        void Start()
        {
            if (updateLabelOnStart)
            {
                bool? state = ProbeCurrentState();
                if (state.HasValue) UpdateStatusLabel(state.Value);
            }
        }

        /// <summary>
        /// Button hook: toggles all matching materials under searchRoot.
        /// </summary>
        [ContextMenu("Toggle Shader Feature")]
        public void ToggleShaderFeatureOnOff()
        {
            var root = searchRoot ? searchRoot : transform;

            // Gather candidate materials
            var mats = new List<Material>(64);
            CollectRendererMaterials(root, mats);
            CollectGraphicMaterials(root, mats); // UGUI/TMP support

            // Filter to target shader + property
            var targets = new List<Material>(mats.Count);
            foreach (var m in mats)
            {
                if (!m) continue;
                var sh = m.shader;
                if (!sh) continue;
                if (sh.name != shaderName) continue;
                if (!m.HasProperty(masterProperty)) continue;
                targets.Add(m);
            }

            if (targets.Count == 0)
            {
                if (logChanges) Debug.Log($"[ToggleShaderFeature] No materials using '{shaderName}' with '{masterProperty}' under '{root.name}'.");
                return;
            }

            // Decide new state based on the first found material (global toggle)
            bool wasOn = targets[0].GetFloat(masterProperty) > 0.5f;
            float newVal = wasOn ? 0f : 1f;

            int changed = 0;
            foreach (var m in targets)
            {
                if (!m) continue;
                float cur = m.GetFloat(masterProperty);
                bool curOn = cur > 0.5f;
                bool newOn = newVal > 0.5f;
                if (curOn != newOn)
                {
                    m.SetFloat(masterProperty, newVal);
                    changed++;
                }
            }

            bool nowOn = newVal > 0.5f;
            UpdateStatusLabel(nowOn);

            if (logChanges)
                Debug.Log($"[ToggleShaderFeature] Toggled '{masterProperty}' to {(nowOn ? "ON" : "OFF")} on {changed}/{targets.Count} materials under '{root.name}'.");
        }

        // ---- Helpers ----

        void CollectRendererMaterials(Transform root, List<Material> outList)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
            foreach (var r in renderers)
            {
                if (!r) continue;

                if (editMaterialPermanent)
                {
                    var arr = r.sharedMaterials;
                    if (arr != null)
                        foreach (var m in arr) if (m) outList.Add(m);
                }
                else
                {
                    var arr = r.materials; // creates instances if needed
                    if (arr != null)
                        foreach (var m in arr) if (m) outList.Add(m);
                }
            }
        }

        void CollectGraphicMaterials(Transform root, List<Material> outList)
        {
            var graphics = root.GetComponentsInChildren<Graphic>(includeInactive);
            foreach (var g in graphics)
            {
                if (!g) continue;
                var m = g.material;
                if (m) outList.Add(m);
            }
        }

        /// <summary>
        /// Looks up first matching material and returns current ON/OFF, or null if not found.
        /// </summary>
        bool? ProbeCurrentState()
        {
            var root = searchRoot ? searchRoot : transform;

            var mats = new List<Material>(32);
            CollectRendererMaterials(root, mats);
            CollectGraphicMaterials(root, mats);

            foreach (var m in mats)
            {
                if (!m || m.shader == null) continue;
                if (m.shader.name != shaderName) continue;
                if (!m.HasProperty(masterProperty)) continue;
                return m.GetFloat(masterProperty) > 0.5f;
            }

            return null;
        }

        void UpdateStatusLabel(bool isOn)
        {
            if (!statusLabel) return;
            statusLabel.text = isOn ? onText : offText;
            statusLabel.color = isOn ? onColor : offColor;
        }
    }
}
