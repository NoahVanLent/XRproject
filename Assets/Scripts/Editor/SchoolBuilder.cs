using UnityEngine;
using UnityEditor;

/// <summary>
/// Builds a simple school environment:
/// - Central hallway (24m long, 4m wide)
/// - 4 classrooms (2 left, 2 right) with doorways
/// - All geometry uses URP Lit material
/// </summary>
public static class SchoolBuilder
{
    private const float WallHeight   = 3f;
    private const float WallThick    = 0.2f;
    private const float DoorWidth    = 3f;    // wider so NavMesh agents fit through
    private const float DoorHeight   = 2.4f;
    private const float HallWidth    = 4f;
    private const float HallLength   = 28f;
    private const float RoomWidth    = 12f;   // bigger classrooms
    private const float RoomDepth    = 9f;

    private static Material _wallMat;
    private static Material _floorMat;
    private static Material _ceilMat;
    private static GameObject _root;

    [MenuItem("Tools/Hide & Sneak/Build School Environment")]
    public static void Build()
    {
        // Remove old school if rebuild
        var old = GameObject.Find("School");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // Remove old flat floor
        var oldFloor = GameObject.Find("Floor");
        if (oldFloor != null) Undo.DestroyObjectImmediate(oldFloor);

        CreateMaterials();

        _root = new GameObject("School");
        Undo.RegisterCreatedObjectUndo(_root, "Build School");

        BuildHallway();
        BuildClassroom("Classroom_L1", new Vector3(-(HallWidth / 2 + RoomWidth / 2), 0,  9f), doorOnRight: true);
        BuildClassroom("Classroom_L2", new Vector3(-(HallWidth / 2 + RoomWidth / 2), 0, -5f), doorOnRight: true);
        BuildClassroom("Classroom_R1", new Vector3( (HallWidth / 2 + RoomWidth / 2), 0,  9f), doorOnRight: false);
        BuildClassroom("Classroom_R2", new Vector3( (HallWidth / 2 + RoomWidth / 2), 0, -5f), doorOnRight: false);

        Debug.Log("School built! Please REBAKE the NavMesh: click NavMesh Surface on the School/Floor object → Bake.");
    }

    // ── Materials ────────────────────────────────────────────────────────────

    static void CreateMaterials()
    {
        _wallMat  = MakeMat(new Color(0.85f, 0.82f, 0.78f)); // off-white walls
        _floorMat = MakeMat(new Color(0.55f, 0.45f, 0.30f)); // warm wood floor
        _ceilMat  = MakeMat(new Color(0.95f, 0.95f, 0.95f)); // white ceiling
    }

