using UnityEngine;
using UnityEditor;

/// <summary>
/// Switches XR rendering to Multi Pass mode.
/// This fixes purple materials AND TMP text squares in VR.
/// Single Pass Instanced is faster but breaks many shaders.
/// </summary>
public static class VRSettingsFixer
{
    [MenuItem("Tools/Hide & Sneak/Fix VR Rendering (Switch to Multi Pass)")]
    public static void FixVRRendering()
    {
        // Set stereo rendering mode to Multi Pass via PlayerSettings
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.MultiPass;

        // Also ensure GPU skinning is off (can cause issues with some NPC meshes)
        PlayerSettings.gpuSkinning = false;

        AssetDatabase.SaveAssets();

        Debug.Log("Switched to Multi Pass stereo rendering. " +
                  "This fixes purple materials and TMP text in VR. " +
                  "Rebuild the project for changes to take effect on device.");
    }
}
