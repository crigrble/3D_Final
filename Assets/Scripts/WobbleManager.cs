using UnityEngine;

/// <summary>
/// 全域管理器：讓 WaterCamera 將狀態傳遞給 URP RendererFeature
/// </summary>
public static class WobbleManager
{
    public static Material WobbleMaterial;
    public static Color UnderwaterColor = new Color(0.7f, 0.9f, 1f, 0.5f);
    public static float Intensity = 6f;
    public static float Amplitude = 0.006f;
    public static float Brightness = 1.2f;
    // UV offset applied to shaders each frame. X,Y in UV space.
    public static UnityEngine.Vector2 Offset = UnityEngine.Vector2.zero;
    public static int BlendMode = 0;
    public static bool EffectActive = false;
    public static float Speed = 0f;
}
