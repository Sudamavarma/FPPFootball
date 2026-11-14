using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup. Open your pitch scene, then use the menu:
///   FPPFootball > Set Up Possession And Opponent   (Player scripts, Ball, goal markers, test opponent)
///   FPPFootball > Add Test Teammate                (a jogging teammate to test Pass and Through)
/// Safe to run again.
/// </summary>
public static class MatchSetupTool
{
    [MenuItem("FPPFootball/Set Up Possession And Opponent")]
    public static void Run()
    {
        GameObject player = GameObject.Find("Player");
        GameObject ball = GameObject.Find("Ball");
        if (player == null || ball == null)
        {
            EditorUtility.DisplayDialog("FPPFootball",
                "Could not find objects named 'Player' and 'Ball' in the open scene. Open your pitch scene first.", "OK");
            return;
        }

        // ---- Player ----
        Ensure<PlayerInputReader>(player);
        FPPPlayerController controller = Ensure<FPPPlayerController>(player);
        Ensure<PlayerBallInteraction>(player);
        PlayerLookAround lookAround = Ensure<PlayerLookAround>(player);
        Ensure<PlayerPossession>(player);
        Ensure<PlayerDefending>(player);
        Ensure<DebugHud>(player);
        PlayerTeam playerTeam = Ensure<PlayerTeam>(player);
        SetEnum(playerTeam, "team", (int)TeamId.Home);

        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            SetRefIfEmpty(controller, "cameraTransform", cam.transform);
            SetRefIfEmpty(lookAround, "cameraTransform", cam.transform);
        }
        else
        {
            Debug.LogWarning("MatchSetupTool: no Camera found under Player. Make Main Camera a child of Player.");
        }

        // ---- Ball ----
        Ensure<BallOwnership>(ball);

        // ---- Goal markers (the player attacks +Z, the opponent attacks -Z) ----
        MakeGoal("Goal_Away", new Vector3(0f, 0f, 18f), TeamId.Away);
        MakeGoal("Goal_Home", new Vector3(0f, 0f, -18f), TeamId.Home);

        // ---- Opponent ----
        GameObject opponent = GameObject.Find("Opponent");
        if (opponent == null)
        {
            opponent = MakeCapsule("Opponent", new Vector3(0f, 1f, 8f), "Opponent_Red", new Color(0.8f, 0.1f, 0.1f));
        }
        SetupCharacter(opponent);

        PlayerTeam opponentTeam = Ensure<PlayerTeam>(opponent);
        SetEnum(opponentTeam, "team", (int)TeamId.Away);
        OpponentDummy dummy = Ensure<OpponentDummy>(opponent);
        GameObject homeGoal = GameObject.Find("Goal_Home");
        if (homeGoal != null) SetRefIfEmpty(dummy, "goalTarget", homeGoal.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Possession, goal markers and opponent set up. Press Ctrl+S to save the scene.");
    }

    [MenuItem("FPPFootball/Add Second Opponent")]
    public static void AddSecondOpponent()
    {
        if (GameObject.Find("Ball") == null)
        {
            EditorUtility.DisplayDialog("FPPFootball", "Open your pitch scene first (no 'Ball' found).", "OK");
            return;
        }

        GameObject opponent = GameObject.Find("Opponent_2");
        if (opponent == null)
        {
            opponent = MakeCapsule("Opponent_2", new Vector3(-7f, 1f, 11f), "Opponent_Red", new Color(0.8f, 0.1f, 0.1f));
        }
        SetupCharacter(opponent);

        PlayerTeam opponentTeam = Ensure<PlayerTeam>(opponent);
        SetEnum(opponentTeam, "team", (int)TeamId.Away);
        OpponentDummy dummy = Ensure<OpponentDummy>(opponent);
        GameObject homeGoal = GameObject.Find("Goal_Home");
        if (homeGoal != null) SetRefIfEmpty(dummy, "goalTarget", homeGoal.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Second opponent added. The two opponents now pass to each other. Press Ctrl+S to save the scene.");
    }

    [MenuItem("FPPFootball/Add Test Teammate")]
    public static void AddTeammate()
    {
        if (GameObject.Find("Player") == null)
        {
            EditorUtility.DisplayDialog("FPPFootball", "Open your pitch scene first (no 'Player' found).", "OK");
            return;
        }

        GameObject mate = GameObject.Find("Teammate");
        if (mate == null)
        {
            mate = MakeCapsule("Teammate", new Vector3(5f, 1f, 2f), "Teammate_Blue", new Color(0.1f, 0.3f, 0.9f));
        }
        SetupCharacter(mate);

        PlayerTeam t = Ensure<PlayerTeam>(mate);
        SetEnum(t, "team", (int)TeamId.Home);
        SetBool(t, "canPossess", false);
        Ensure<TeammateDummy>(mate);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Test teammate added. Press Ctrl+S to save the scene.");
    }

    // ------------------------------------------------------------ helpers

    private static GameObject MakeCapsule(string name, Vector3 position, string materialName, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.position = position;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
        return go;
    }

    private static void SetupCharacter(GameObject go)
    {
        CharacterController cc = go.GetComponent<CharacterController>();
        if (cc == null) cc = Undo.AddComponent<CharacterController>(go);
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        cc.stepOffset = 0.05f;
        EditorUtility.SetDirty(cc);
    }

    private static void MakeGoal(string name, Vector3 position, TeamId defendedBy)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.position = position;
            // local +Z must point out of the pitch, behind the goal
            go.transform.rotation = position.z >= 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        }
        GoalMarker marker = Ensure<GoalMarker>(go);
        SetEnum(marker, "defendedBy", (int)defendedBy);
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    private static void SetEnum(Object target, string field, int index)
    {
        if (target == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.enumValueIndex = index;
        so.ApplyModifiedProperties();
    }

    private static void SetBool(Object target, string field, bool value)
    {
        if (target == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.boolValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetRefIfEmpty(Object target, string field, Object value)
    {
        if (target == null || value == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return;
        if (p.objectReferenceValue != null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static Material GetMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = "Assets/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
