using UnityEngine;
using UnityEditor;

/// <summary>
/// Builds a visually improved school environment:
/// - Central hallway with lockers and ceiling lights
/// - 4 classrooms with unique accent colors, blackboards, windows, desks
/// - Agents with simple character shapes
/// </summary>
public static class SchoolBuilder
{
    private const float WallHeight  = 3.2f;
    private const float WallThick   = 0.2f;
    private const float DoorWidth   = 3f;
    private const float DoorHeight  = 2.4f;
    private const float HallWidth   = 4f;
    private const float HallLength  = 28f;
    private const float RoomWidth   = 12f;
    private const float RoomDepth   = 9f;

    // Shared materials
    private static Material _wallMat;
    private static Material _floorHallMat;
    private static Material _floorRoomMat;
    private static Material _ceilMat;
    private static Material _trimMat;
    private static Material _blackboardMat;
    private static Material _windowMat;
    private static Material _lockerMat;

    // Per-classroom accent colors
    private static readonly Color[] RoomAccents =
    {
        new Color(0.20f, 0.45f, 0.72f), // blue
        new Color(0.18f, 0.62f, 0.35f), // green
        new Color(0.82f, 0.35f, 0.20f), // orange-red
        new Color(0.58f, 0.25f, 0.68f), // purple
    };

    private static GameObject _root;

    [MenuItem("Tools/Hide & Sneak/Build School Environment")]
    public static void Build()
    {
        var old = GameObject.Find("School");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var oldFloor = GameObject.Find("Floor");
        if (oldFloor != null) Undo.DestroyObjectImmediate(oldFloor);

        CreateMaterials();

        _root = new GameObject("School");
        Undo.RegisterCreatedObjectUndo(_root, "Build School");

        BuildHallway();

        BuildClassroom("Classroom_L1", new Vector3(-(HallWidth / 2 + RoomWidth / 2), 0,  9f), doorOnRight: true,  accentIndex: 0);
        BuildClassroom("Classroom_L2", new Vector3(-(HallWidth / 2 + RoomWidth / 2), 0, -5f), doorOnRight: true,  accentIndex: 1);
        BuildClassroom("Classroom_R1", new Vector3( (HallWidth / 2 + RoomWidth / 2), 0,  9f), doorOnRight: false, accentIndex: 2);
        BuildClassroom("Classroom_R2", new Vector3( (HallWidth / 2 + RoomWidth / 2), 0, -5f), doorOnRight: false, accentIndex: 3);

        ImproveAgentVisuals();

        Debug.Log("School built! Rebake NavMesh: select NavMesh Surface → Bake.");
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    static void CreateMaterials()
    {
        _wallMat       = MakeMat(new Color(0.92f, 0.90f, 0.86f)); // warm white
        _floorHallMat  = MakeMat(new Color(0.55f, 0.53f, 0.50f)); // grey tiles
        _floorRoomMat  = MakeMat(new Color(0.62f, 0.48f, 0.30f)); // warm wood
        _ceilMat       = MakeMat(new Color(0.97f, 0.97f, 0.97f)); // white ceiling
        _trimMat       = MakeMat(new Color(0.25f, 0.25f, 0.25f)); // dark skirting/trim
        _blackboardMat = MakeMat(new Color(0.10f, 0.22f, 0.15f)); // dark green board
        _windowMat     = MakeMat(new Color(0.65f, 0.85f, 0.95f, 0.4f)); // light blue glass
        _lockerMat     = MakeMat(new Color(0.30f, 0.45f, 0.55f)); // steel blue lockers
    }

    static Material MakeMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }

    static Material MakeAccentMat(int index) => MakeMat(RoomAccents[index % RoomAccents.Length]);

    // ── Hallway ───────────────────────────────────────────────────────────────

