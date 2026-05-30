using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
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
        SetupXROrigin();
        SetupAgents();
        SetupHidingSpots();
        SetupGrabbableProps();
        SetupFloor();

        Debug.Log("Scene setup complete! Don't forget to bake the NavMesh: Window > AI > Navigation > Bake.");
    }

    [MenuItem("Tools/Hide & Sneak/Setup XR Origin Only")]
    public static void SetupXROriginOnly() => SetupXROrigin();

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

    static void SetupXROrigin()
    {
        // Remove old placeholder if present
        var old = GameObject.Find("XR Origin (XR Rig)");
        if (old != null && old.GetComponent<CharacterController>() == null)
        {
            Undo.DestroyObjectImmediate(old);
            old = null;
        }

        var existing = GameObject.Find("XR Origin");
        if (existing != null)
        {
            ConfigureXROrigin(existing);
            return;
        }

        // --- Build XR Origin hierarchy ---
        var xrOrigin = new GameObject("XR Origin");
        xrOrigin.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(xrOrigin, "Create XR Origin");

        // Add the actual XROrigin component for proper HMD tracking
        var xrOriginComponent = xrOrigin.AddComponent<Unity.XR.CoreUtils.XROrigin>();

        // Camera Offset
        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOrigin.transform);
        cameraOffset.transform.localPosition = Vector3.zero;

        // Main Camera
        var cameraGO = new GameObject("Main Camera");
        cameraGO.transform.SetParent(cameraOffset.transform);
        cameraGO.transform.localPosition = Vector3.zero;
        var cam = cameraGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        // TrackedPoseDriver — makes the camera follow the headset rotation & position
        var tpd = cameraGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        var posAction = new UnityEngine.InputSystem.InputAction(
            binding: "<XRHMD>/centerEyePosition",
            expectedControlType: "Vector3");
        var rotAction = new UnityEngine.InputSystem.InputAction(
            binding: "<XRHMD>/centerEyeRotation",
            expectedControlType: "Quaternion");
        tpd.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
        tpd.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);

        // Wire XROrigin references
        xrOriginComponent.Camera = cam;
        xrOriginComponent.CameraFloorOffsetObject = cameraOffset;

        // Left Controller — direct interactor (grab nearby objects)
        var leftHand = new GameObject("Left Controller");
        leftHand.transform.SetParent(cameraOffset.transform);
        leftHand.transform.localPosition = new Vector3(-0.2f, -0.2f, 0.3f);
        var leftXRController = leftHand.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRController>();
        leftHand.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();

        // Left controller tracking
        var leftTPD = leftHand.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        var leftPosAction = new UnityEngine.InputSystem.InputAction(binding: "<XRController>{LeftHand}/devicePosition", expectedControlType: "Vector3");
        var leftRotAction = new UnityEngine.InputSystem.InputAction(binding: "<XRController>{LeftHand}/deviceRotation", expectedControlType: "Quaternion");
        leftTPD.positionInput = new UnityEngine.InputSystem.InputActionProperty(leftPosAction);
        leftTPD.rotationInput = new UnityEngine.InputSystem.InputActionProperty(leftRotAction);

        // Right Controller — custom ray (reliable, no XRRayInteractor needed)
        var rightHand = new GameObject("Right Controller");
        rightHand.transform.SetParent(cameraOffset.transform);
        rightHand.transform.localPosition = new Vector3(0.2f, -0.2f, 0.3f);
        rightHand.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRController>();
        rightHand.AddComponent<ControllerRay>();

        // Right controller tracking
        var rightTPD = rightHand.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        var rightPosAction = new UnityEngine.InputSystem.InputAction(binding: "<XRController>{RightHand}/devicePosition", expectedControlType: "Vector3");
        var rightRotAction = new UnityEngine.InputSystem.InputAction(binding: "<XRController>{RightHand}/deviceRotation", expectedControlType: "Quaternion");
        rightTPD.positionInput = new UnityEngine.InputSystem.InputActionProperty(rightPosAction);
        rightTPD.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rightRotAction);

        // XR Interaction Manager (needed for interactions)
        if (GameObject.FindObjectOfType<XRInteractionManager>() == null)
        {
            var mgr = new GameObject("XR Interaction Manager");
            mgr.AddComponent<XRInteractionManager>();
            Undo.RegisterCreatedObjectUndo(mgr, "Create XR Interaction Manager");
        }

        ConfigureXROrigin(xrOrigin);
        Debug.Log("XR Origin created with full HMD + controller tracking.");
    }

    static void ConfigureXROrigin(GameObject xrOrigin)
    {
        xrOrigin.tag = "Player";

        if (xrOrigin.GetComponent<CharacterController>() == null)
        {
            var cc = xrOrigin.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
        }

        if (xrOrigin.GetComponent<PlayerHide>() == null)
            xrOrigin.AddComponent<PlayerHide>();

        if (xrOrigin.GetComponent<ApplicationControl>() == null)
            xrOrigin.AddComponent<ApplicationControl>();

        if (xrOrigin.GetComponent<VRLocomotion>() == null)
        {
            var loco = xrOrigin.AddComponent<VRLocomotion>();
            // Wire up camera transform
            var cam = xrOrigin.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                var so = new SerializedObject(loco);
                so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
                so.ApplyModifiedProperties();
            }
        }
    }

    static void SetupAgents()
    {
        // Agent 1: Wander (red) — roams randomly
        if (GameObject.Find("Agent_Wanderer") == null)
        {
            var go = CreateAgentObject("Agent_Wanderer", new Vector3(0, 0, -8), Color.red);
            var navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.radius = 0.4f; navAgent.height = 1.8f;

            var seeker = go.AddComponent<SeekerAgent>();
            var so = new SerializedObject(seeker);
            so.FindProperty("mode").enumValueIndex = 0;         // Wander
            so.FindProperty("wanderSpeed").floatValue = 1.2f;
            so.FindProperty("wanderRadius").floatValue = 12f;
            so.ApplyModifiedProperties();
            Undo.RegisterCreatedObjectUndo(go, "Create Wanderer");
        }

        // Agent 2: Chaser (blue) — always follows player
        if (GameObject.Find("Agent_Chaser") == null)
        {
            var go = CreateAgentObject("Agent_Chaser", new Vector3(-8, 0, 0), Color.blue);
            var navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.radius = 0.4f; navAgent.height = 1.8f;

            var seeker = go.AddComponent<SeekerAgent>();
            var so = new SerializedObject(seeker);
            so.FindProperty("mode").enumValueIndex = 1;         // Chaser
            so.FindProperty("chaseSpeed").floatValue = 1.3f;
            so.ApplyModifiedProperties();
            Undo.RegisterCreatedObjectUndo(go, "Create Chaser");
        }

        // Agent 3: Stalker (magenta) — stands still until it sees player
        if (GameObject.Find("Agent_Stalker") == null)
        {
            var go = CreateAgentObject("Agent_Stalker", new Vector3(8, 0, 0), Color.magenta);
            var navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.radius = 0.4f; navAgent.height = 1.8f;

            var seeker = go.AddComponent<SeekerAgent>();
            var so = new SerializedObject(seeker);
            so.FindProperty("mode").enumValueIndex = 2;         // Stalker
            so.FindProperty("stalkerSpeed").floatValue = 1.5f;
            so.FindProperty("sightRange").floatValue = 10f;
            so.ApplyModifiedProperties();
            Undo.RegisterCreatedObjectUndo(go, "Create Stalker");
        }
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
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.6f, 0.3f);
            renderer.sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(cube, "Create " + name);
        }
    }

    static void SetupGrabbableProps()
    {
        if (GameObject.Find("GrabbableProp_1") != null) return;

        var positions = new Vector3[] { new Vector3(1, 1, 2), new Vector3(-1, 1, 2) };
        foreach (var pos in positions)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "GrabbableProp_" + System.Array.IndexOf(positions, pos);
            cube.transform.position = pos;
            cube.transform.localScale = Vector3.one * 0.2f;

            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.5f;

            cube.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            cube.AddComponent<GrabbableObject>();

            var renderer = cube.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.8f, 0.5f, 0.1f);
            renderer.sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(cube, "Create " + cube.name);
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
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        renderer.sharedMaterial = mat;

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
