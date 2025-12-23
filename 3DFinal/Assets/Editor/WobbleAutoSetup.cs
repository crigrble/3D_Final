// WobbleAutoSetup disabled: removed in cleanup. Keep file to allow easy restore if needed.
#if false
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WobbleAutoSetup
{
    static WobbleAutoSetup()
    {
        // Delay call to let Unity initialize assets
        EditorApplication.delayCall += RunAutoSetupOnce;
    }

    private static void RunAutoSetupOnce()
    {
        // Run only in editor and not during play
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Call existing utility menu items, but via reflection to avoid menu path assumptions
        WobbleEditorUtilities.CreateWobbleRenderFeatureAsset();
        WobbleEditorUtilities.AutoAddWobbleFeatureToURP();
        WobbleEditorUtilities.AssignFoundMaterialToFeatures();
        WobbleEditorUtilities.EnsureWaterCameraOnMainCamera();

        // Remove this callback to avoid repeated runs
        EditorApplication.delayCall -= RunAutoSetupOnce;
    }
}
#endif
