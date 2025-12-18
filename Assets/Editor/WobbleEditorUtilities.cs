// WobbleEditorUtilities disabled: removed in cleanup. Keep file to allow easy restore if needed.
#if false
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Rendering;

// URP types
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

public static class WobbleEditorUtilities
{
    [MenuItem("Tools/Wobble/Create WobbleRenderFeature Asset")]
    public static void CreateWobbleRenderFeatureAsset()
    {
#if UNITY_2020_2_OR_NEWER
        string assetFolder = "Assets/Rendering";
        string assetPath = assetFolder + "/WobbleRenderFeature.asset";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(assetFolder))
        {
            string parent = "Assets";
            string newFolderName = "Rendering";
            AssetDatabase.CreateFolder(parent, newFolderName);
            AssetDatabase.Refresh();
        }

        var existing = AssetDatabase.LoadAssetAtPath<WobbleRenderFeature>(assetPath);
        if (existing == null)
        {
            try
            {
                var feature = ScriptableObject.CreateInstance<WobbleRenderFeature>();
                AssetDatabase.CreateAsset(feature, assetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Wobble", $"Created WobbleRenderFeature asset at {assetPath}.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create WobbleRenderFeature asset: {ex.Message}");
                EditorUtility.DisplayDialog("Wobble", $"Failed to create WobbleRenderFeature asset: {ex.Message}", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Wobble", $"WobbleRenderFeature asset already exists at {assetPath}.", "OK");
        }
#endif
    }

    [MenuItem("Tools/Wobble/Auto Add Render Feature to URP Renderer")]
    public static void AutoAddWobbleFeatureToURP()
    {
#if UNITY_2020_2_OR_NEWER
        // Find all renderer assets
        string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Wobble", "No ScriptableRendererData assets found. Is URP installed?", "OK");
            return;
        }

        // Try find a material in project that uses the vdev/FX/Wobble shader or named "Wobble"
        Material foundMaterial = null;
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var mg in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(mg);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if ((mat.shader != null && mat.shader.name != null && mat.shader.name.Contains("vdev/FX/Wobble")) || path.ToLower().Contains("wobble"))
            {
                foundMaterial = mat;
                break;
            }
        }

        int addedCount = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null) continue;

            // Skip if already contains WobbleRenderFeature
            bool has = rendererData.rendererFeatures.Any(f => f != null && f.GetType().Name == "WobbleRenderFeature");
            if (has) continue;

            // Create instance
            var feature = ScriptableObject.CreateInstance<WobbleRenderFeature>();
            feature.name = "WobbleRenderFeature";

            // Set material if found
            if (foundMaterial != null)
            {
                feature.settings.wobbleMaterial = foundMaterial;
            }

            // Add as sub-asset to renderer asset
            AssetDatabase.AddObjectToAsset(feature, path);
            rendererData.rendererFeatures.Add(feature);

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(feature);
            addedCount++;
        }

        if (addedCount > 0)
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Wobble", $"Added WobbleRenderFeature to {addedCount} renderer asset(s).", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Wobble", "No renderers needed updating (feature already present or none found).", "OK");
        }
#else
        EditorUtility.DisplayDialog("Wobble", "URP support requires Unity 2020.2 or newer.", "OK");
