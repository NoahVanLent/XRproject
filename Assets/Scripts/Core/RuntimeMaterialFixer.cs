using UnityEngine;

/// <summary>
/// Fixes non-URP materials at runtime so they don't appear purple in VR.
/// Attach to GameManager or any persistent object in the scene.
/// Runs once at startup before first frame.
/// </summary>
public class RuntimeMaterialFixer : MonoBehaviour
{
    void Awake()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        int count = 0;
        foreach (var rend in FindObjectsOfType<Renderer>(true))
        {
            var mats = rend.materials; // use .materials (instances) not sharedMaterials
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                var sName = mats[i].shader.name;
                if (sName.Contains("Universal Render Pipeline")) continue;
                if (sName.Contains("Hidden"))  continue;
                if (sName.Contains("Decal"))   continue;
                if (sName.Contains("Sprites")) continue;
                if (sName.Contains("TextMeshPro")) continue;
                if (sName.Contains("UI"))      continue;

                Color color = Color.white;
                try { if (mats[i].HasProperty("_Color")) color = mats[i].color; } catch { }

                mats[i].shader = urpLit;
                try { mats[i].color = color; } catch { }
                changed = true;
                count++;
            }

            if (changed) rend.materials = mats;
        }

        Debug.Log($"RuntimeMaterialFixer: fixed {count} materials at startup.");
    }
}
