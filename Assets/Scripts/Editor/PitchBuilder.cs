using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PitchBuilder
{
    // Pitch size in meters (Width = X, Length = Z). Change and re-run to resize.
    private const float Width = 24f;
    private const float Length = 40f;
    private const float WallHeight = 6f;
    private const float BallSize = 0.3f; // real football is about 0.22

    [MenuItem("FPPFootball/Build Test Pitch")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material grassMat = GetMaterial("Pitch_Grass", new Color(0.18f, 0.55f, 0.22f));
        Material wallMat = GetMaterial("Pitch_Wall", new Color(0.35f, 0.38f, 0.42f));
        Material lineMat = GetMaterial("Pitch_Line", Color.white);
        Material ballMat = GetMaterial("Ball_White", new Color(0.95f, 0.95f, 0.95f));
        PhysicsMaterial ballPhysics = GetBallPhysics();

        RemoveIfExists("Pitch");
        RemoveIfExists("Plane");

        GameObject root = new GameObject("Pitch");
        Undo.RegisterCreatedObjectUndo(root, "Build Test Pitch");

        float halfW = Width / 2f;
        float halfL = Length / 2f;
        float y = 0.011f;
        float t = 0.2f; // line thickness

        // Ground (top surface at y = 0)
        MakeBlock("Ground", root.transform, new Vector3(0f, -0.5f, 0f),
            new Vector3(Width + 2f, 1f, Length + 2f), grassMat, null, true);

        // Walls
        MakeBlock("Wall_Left", root.transform, new Vector3(-halfW - 0.5f, WallHeight / 2f, 0f),
            new Vector3(1f, WallHeight, Length + 2f), wallMat, ballPhysics, true);
        MakeBlock("Wall_Right", root.transform, new Vector3(halfW + 0.5f, WallHeight / 2f, 0f),
            new Vector3(1f, WallHeight, Length + 2f), wallMat, ballPhysics, true);
        MakeBlock("Wall_Back", root.transform, new Vector3(0f, WallHeight / 2f, -halfL - 0.5f),
            new Vector3(Width + 2f, WallHeight, 1f), wallMat, ballPhysics, true);
        MakeBlock("Wall_Front", root.transform, new Vector3(0f, WallHeight / 2f, halfL + 0.5f),
            new Vector3(Width + 2f, WallHeight, 1f), wallMat, ballPhysics, true);

        // Border lines and center line (no colliders)
        MakeBlock("Line_Left", root.transform, new Vector3(-halfW + t / 2f, y, 0f),
            new Vector3(t, 0.02f, Length), lineMat, null, false);
        MakeBlock("Line_Right", root.transform, new Vector3(halfW - t / 2f, y, 0f),
            new Vector3(t, 0.02f, Length), lineMat, null, false);
        MakeBlock("Line_Back", root.transform, new Vector3(0f, y, -halfL + t / 2f),
            new Vector3(Width, 0.02f, t), lineMat, null, false);
        MakeBlock("Line_Front", root.transform, new Vector3(0f, y, halfL - t / 2f),
            new Vector3(Width, 0.02f, t), lineMat, null, false);
        MakeBlock("Line_Center", root.transform, new Vector3(0f, y, 0f),
            new Vector3(Width, 0.02f, t), lineMat, null, false);

        // Center spot
        GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spot.name = "CenterSpot";
        spot.transform.SetParent(root.transform, false);
        spot.transform.localPosition = new Vector3(0f, y + 0.001f, 0f);
        spot.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
        spot.GetComponent<Renderer>().sharedMaterial = lineMat;
        Object.DestroyImmediate(spot.GetComponent<Collider>());

        // Ball (reuses your existing "Ball" if there is one)
        GameObject ball = GameObject.Find("Ball");
        if (ball == null)
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            Undo.RegisterCreatedObjectUndo(ball, "Create Ball");
        }
        ball.transform.position = new Vector3(0f, 0.2f, 0f);
        ball.transform.localScale = Vector3.one * BallSize;
        ball.GetComponent<Renderer>().sharedMaterial = ballMat;
        ball.GetComponent<Collider>().sharedMaterial = ballPhysics;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.43f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Put the player on the pitch facing the ball
        GameObject player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(0f, 1f, -4f);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Test pitch built. Press Ctrl+S to save the scene.");
    }

    private static GameObject MakeBlock(string name, Transform parent, Vector3 pos, Vector3 scale,
        Material mat, PhysicsMaterial phys, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        Collider col = go.GetComponent<Collider>();
        if (!keepCollider) Object.DestroyImmediate(col);
        else if (phys != null) col.sharedMaterial = phys;
        return go;
    }

    private static void RemoveIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    private static Material GetMaterial(string name, Color color)
    {
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
        mat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static PhysicsMaterial GetBallPhysics()
    {
        string path = "Assets/Materials/BallPhysics.asset";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("BallPhysics");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0.4f;
        pm.staticFriction = 0.4f;
        pm.bounciness = 0.6f;
        EditorUtility.SetDirty(pm);
        return pm;
    }
}
