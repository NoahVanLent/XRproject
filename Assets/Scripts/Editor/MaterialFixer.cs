using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Fixes purple/missing materials by:
/// 1. Upgrading all Built-in materials to URP via Unity's upgrader
/// 2. For NPC assets: swaps Materials folder references to MaterialsUPR equivalents
/// Menu: Tools > Hide & Sneak > Fix Purple Materials
/// </summary>
public static class MaterialFixer
{
    [MenuItem("Tools/Hide & Sneak/Fix Purple Materials (All)")]
    public static void FixAll()
    {
        ExtractAndUpgradeFBXMaterials();
        FixNPCMaterials();
        FixSchoolMaterials();
        DisableAgentAnimators();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Done! If anything is still purple: Edit > Rendering > Materials > Convert All Built-in Materials to URP.");
    }

    // ── Force upgrade ALL materials in entire project ─────────────────────────

    [MenuItem("Tools/Hide & Sneak/Fix Extra Material Slots")]
    public static void FixExtraMaterialSlots()
    {
        // Find all renderers in the scene where materials > submeshes
        var renderers = Object.FindObjectsOfType<Renderer>();
        int fixed_count = 0;

        foreach (var rend in renderers)
        {
            var mf = rend.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            int submeshCount = mf.sharedMesh.subMeshCount;
            var mats = rend.sharedMaterials;

            if (mats.Length <= submeshCount) continue;

            // Trim to submesh count
            var trimmed = new Material[submeshCount];
            for (int i = 0; i < submeshCount; i++)
                trimmed[i] = mats[i];

            rend.sharedMaterials = trimmed;
            EditorUtility.SetDirty(rend.gameObject);
            fixed_count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed {fixed_count} renderers with too many material slots.");
    }

    [MenuItem("Tools/Hide & Sneak/Force Upgrade All Materials to URP")]
    public static void ForceUpgradeAll()
    {
        var allMatGuids = AssetDatabase.FindAssets("t:Material");
        int count = 0;
        foreach (var guid in allMatGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            var shaderName = mat.shader.name;
            // Skip URP, Hidden, Decal and other special shaders
            if (shaderName.Contains("Universal Render Pipeline")) continue;
            if (shaderName.Contains("Hidden"))  continue;
            if (shaderName.Contains("Decal"))   continue;
            if (shaderName.Contains("Sprites"))  continue;
            if (shaderName.Contains("Particle")) continue;

            Color color = Color.white;
            try { if (mat.HasProperty("_Color")) color = mat.color; }
            catch { /* some materials throw even with HasProperty check */ }

            try
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                mat.color  = color;
                EditorUtility.SetDirty(mat);
                count++;
            }
            catch { /* skip materials that refuse to change */ }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Force-upgraded {count} materials to URP Lit.");
    }

    // ── Extract FBX Materials ─────────────────────────────────────────────────

    static void ExtractAndUpgradeFBXMaterials()
    {
        // Find all FBX importers in the npc and school folders
        var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/npc_casual_set_00", "Assets/school" });
        int extracted = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            // Set material import mode to import and extract
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            }

            // Extract materials to a folder next to the FBX
            var dir = Path.GetDirectoryName(path) + "/ExtractedMaterials";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var errors = importer.ExtractTextures(dir);
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
                                              ModelImporterMaterialSearch.Everywhere);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            extracted++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Extracted materials from {extracted} FBX files.");

