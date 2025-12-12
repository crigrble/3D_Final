This package has the following dependencies for materials to work:  Anime Cel Shader (https://u3d.as/36Z3)

The goal of this package is to always provide an up-to-date shared assets across all demo scenes for Neko Legend in order to:

1) Reduce disk space of duplicate assets when importing multiple Neko Legends products

2) Solve the issue of older scripts in less updated assets overriding newer scripts

3) Since this is a free package, users who does not have any other Neko Legends products can still take advantage of the various free 3D models and scripts.


Installation Instructions If you have errors on import on new SRP Universal Render Pipeline for: Unity 6000.2.9f +

1) Install Unity UI (Window-> Package Management ->Package Manager -> Unity Register -> Unity UI)
Fixes example error:
 Assets\NekoLegends\SharedAssets\Scripts\BGMManager.cs(3,7): error CS0246: The type or namespace name 'TMPro' could not be found (are you missing a using directive or an assembly reference?)

2) Install URP (Universal Render Pipeline)
Fixes example error:
Assets\NekoLegends\SharedAssets\Scripts\DemoScenes.cs(8,29): error CS0234: The type or namespace name 'Universal' does not exist in the namespace 'UnityEngine.Rendering' (are you missing an assembly reference?)

3) Install TMP.  Open SharedAssetsMain and press play, and installation dialog will show up, or Window-> TextMeshPro -> Import

4) Project settings -> Player-> Set Active Ipput Handling to "Both"

5) Project Settings -> Graphics (URP) -> Render Graph, enable checkbox Compatibility Mode

6) Install Neko Legends - Anime Cel Shader to fix missing prefabs and pink textures


Burst, Jobs, Collections packages is required for this package to run as of version 1.2.0 due to the free grass shader.



*Always enable both in Assets->Settings-> Universal Render Pipeline Asset both Depth Texture and Opaque Texture checkboxes.
