using UnityEngine;

namespace vnc.FX
{
    public class WaterCameraLegacy : MonoBehaviour
    {
        public Material Wobble;
        public Color underwaterColor;
        public BlendMode Blend;

        [Header("Shaders"), Space]
        public Shader multiply;
        public Shader overlay;
        public Shader screen;

        public bool effectActive = false;

        private void Update()
        {
            switch (Blend)
            {
                case BlendMode.Multiply:
                    Wobble.shader = multiply;
                    break;
                case BlendMode.Overlay:
                    Wobble.shader = overlay;
                    break;
                case BlendMode.Screen:
                    Wobble.shader = screen;
                    break;
                default:
                    break;
            }

            // 同步設定給 URP 的 WobbleManager（ScriptableRendererFeature 會讀取）
            if (Wobble != null)
            {
                WobbleManager.WobbleMaterial = Wobble;
                WobbleManager.UnderwaterColor = underwaterColor;
            }
            WobbleManager.EffectActive = effectActive;
            WobbleManager.BlendMode = (int)Blend;

        }

        public void SetBlend(int mode)
        {
            Blend = (BlendMode)mode;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Wobble == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (effectActive)
            {
                Wobble.SetColor("_Color", underwaterColor);
                Graphics.Blit(source, destination, Wobble);
            }
            else
            {
                Wobble.SetColor("_Color", Color.white);
                Graphics.Blit(source, destination);
            }
        }
    }

    // Legacy blend enum removed. Use the main WaterCamera in Assets/Scripts/WaterCamera.cs
}