#endif
    }

    [MenuItem("Tools/Wobble/Assign Found Material To Features")]
    public static void AssignFoundMaterialToFeatures()
    {
#if UNITY_2020_2_OR_NEWER
        Material foundMaterial = null;
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var mg in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(mg);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if ((mat.shader != null && mat.shader.name != null && mat.shader.name.Contains("vdev/FX/Wobble")) || path.ToLower().Contains("wobble"))
            {
                foundMaterial = mat;
                break;
            }
        }

        if (foundMaterial == null)
        {
            EditorUtility.DisplayDialog("Wobble", "No Wobble material found in project.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        int updatedCount = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData == null) continue;

            foreach (var f in rendererData.rendererFeatures)
            {
                if (f != null && f.GetType().Name == "WobbleRenderFeature")
                {
                    var wob = f as WobbleRenderFeature;
                    if (wob != null && wob.settings.wobbleMaterial != foundMaterial)
                    {
                        wob.settings.wobbleMaterial = foundMaterial;
                        EditorUtility.SetDirty(wob);
                        updatedCount++;
                    }
                }
            }

            if (updatedCount > 0)
                EditorUtility.SetDirty(rendererData);
        }

        // Assign to components in all open scenes that have a 'wobbleMaterial' or 'Wobble' field/property
        int camAssigned = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                // Find all MonoBehaviour-derived components
                var components = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    var type = comp.GetType();
                    var field = type.GetField("wobbleMaterial", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(Material))
                    {
                        var current = (Material)field.GetValue(comp);
                        if (current != foundMaterial)
                        {
                            Undo.RecordObject(comp, "Assign Wobble Material");
                            field.SetValue(comp, foundMaterial);
                            EditorUtility.SetDirty(comp);
                            camAssigned++;
                            continue;
                        }
                    }

                    // Also check for field named 'Wobble' (older vnc.FX version)
                    var fieldW = type.GetField("Wobble", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (fieldW != null && fieldW.FieldType == typeof(Material))
                    {
                        var current2 = (Material)fieldW.GetValue(comp);
                        if (current2 != foundMaterial)
                        {
                            Undo.RecordObject(comp, "Assign Wobble Material");
                            fieldW.SetValue(comp, foundMaterial);
                            EditorUtility.SetDirty(comp);
                            camAssigned++;
                            continue;
                        }
                    }

                    // Also check for property 'wobbleMaterial' or 'Wobble'
                    var prop = type.GetProperty("wobbleMaterial", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(Material) && prop.CanWrite)
                    {
                        var current3 = (Material)prop.GetValue(comp);
                        if (current3 != foundMaterial)
                        {
                            Undo.RecordObject(comp, "Assign Wobble Material");
                            prop.SetValue(comp, foundMaterial);
                            EditorUtility.SetDirty(comp);
                            camAssigned++;
                            continue;
                        }
                    }

                    var propW = type.GetProperty("Wobble", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (propW != null && propW.PropertyType == typeof(Material) && propW.CanWrite)
                    {
                        var current4 = (Material)propW.GetValue(comp);
                        if (current4 != foundMaterial)
                        {
                            Undo.RecordObject(comp, "Assign Wobble Material");
                            propW.SetValue(comp, foundMaterial);
                            EditorUtility.SetDirty(comp);
                            camAssigned++;
                            continue;
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Wobble", $"Assigned Wobble material to {updatedCount} feature(s) and {camAssigned} WaterCamera(s).", "OK");
#else
        EditorUtility.DisplayDialog("Wobble", "URP support requires Unity 2020.2 or newer.", "OK");
#endif
    }

    [MenuItem("Tools/Wobble/Ensure WaterCamera On Main Camera(s)")]
    public static void EnsureWaterCameraOnMainCamera()
    {
#if UNITY_2020_2_OR_NEWER
        int assignedCount = 0;
        System.Type[] candidateTypes = new System.Type[] {
            System.Type.GetType("vnc.FX.WaterCamera, Assembly-CSharp"),
            System.Type.GetType("WaterCamera, Assembly-CSharp")
        };

        // Fallback: if GetType didn't work (assembly-qualified name may vary), search loaded assemblies
        if (candidateTypes[0] == null || candidateTypes[1] == null)
        {
            var allTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return new System.Type[0]; }
                })
                .ToArray();

            if (candidateTypes[0] == null)
                candidateTypes[0] = allTypes.FirstOrDefault(t => t.FullName == "vnc.FX.WaterCamera" || t.Name == "WaterCamera" && t.Namespace == "vnc.FX");

            if (candidateTypes[1] == null)
                candidateTypes[1] = allTypes.FirstOrDefault(t => t.FullName == "WaterCamera" || t.Name == "WaterCamera" && string.IsNullOrEmpty(t.Namespace));
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var cams = root.GetComponentsInChildren<Camera>(true);
                foreach (var c in cams)
                {
                    if (c == Camera.main)
                    {
                        bool added = false;
                        foreach (var t in candidateTypes)
                        {
                            if (t == null) continue;
                            var comp = c.GetComponent(t);
                            if (comp == null)
                            {
                                Undo.AddComponent(c.gameObject, t);
                                added = true;
                            }
                        }
                        if (added) assignedCount++;
                    }
                }
            }
        }
        EditorUtility.DisplayDialog("Wobble", $"Ensured WaterCamera component added to {assignedCount} MainCamera(s).", "OK");
#else
        EditorUtility.DisplayDialog("Wobble", "URP support requires Unity 2020.2 or newer.", "OK");
#endif
    }
}
#endif