        // Now upgrade all extracted materials to URP
        UpgradeExtractedMaterials();
    }

    static void UpgradeExtractedMaterials()
    {
        var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/npc_casual_set_00", "Assets/school" });
        int upgraded = 0;
        foreach (var guid in matGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // Skip already-URP materials
            if (path.Contains("MaterialsUPR")) continue;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader == null) continue;
            if (mat.shader.name.Contains("Universal Render Pipeline")) continue;

            // Save color before upgrading shader
            var color = mat.HasProperty("_Color") ? mat.color : Color.white;
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            mat.color  = color;
            EditorUtility.SetDirty(mat);
            upgraded++;
        }
        Debug.Log($"Upgraded {upgraded} materials to URP Lit.");
    }

    // ── Disable Agent Animators ───────────────────────────────────────────────

    static void DisableAgentAnimators()
    {
        string[] agentNames = { "Agent_Wanderer", "Agent_Chaser", "Agent_Stalker" };
        foreach (var name in agentNames)
        {
            var agent = GameObject.Find(name);
            if (agent == null) continue;

            // Disable all animators on NPC children so they don't float around
            foreach (var anim in agent.GetComponentsInChildren<Animator>(true))
            {
                anim.enabled = false;
                EditorUtility.SetDirty(anim.gameObject);
            }

            // Also reset all child transforms to ground level
            var npcModel = agent.transform.Find("NPCModel");
            if (npcModel != null)
            {
                npcModel.localPosition = new Vector3(0, -0.9f, 0);
                npcModel.localRotation = Quaternion.identity;
            }

            Debug.Log($"Disabled animators on {name}.");
        }
    }

    // ── NPC Materials ─────────────────────────────────────────────────────────

    static void FixNPCMaterials()
    {
        // Build a lookup: filename -> URP material path
        var urpMaterials = new Dictionary<string, string>();
        var urpGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/npc_casual_set_00/MaterialsUPR" });
        foreach (var guid in urpGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = Path.GetFileNameWithoutExtension(path).ToLower();
            urpMaterials[name] = path;
        }

        Debug.Log($"Found {urpMaterials.Count} URP materials for NPCs.");

        // Find all renderers under NPC model children of agents
        string[] agentNames = { "Agent_Wanderer", "Agent_Chaser", "Agent_Stalker" };
        foreach (var agentName in agentNames)
        {
            var agent = GameObject.Find(agentName);
            if (agent == null) continue;

            var renderers = agent.GetComponentsInChildren<Renderer>(true);
            int fixed_count = 0;
            foreach (var rend in renderers)
            {
                var mats = rend.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var matName = mats[i].name.ToLower();
                    if (urpMaterials.TryGetValue(matName, out var urpPath))
                    {
                        var urpMat = AssetDatabase.LoadAssetAtPath<Material>(urpPath);
                        if (urpMat != null) { mats[i] = urpMat; changed = true; fixed_count++; }
                    }
                }
                if (changed)
                {
                    rend.sharedMaterials = mats;
                    EditorUtility.SetDirty(rend.gameObject);
                }
            }
            Debug.Log($"{agentName}: fixed {fixed_count} materials.");
        }
    }

    // ── School Materials ──────────────────────────────────────────────────────

    static void FixSchoolMaterials()
    {
        // Find all school material guids
        var schoolMatGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/school/material" });
        var schoolMats = new Dictionary<string, string>();
        foreach (var guid in schoolMatGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = Path.GetFileNameWithoutExtension(path).ToLower();
            schoolMats[name] = path;
        }

        // Upgrade school prop renderers in scene
        var school = GameObject.Find("School");
        if (school == null) return;

        var renderers = school.GetComponentsInChildren<Renderer>(true);
        int fixed_count = 0;
        foreach (var rend in renderers)
        {
            var mats = rend.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                // Check if shader is Built-in (not URP)
                var shader = mats[i].shader;
                if (shader == null || shader.name.Contains("Universal Render Pipeline")) continue;
                if (!shader.name.Contains("Standard") && !shader.name.Contains("Diffuse")) continue;

                // Try to find matching school material
                var matName = mats[i].name.ToLower();
                if (schoolMats.TryGetValue(matName, out var schoolPath))
                {
                    var newMat = AssetDatabase.LoadAssetAtPath<Material>(schoolPath);
                    if (newMat != null) { mats[i] = newMat; changed = true; fixed_count++; continue; }
                }

                // Fallback: upgrade shader to URP Lit and keep color
                var color = mats[i].color;
                mats[i].shader = Shader.Find("Universal Render Pipeline/Lit");
                mats[i].color  = color;
                changed = true;
                fixed_count++;
            }
            if (changed)
            {
                rend.sharedMaterials = mats;
                EditorUtility.SetDirty(rend.gameObject);
            }
        }
        Debug.Log($"School props: fixed {fixed_count} materials.");
    }
}