    static void BuildHallway()
    {
        var hall = new GameObject("Hallway");
        hall.transform.SetParent(_root.transform);

        float startZ = -12f;
        float endZ   = HallLength + startZ;
        float midZ   = (startZ + endZ) / 2f;

        // Floor — grey tiles
        CreateBox("Floor", hall,
            new Vector3(0, 0, midZ),
            new Vector3(HallWidth, WallThick, HallLength), _floorHallMat);

        // Ceiling
        CreateBox("Ceiling", hall,
            new Vector3(0, WallHeight, midZ),
            new Vector3(HallWidth, WallThick, HallLength), _ceilMat);

        // End walls
        CreateBox("WallFar",  hall, new Vector3(0, WallHeight / 2, endZ),
            new Vector3(HallWidth, WallHeight, WallThick), _wallMat);
        CreateBox("WallNear", hall, new Vector3(0, WallHeight / 2, startZ),
            new Vector3(HallWidth, WallHeight, WallThick), _wallMat);

        // Skirting boards (decorative)
        CreateDecor("SkirtLeft",  hall, new Vector3(-HallWidth / 2, 0.08f, midZ),
            new Vector3(WallThick * 0.5f, 0.16f, HallLength), _trimMat);
        CreateDecor("SkirtRight", hall, new Vector3( HallWidth / 2, 0.08f, midZ),
            new Vector3(WallThick * 0.5f, 0.16f, HallLength), _trimMat);

        // Side walls with door gaps
        BuildCorridorSideWall("WallLeft",  hall, -HallWidth / 2f, new float[] {  9f, -5f });
        BuildCorridorSideWall("WallRight", hall,  HallWidth / 2f, new float[] {  9f, -5f });

        // Ceiling light strips
        AddCeilingLight(hall, new Vector3(0, WallHeight - 0.05f, midZ + 6f));
        AddCeilingLight(hall, new Vector3(0, WallHeight - 0.05f, midZ - 6f));

        // Lockers along the hallway walls
        AddLockerRow(hall, -HallWidth / 2f + 0.15f, midZ, 5);
        AddLockerRow(hall,  HallWidth / 2f - 0.15f, midZ, 5);
    }

    static void BuildCorridorSideWall(string name, GameObject parent, float x, float[] doorCentersZ)
    {
        float start = -12f;
        float end   = HallLength - 12f;

        var gaps = new System.Collections.Generic.List<(float min, float max)>();
        foreach (float cz in doorCentersZ)
            gaps.Add((cz - DoorWidth / 2, cz + DoorWidth / 2));
        gaps.Sort((a, b) => a.min.CompareTo(b.min));

        float cursor = start;
        int seg = 0;
        foreach (var gap in gaps)
        {
            if (cursor < gap.min)
            {
                float len = gap.min - cursor;
                CreateBox($"{name}_seg{seg++}", parent,
                    new Vector3(x, WallHeight / 2, cursor + len / 2),
                    new Vector3(WallThick, WallHeight, len), _wallMat);
            }
            float aboveH = WallHeight - DoorHeight;
            CreateBox($"{name}_above{seg++}", parent,
                new Vector3(x, DoorHeight + aboveH / 2, gap.min + DoorWidth / 2),
                new Vector3(WallThick, aboveH, DoorWidth), _wallMat);

            // Door frame trim
            CreateBox($"{name}_frameL{seg}", parent,
                new Vector3(x, DoorHeight / 2, gap.min),
                new Vector3(WallThick * 1.5f, DoorHeight, 0.08f), _trimMat);
            CreateBox($"{name}_frameR{seg}", parent,
                new Vector3(x, DoorHeight / 2, gap.max),
                new Vector3(WallThick * 1.5f, DoorHeight, 0.08f), _trimMat);

            cursor = gap.max;
        }
        if (cursor < end)
        {
            float len = end - cursor;
            CreateBox($"{name}_seg{seg}", parent,
                new Vector3(x, WallHeight / 2, cursor + len / 2),
                new Vector3(WallThick, WallHeight, len), _wallMat);
        }
    }

