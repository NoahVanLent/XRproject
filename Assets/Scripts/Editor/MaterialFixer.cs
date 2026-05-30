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
    [MenuItem("Tools/Hide & Sneak/Fix Purple Materials")]
    public static void FixAll()
    {
        FixNPCMaterials();
        FixSchoolMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Material fix complete! If anything is still purple, run Edit > Rendering > Materials > Convert All Built-in Materials to URP.");
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
