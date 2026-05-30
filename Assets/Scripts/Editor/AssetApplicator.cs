using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Applies NPC models to agents and adds school props to classrooms.
/// Menu: Tools > Hide & Sneak > Apply Assets
/// </summary>
public static class AssetApplicator
{
    [MenuItem("Tools/Hide & Sneak/Apply Assets")]
    public static void ApplyAll()
    {
        ApplyNPCModels();
        AddSchoolProps();
        Debug.Log("Assets applied! NPC models and school props placed.");
    }

    // ── NPC Models ────────────────────────────────────────────────────────────

    static void ApplyNPCModels()
    {
        ApplyNPC("Agent_Wanderer",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_01.prefab");
        ApplyNPC("Agent_Chaser",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_02m_01.prefab");
        ApplyNPC("Agent_Stalker",
            "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_01.prefab");
    }

    static void ApplyNPC(string agentName, string prefabPath)
    {
        var agent = GameObject.Find(agentName);
        if (agent == null) { Debug.LogWarning($"Agent not found: {agentName}"); return; }

        // Remove old head/eye visuals from SchoolBuilder
        foreach (Transform child in agent.transform)
        {
            if (child.name == "Head" || child.name == "NPCModel")
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        // Hide the capsule renderer
        var renderer = agent.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        // Load and instantiate NPC prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"Prefab not found: {prefabPath}"); return; }

        var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, agent.transform);
        npc.name = "NPCModel";
        npc.transform.localPosition = new Vector3(0, -0.9f, 0); // align feet to ground
        npc.transform.localRotation = Quaternion.identity;
        npc.transform.localScale    = Vector3.one;

        // Remove colliders from NPC model so they don't block NavMesh
        foreach (var col in npc.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        Undo.RegisterCreatedObjectUndo(npc, $"Apply NPC to {agentName}");
        Debug.Log($"NPC model applied to {agentName}");
    }

    // ── School Props ──────────────────────────────────────────────────────────

    static void AddSchoolProps()
    {
        var school = GameObject.Find("School");
        if (school == null) { Debug.LogWarning("School not found — run Build School Environment first."); return; }

        // Add props to each classroom
        AddClassroomProps("Classroom_L1", new Vector3(-8f, 0, 9f),  true);
        AddClassroomProps("Classroom_L2", new Vector3(-8f, 0, -5f), true);
        AddClassroomProps("Classroom_R1", new Vector3(10f, 0, 9f),  false);
        AddClassroomProps("Classroom_R2", new Vector3(10f, 0, -5f), false);

        AddHallwayProps();
    }

    static void AddClassroomProps(string roomName, Vector3 center, bool doorOnRight)
    {
        var room = GameObject.Find(roomName);
        if (room == null) return;

        float cx = center.x, cz = center.z;
        float dirX = doorOnRight ? 1f : -1f;

        // Blackboard prop at front
        PlaceProp("Assets/school/Prefabs/props/board1.prefab", room,
            new Vector3(cx - dirX * 4.5f, 0, cz), Quaternion.Euler(0, doorOnRight ? 90 : -90, 0));

        // Projector
        PlaceProp("Assets/school/Prefabs/props/projector.prefab", room,
            new Vector3(cx - dirX * 3f, 1.5f, cz), Quaternion.Euler(0, doorOnRight ? 90 : -90, 0));

        // Teacher chair
        PlaceProp("Assets/school/Prefabs/props/chair1.prefab", room,
            new Vector3(cx - dirX * 3.5f, 0, cz + 0.5f), Quaternion.identity);

        // Student chairs at desk positions
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                Vector3 pos = new Vector3(
                    cx + dirX * (row * 2.5f - 2f),
                    0,
                    cz - 2f + col * 4f + 0.6f);
                PlaceProp("Assets/school/Prefabs/props/chair.prefab", room,
                    pos, Quaternion.Euler(0, doorOnRight ? -90 : 90, 0));
            }
        }

        // Books on a desk
        PlaceProp("Assets/school/Prefabs/props/book1.prefab", room,
            new Vector3(cx + dirX * 0.5f, 0.75f, cz - 1.5f), Quaternion.identity);
        PlaceProp("Assets/school/Prefabs/props/book3.prefab", room,
            new Vector3(cx + dirX * 0.3f, 0.75f, cz + 1.5f), Quaternion.identity);

        // Laptop
        PlaceProp("Assets/school/Prefabs/props/a laptop.prefab", room,
            new Vector3(cx - dirX * 3.2f, 0.8f, cz - 0.2f), Quaternion.Euler(0, doorOnRight ? 90 : -90, 0));
    }

    static void AddHallwayProps()
    {
        var hall = GameObject.Find("Hallway");
        if (hall == null) return;

        // Lockers along left wall
        for (int i = 0; i < 3; i++)
        {
            PlaceProp("Assets/school/Prefabs/props/locker_1.prefab", hall,
                new Vector3(-1.8f, 0, -6f + i * 3f), Quaternion.Euler(0, 90, 0));
        }

        // Lockers along right wall
        for (int i = 0; i < 3; i++)
        {
            PlaceProp("Assets/school/Prefabs/props/locker_2.prefab", hall,
                new Vector3(1.8f, 0, -6f + i * 3f), Quaternion.Euler(0, -90, 0));
        }

        // Fire extinguisher
        PlaceProp("Assets/school/Prefabs/props/fire.prefab", hall,
            new Vector3(-1.5f, 0, 10f), Quaternion.identity);
    }

    static void PlaceProp(string prefabPath, GameObject parent, Vector3 position, Quaternion rotation)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"Prop not found: {prefabPath}"); return; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.transform.position = position;
        go.transform.rotation = rotation;

        // Remove colliders so they don't block NavMesh or agents
        foreach (var col in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        Undo.RegisterCreatedObjectUndo(go, "Place Prop");
    }
}