    // ── Classroom ─────────────────────────────────────────────────────────────

    static void BuildClassroom(string name, Vector3 center, bool doorOnRight, int accentIndex)
    {
        var room = new GameObject(name);
        room.transform.SetParent(_root.transform);

        var accentMat = MakeAccentMat(accentIndex);
        float halfW = RoomWidth / 2f;
        float halfD = RoomDepth / 2f;
        float cx = center.x, cz = center.z;

        // Floor (wood) and ceiling
        CreateBox("Floor", room, new Vector3(cx, 0, cz),
            new Vector3(RoomWidth, WallThick, RoomDepth), _floorRoomMat);
        CreateBox("Ceiling", room, new Vector3(cx, WallHeight, cz),
            new Vector3(RoomWidth, WallThick, RoomDepth), _ceilMat);

        // Skirting (decorative)
        CreateDecor("SkirtFront", room, new Vector3(cx, 0.08f, cz + halfD),
            new Vector3(RoomWidth, 0.16f, WallThick * 0.5f), _trimMat);
        CreateDecor("SkirtBack2", room, new Vector3(cx, 0.08f, cz - halfD),
            new Vector3(RoomWidth, 0.16f, WallThick * 0.5f), _trimMat);

        // Back wall (away from hallway) with accent color strip
        float backX = doorOnRight ? cx - halfW : cx + halfW;
        CreateBox("WallBack", room, new Vector3(backX, WallHeight / 2, cz),
            new Vector3(WallThick, WallHeight, RoomDepth), _wallMat);
        // Accent strip on back wall (decorative)
        CreateDecor("AccentStrip", room, new Vector3(backX, 1.5f, cz),
            new Vector3(WallThick * 0.5f, 1.0f, RoomDepth), accentMat);

        // Side walls with windows
        BuildRoomSideWall("WallFront", room, cx, cz + halfD, horizontal: true, accentMat);
        BuildRoomSideWall("WallBack2", room, cx, cz - halfD, horizontal: true, accentMat);

        // Hallway wall with door
        float hallSideX = doorOnRight ? cx + halfW : cx - halfW;
        float doorCZ = cz;
        float leftLen  = halfD - DoorWidth / 2;
        float rightLen = halfD - DoorWidth / 2;

        CreateBox("WallHall_L", room,
            new Vector3(hallSideX, WallHeight / 2, cz - halfD + leftLen / 2),
            new Vector3(WallThick, WallHeight, leftLen), _wallMat);
        CreateBox("WallHall_R", room,
            new Vector3(hallSideX, WallHeight / 2, cz + halfD - rightLen / 2),
            new Vector3(WallThick, WallHeight, rightLen), _wallMat);
        float aboveH = WallHeight - DoorHeight;
        CreateBox("WallHall_Top", room,
            new Vector3(hallSideX, DoorHeight + aboveH / 2, doorCZ),
            new Vector3(WallThick, aboveH, DoorWidth), _wallMat);

        // Door frame (decorative)
        CreateDecor("DoorFrameL", room,
            new Vector3(hallSideX, DoorHeight / 2, doorCZ - DoorWidth / 2),
            new Vector3(WallThick * 2f, DoorHeight, 0.08f), _trimMat);
        CreateDecor("DoorFrameR", room,
            new Vector3(hallSideX, DoorHeight / 2, doorCZ + DoorWidth / 2),
            new Vector3(WallThick * 2f, DoorHeight, 0.08f), _trimMat);

        // Blackboard on back wall (decorative)
        float boardX = backX + (doorOnRight ? WallThick : -WallThick);
        CreateDecor("Blackboard", room,
            new Vector3(boardX, 1.6f, cz),
            new Vector3(0.1f, 1.0f, 3.0f), _blackboardMat);
        CreateDecor("BoardLedge", room,
            new Vector3(boardX, 1.08f, cz),
            new Vector3(0.12f, 0.06f, 3.1f), _trimMat);

        // Ceiling light
        AddCeilingLight(room, new Vector3(cx, WallHeight - 0.05f, cz));

        // Desks in rows
        float deskStartX = doorOnRight ? cx - halfW + 1.5f : cx + halfW - 1.5f;
        float deskDirX   = doorOnRight ? 1f : -1f;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                Vector3 deskPos = new Vector3(
                    deskStartX + deskDirX * row * 2.5f,
                    0,
                    cz - 2f + col * 4f);
                AddDesk(room, deskPos, accentMat);
            }
        }

        // Teacher desk
        float teacherX = backX + (doorOnRight ? 2f : -2f);
        AddTeacherDesk(room, new Vector3(teacherX, 0, cz), accentMat);

        // Number sign on wall next to door
        int roomNum = accentIndex + 1;
        CreateBox($"RoomSign_{roomNum}", room,
            new Vector3(hallSideX, 2.0f, doorCZ + DoorWidth / 2 + 0.3f),
            new Vector3(WallThick * 0.5f, 0.3f, 0.5f), accentMat);
    }

    static void BuildRoomSideWall(string name, GameObject parent, float cx, float wallZ, bool horizontal, Material accentMat)
    {
        // Main wall
        CreateBox(name, parent,
            new Vector3(cx, WallHeight / 2, wallZ),
            new Vector3(RoomWidth, WallHeight, WallThick), _wallMat);

        // Window and frame (decorative — no colliders)
        CreateDecor(name + "_WinFrame", parent,
            new Vector3(cx, 1.5f, wallZ),
            new Vector3(RoomWidth * 0.37f, 1.06f, WallThick * 0.5f), _trimMat);
        CreateDecor(name + "_Window", parent,
            new Vector3(cx, 1.5f, wallZ),
            new Vector3(RoomWidth * 0.34f, 0.98f, WallThick * 0.2f), _windowMat);
    }

    // ── Furniture ─────────────────────────────────────────────────────────────

    static void AddDesk(GameObject parent, Vector3 pos, Material accentMat)
    {
        var deskMat = MakeMat(new Color(0.75f, 0.62f, 0.42f));

        // Surface
        var desk = CreateBox("Desk", parent,
            new Vector3(pos.x, 0.72f, pos.z),
            new Vector3(1.1f, 0.04f, 0.55f), deskMat);

        // Legs (decorative — no collider)
        float lx = 0.45f, lz = 0.22f;
        foreach (var offset in new[] {
            new Vector3(-lx, -0.36f, -lz), new Vector3( lx, -0.36f, -lz),
            new Vector3(-lx, -0.36f,  lz), new Vector3( lx, -0.36f,  lz) })
        {
            CreateDecor("Leg", desk, offset, new Vector3(0.04f, 0.68f, 0.04f), _trimMat);
        }

        // Chair (decorative)
        var chairMat = MakeMat(new Color(0.85f, 0.85f, 0.85f));
        var chair = CreateDecor("Chair", parent,
            new Vector3(0f, -0.28f, 0.5f),
            new Vector3(0.42f, 0.03f, 0.42f), chairMat);
        CreateDecor("ChairBack", chair,
            new Vector3(0, 10f, -6f),
            new Vector3(1f, 0.5f, 0.06f), chairMat);

        // Hiding spot under desk
        var trigger = new GameObject("HidingSpot_Desk");
        trigger.transform.SetParent(desk.transform);
        trigger.transform.localPosition = new Vector3(0, -8f, 0);
        var col = trigger.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.8f, 0.5f, 0.4f);
        trigger.AddComponent<HidingSpot>();
    }

    static void AddTeacherDesk(GameObject parent, Vector3 pos, Material accentMat)
    {
        var deskMat = MakeMat(new Color(0.35f, 0.22f, 0.10f));
        CreateBox("TeacherDesk", parent,
            new Vector3(pos.x, 0.78f, pos.z),
            new Vector3(1.8f, 0.06f, 0.7f), deskMat);
        // Drawer block
        CreateDecor("TeacherDrawers", parent,
            new Vector3(0.55f, -0.39f, 0f),
            new Vector3(0.6f, 0.72f, 0.65f), deskMat);
        CreateDecor("Monitor", parent,
            new Vector3(-0.3f, 0.42f, 0f),
            new Vector3(0.5f, 0.35f, 0.04f),
            MakeMat(new Color(0.1f, 0.1f, 0.1f)));
    }

    static void AddCeilingLight(GameObject parent, Vector3 pos)
    {
        var lightStrip = CreateBox("CeilingLight", parent, pos,
            new Vector3(0.2f, 0.06f, 1.2f),
            MakeMat(new Color(1f, 1f, 0.9f)));

        // Add actual light component
        var light = lightStrip.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = new Color(1f, 0.97f, 0.88f);
        light.intensity = 1.5f;
        light.range     = 8f;
        light.shadows   = LightShadows.Soft;
    }

    static void AddLockerRow(GameObject parent, float x, float centerZ, int count)
    {
        float lockerW = 0.5f;
        float startZ  = centerZ - (count * lockerW) / 2f;
        for (int i = 0; i < count; i++)
        {
            float lz = startZ + i * lockerW + lockerW / 2f;
            var locker = CreateBox($"Locker_{i}", parent,
                new Vector3(x, 0.9f, lz),
                new Vector3(0.3f, 1.8f, lockerW - 0.02f), _lockerMat);
            // Locker handle (decorative)
            CreateDecor("Handle", locker,
                new Vector3(0f, 0f, 0.54f),
                new Vector3(0.08f, 0.22f, 0.04f),
                MakeMat(new Color(0.8f, 0.7f, 0.1f)));
        }
    }

    // ── Agent Visuals ─────────────────────────────────────────────────────────

    static void ImproveAgentVisuals()
    {
        UpdateAgentVisual("Agent_Wanderer", new Color(0.85f, 0.15f, 0.15f));
        UpdateAgentVisual("Agent_Chaser",   new Color(0.15f, 0.35f, 0.85f));
        UpdateAgentVisual("Agent_Stalker",  new Color(0.65f, 0.15f, 0.80f));
    }

    static void UpdateAgentVisual(string agentName, Color bodyColor)
    {
        var agent = GameObject.Find(agentName);
        if (agent == null) return;

        // Remove old default renderer look
        var existing = agent.GetComponent<Renderer>();
        if (existing != null)
        {
            var mat = MakeMat(bodyColor);
            existing.sharedMaterial = mat;
        }

        // Add a head sphere if not already present
        if (agent.transform.Find("Head") != null) return;

        var head = CreateDecorSphere("Head", agent,
            new Vector3(0, 1.15f, 0),
            new Vector3(0.45f, 0.45f, 0.45f),
            MakeMat(new Color(0.95f, 0.80f, 0.65f)));

        // Eyes (decorative, no colliders)
        var eyeMat = MakeMat(Color.black);
        CreateDecorSphere("EyeL", head, new Vector3(-0.22f, 0.1f, 0.42f), Vector3.one * 0.2f, eyeMat);
        CreateDecorSphere("EyeR", head, new Vector3( 0.22f, 0.1f, 0.42f), Vector3.one * 0.2f, eyeMat);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // With collider — use for floors, walls, main desk surfaces (blocks navigation + player)
    static GameObject CreateBox(string name, GameObject parent,
        Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position   = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Without collider — use for decorative pieces (legs, handles, eyes, frames)
    // so they don't block NavMesh baking or agent movement
    static GameObject CreateDecor(string name, GameObject parent,
        Vector3 localPosition, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPosition;
        go.transform.localScale    = localScale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject CreateDecorSphere(string name, GameObject parent,
        Vector3 localPosition, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPosition;
        go.transform.localScale    = localScale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
