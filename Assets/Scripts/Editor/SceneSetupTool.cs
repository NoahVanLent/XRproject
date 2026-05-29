using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

// Menu: Tools > Hide & Sneak > Setup Scene
// Run this once to scaffold the entire scene. Safe to re-run; skips objects that already exist.
public static class SceneSetupTool
{
    [MenuItem("Tools/Hide & Sneak/Setup Scene")]
    public static void SetupScene()
    {
        SetupTags();
        SetupGameManager();
        SetupPlayer();
        SetupSeekerAgent();
        SetupGuardAgent();
        SetupWatcherAgent();
        SetupHidingSpots();
        SetupFloor();

        Debug.Log("Scene setup complete! Don't forget to bake the NavMesh: Window > AI > Navigation > Bake.");
    }

    static void SetupTags()
    {
        // Ensure Player tag exists (it's built-in in Unity, so this is just a reminder)
        // Custom tags need to be added via TagManager — we log a reminder instead
        Debug.Log("Make sure 'Player' tag exists in Tags & Layers (it's built-in by default).");
    }

    static void SetupGameManager()
    {
        if (GameObject.Find("GameManager") != null) return;

        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
    }

    static void SetupPlayer()
    {
        // Try to find existing XR Origin
        var player = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
        if (player == null)
        {
            // Create a simple stand-in if no XR rig is in the scene yet
            player = new GameObject("XR Origin (XR Rig)");
            player.transform.position = new Vector3(0, 0, 0);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(player.transform);
            capsule.transform.localPosition = new Vector3(0, 1, 0);
            capsule.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        player.tag = "Player";

        if (player.GetComponent<PlayerHide>() == null)
            player.AddComponent<PlayerHide>();

        // Add a CapsuleCollider so HidingSpot triggers fire
        if (player.GetComponent<Collider>() == null)
        {
            var col = player.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 0.9f, 0);
        }
    }

    static void SetupSeekerAgent()
    {
        if (GameObject.Find("Seeker") != null) return;

        var patrolPoints = CreatePatrolPoints("SeekerPatrol", new Vector3[]
        {
            new Vector3(-5, 0, -5),
            new Vector3(5, 0, -5),
            new Vector3(5, 0, 5),
            new Vector3(-5, 0, 5),
        });

        var go = CreateAgentObject("Seeker", new Vector3(0, 0, -8), Color.red);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;

        var seeker = go.AddComponent<SeekerAgent>();

        // Assign patrol points via SerializedObject so they show in Inspector
        var so = new SerializedObject(seeker);
        var prop = so.FindProperty("patrolPoints");
        prop.arraySize = patrolPoints.Count;
        for (int i = 0; i < patrolPoints.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = patrolPoints[i];
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(go, "Create Seeker");
    }

    static void SetupGuardAgent()
    {
        if (GameObject.Find("Guard") != null) return;

        var patrolPoints = CreatePatrolPoints("GuardPatrol", new Vector3[]
        {
            new Vector3(-8, 0, 0),
            new Vector3(-8, 0, 8),
            new Vector3(0, 0, 8),
        });

        var go = CreateAgentObject("Guard", new Vector3(-8, 0, 0), Color.cyan);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;

        var guard = go.AddComponent<GuardAgent>();

        var so = new SerializedObject(guard);
        var prop = so.FindProperty("patrolPoints");
        prop.arraySize = patrolPoints.Count;
        for (int i = 0; i < patrolPoints.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = patrolPoints[i];
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(go, "Create Guard");
    }

    static void SetupWatcherAgent()
    {
        if (GameObject.Find("Watcher") != null) return;

        var go = CreateAgentObject("Watcher", new Vector3(8, 0, 0), Color.magenta);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;

        go.AddComponent<WatcherAgent>();

        Undo.RegisterCreatedObjectUndo(go, "Create Watcher");
    }

    static void SetupHidingSpots()
    {
        var spots = new (Vector3 pos, Vector3 size, string name)[]
        {
            (new Vector3(3, 0.75f, 3),  new Vector3(2, 1.5f, 1), "HidingSpot_Desk"),
            (new Vector3(-3, 0.75f, -3), new Vector3(1.5f, 1.5f, 1.5f), "HidingSpot_Closet"),
            (new Vector3(0, 0.75f, 5),  new Vector3(2, 1.5f, 1), "HidingSpot_Cabinet"),
        };

        foreach (var (pos, size, name) in spots)
        {
            if (GameObject.Find(name) != null) continue;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = pos;
            cube.transform.localScale = size;

            // Make trigger collider slightly larger than the visual
            var col = cube.GetComponent<BoxCollider>();
            col.isTrigger = false; // visual collider stays solid

            // Add a child trigger for hiding detection
            var trigger = new GameObject("Trigger");
            trigger.transform.SetParent(cube.transform);
            trigger.transform.localPosition = Vector3.zero;
            var triggerCol = trigger.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1.4f, 1.4f, 1.4f);
            trigger.AddComponent<HidingSpot>();

            // Give it a distinct color
            var renderer = cube.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.3f, 0.6f, 0.3f);

            Undo.RegisterCreatedObjectUndo(cube, "Create " + name);
        }
    }

    static void SetupFloor()
    {
        if (GameObject.Find("Floor") != null) return;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3, 1, 3);

        Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
    }

    // --- Helpers ---

    static GameObject CreateAgentObject(string name, Vector3 position, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

        var renderer = go.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;

        return go;
    }

    static List<Transform> CreatePatrolPoints(string groupName, Vector3[] positions)
    {
        var group = GameObject.Find(groupName);
        if (group == null)
        {
            group = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(group, "Create " + groupName);
        }

        var points = new List<Transform>();
        for (int i = 0; i < positions.Length; i++)
        {
            string pointName = groupName + "_" + i;
            var existing = GameObject.Find(pointName);
            if (existing != null)
            {
                points.Add(existing.transform);
                continue;
            }

            var point = new GameObject(pointName);
            point.transform.SetParent(group.transform);
            point.transform.position = positions[i];
            points.Add(point.transform);
            Undo.RegisterCreatedObjectUndo(point, "Create " + pointName);
        }

        return points;
    }
}