    static Material MakeMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }

    // ── Hallway ──────────────────────────────────────────────────────────────

    static void BuildHallway()
    {
        var hall = new GameObject("Hallway");
        hall.transform.SetParent(_root.transform);

        // Floor
        CreateBox("Floor", hall,
            new Vector3(0, 0, HallLength / 2 - 12f),
            new Vector3(HallWidth, WallThick, HallLength),
            _floorMat);

        // Ceiling
        CreateBox("Ceiling", hall,
            new Vector3(0, WallHeight, HallLength / 2 - 12f),
            new Vector3(HallWidth, WallThick, HallLength),
            _ceilMat);

        // End wall (far)
        CreateBox("WallFar", hall,
            new Vector3(0, WallHeight / 2, HallLength - 12f),
            new Vector3(HallWidth, WallHeight, WallThick),
            _wallMat);

        // End wall (near)
        CreateBox("WallNear", hall,
            new Vector3(0, WallHeight / 2, -12f),
            new Vector3(HallWidth, WallHeight, WallThick),
            _wallMat);

        // Left side wall — gaps at classroom center Z positions
        BuildCorridorSideWall("WallLeft",  hall, -HallWidth / 2f, new float[] { 9f, -5f });
        // Right side wall
        BuildCorridorSideWall("WallRight", hall,  HallWidth / 2f, new float[] { 9f, -5f });
    }

    // Side walls of hallway with gaps where classroom doors are
    static void BuildCorridorSideWall(string name, GameObject parent, float x, float[] doorCentersZ)
    {
        float start = -12f;
        float end   = HallLength - 12f;

        // Collect door ranges
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
                // Full height segment before door
                float len = gap.min - cursor;
                CreateBox($"{name}_seg{seg++}", parent,
                    new Vector3(x, WallHeight / 2, cursor + len / 2),
                    new Vector3(WallThick, WallHeight, len), _wallMat);
            }
            // Above door
            float aboveH = WallHeight - DoorHeight;
            CreateBox($"{name}_above{seg++}", parent,
                new Vector3(x, DoorHeight + aboveH / 2, gap.min + DoorWidth / 2),
                new Vector3(WallThick, aboveH, DoorWidth), _wallMat);

            cursor = gap.max;
        }

        // Remaining segment after last door
        if (cursor < end)
        {
            float len = end - cursor;
            CreateBox($"{name}_seg{seg}", parent,
                new Vector3(x, WallHeight / 2, cursor + len / 2),
                new Vector3(WallThick, WallHeight, len), _wallMat);
        }
    }

    // ── Classroom ────────────────────────────────────────────────────────────

    static void BuildClassroom(string name, Vector3 center, bool doorOnRight)
    {
        var room = new GameObject(name);
        room.transform.SetParent(_root.transform);

        float halfW = RoomWidth / 2f;
        float halfD = RoomDepth / 2f;
        float cx = center.x, cz = center.z;

        // Floor & ceiling
        CreateBox("Floor", room,
            new Vector3(cx, 0, cz),
            new Vector3(RoomWidth, WallThick, RoomDepth), _floorMat);

        CreateBox("Ceiling", room,
            new Vector3(cx, WallHeight, cz),
            new Vector3(RoomWidth, WallThick, RoomDepth), _ceilMat);

        // Back wall (away from hallway)
        float backX = doorOnRight ? cx - halfW : cx + halfW;
        CreateBox("WallBack", room,
            new Vector3(backX, WallHeight / 2, cz),
            new Vector3(WallThick, WallHeight, RoomDepth), _wallMat);

        // Side walls (parallel to hallway)
        CreateBox("WallFront", room,
            new Vector3(cx, WallHeight / 2, cz + halfD),
            new Vector3(RoomWidth, WallHeight, WallThick), _wallMat);

        CreateBox("WallBack2", room,
            new Vector3(cx, WallHeight / 2, cz - halfD),
            new Vector3(RoomWidth, WallHeight, WallThick), _wallMat);

        // Hallway wall — with door opening in the middle
        float hallSideX = doorOnRight ? cx + halfW : cx - halfW;
        float doorCZ = cz; // door centred on room

        // Left of door
        float leftLen = halfD - DoorWidth / 2;
        CreateBox("WallHall_L", room,
            new Vector3(hallSideX, WallHeight / 2, cz - halfD + leftLen / 2),
            new Vector3(WallThick, WallHeight, leftLen), _wallMat);

        // Right of door
        float rightLen = halfD - DoorWidth / 2;
        CreateBox("WallHall_R", room,
            new Vector3(hallSideX, WallHeight / 2, cz + halfD - rightLen / 2),
            new Vector3(WallThick, WallHeight, rightLen), _wallMat);

        // Above door
        float aboveH = WallHeight - DoorHeight;
        CreateBox("WallHall_Top", room,
            new Vector3(hallSideX, DoorHeight + aboveH / 2, doorCZ),
            new Vector3(WallThick, aboveH, DoorWidth), _wallMat);

        // Simple classroom furniture
        AddDesk(room, new Vector3(cx - 2f, 0, cz + 1f));
        AddDesk(room, new Vector3(cx,      0, cz + 1f));
        AddDesk(room, new Vector3(cx + 2f, 0, cz + 1f));
        AddDesk(room, new Vector3(cx - 2f, 0, cz - 1f));
        AddDesk(room, new Vector3(cx,      0, cz - 1f));
        AddDesk(room, new Vector3(cx + 2f, 0, cz - 1f));

        // Teacher's desk at back
        CreateBox("TeacherDesk", room,
            new Vector3(backX + (doorOnRight ? 1.5f : -1.5f), 0.4f, cz),
            new Vector3(1.5f, 0.8f, 0.7f),
            MakeMat(new Color(0.4f, 0.25f, 0.1f)));
    }

    static void AddDesk(GameObject parent, Vector3 pos)
    {
        // Desk surface
        var desk = CreateBox("Desk", parent,
            new Vector3(pos.x, 0.4f, pos.z),
            new Vector3(1.2f, 0.05f, 0.6f),
            MakeMat(new Color(0.6f, 0.45f, 0.25f)));

        // Hiding spot trigger under the desk
        var trigger = new GameObject("HidingSpot_Desk");
        trigger.transform.SetParent(desk.transform);
        trigger.transform.localPosition = new Vector3(0, -3f, 0); // below desk surface
        var col = trigger.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.9f, 0.6f, 0.5f);
        trigger.AddComponent<HidingSpot>();
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    static GameObject CreateBox(string name, GameObject parent,
        Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
