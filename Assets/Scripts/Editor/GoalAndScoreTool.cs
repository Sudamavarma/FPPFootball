using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds real goals and the score display:
///   FPPFootball > Build Goals And Scoreboard
///
/// - For each goal marker: two posts, a crossbar and a net (back, sides and roof) with colliders,
///   so the ball hits the posts and stops in the net.
/// - A MatchManager that counts goals and resets the kickoff.
/// - A scoreboard at the top centre of the screen and a GOAL! banner.
/// Run "Set Up Possession And Opponent" first (it creates the goal markers).
/// Safe to run again: it rebuilds the frames.
/// </summary>
public static class GoalAndScoreTool
{
    private const float GoalLineZ = 18f;   // distance of each goal line from the pitch centre

    [MenuItem("FPPFootball/Build Goals And Scoreboard")]
    public static void Build()
    {
        GoalMarker[] markers = Object.FindObjectsByType<GoalMarker>(FindObjectsSortMode.None);
        if (markers.Length == 0)
        {
            EditorUtility.DisplayDialog("FPPFootball",
                "No goal markers found. Run FPPFootball > Set Up Possession And Opponent first.", "OK");
            return;
        }

        Material postMaterial = GetMaterial("Goal_Post", Color.white);
        Material netMaterial = GetNetMaterial();
        PhysicsMaterial postPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/BallPhysics.asset");
        PhysicsMaterial netPhysics = GetNetPhysics();
        if (postPhysics == null) postPhysics = netPhysics;

        foreach (GoalMarker marker in markers)
        {
            Place(marker);
            BuildFrame(marker, postMaterial, netMaterial, postPhysics, netPhysics);
        }

        GameObject manager = GameObject.Find("MatchManager");
        if (manager == null)
        {
            manager = new GameObject("MatchManager");
            Undo.RegisterCreatedObjectUndo(manager, "Create MatchManager");
        }
        if (manager.GetComponent<MatchManager>() == null) Undo.AddComponent<MatchManager>(manager);

        bool scoreboard = BuildScoreboard();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        string message = "Goals built for " + markers.Length + " goal marker(s), and the MatchManager is in the scene.";
        message += scoreboard
            ? "\nScoreboard added at the top centre of the touch canvas."
            : "\nNo Canvas found, so no scoreboard was added. Open the scene with your touch controls and run this again.";
        message += "\n\nIf the canvas is a prefab: select TouchControlsCanvas, open Overrides and click Apply All.";
        message += "\nPress Ctrl+S to save the scene.";
        EditorUtility.DisplayDialog("FPPFootball", message, "OK");
    }

    // ------------------------------------------------------------ goals

    // Goal line at +/-GoalLineZ, local +Z pointing out of the pitch (behind the goal)
    private static void Place(GoalMarker marker)
    {
        Undo.RecordObject(marker.transform, "Place goal");
        float sign = marker.transform.position.z >= 0f ? 1f : -1f;
        marker.transform.position = new Vector3(marker.transform.position.x, 0f, sign * GoalLineZ);
        marker.transform.rotation = sign > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        EditorUtility.SetDirty(marker.transform);
    }

    private static void BuildFrame(GoalMarker marker, Material post, Material net, PhysicsMaterial postPhysics, PhysicsMaterial netPhysics)
    {
        Transform old = marker.transform.Find("GoalFrame");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject root = new GameObject("GoalFrame");
        Undo.RegisterCreatedObjectUndo(root, "Create goal frame");
        root.transform.SetParent(marker.transform, false);

        float w = marker.Width;
        float h = marker.Height;
        float d = marker.Depth;
        const float t = 0.12f;    // post thickness
        const float n = 0.15f;    // net collider thickness

        MakePart(root.transform, "Post_Left", new Vector3(-(w * 0.5f + t * 0.5f), (h + t) * 0.5f, 0f), new Vector3(t, h + t, t), post, postPhysics);
        MakePart(root.transform, "Post_Right", new Vector3(w * 0.5f + t * 0.5f, (h + t) * 0.5f, 0f), new Vector3(t, h + t, t), post, postPhysics);
        MakePart(root.transform, "Crossbar", new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + 2f * t, t, t), post, postPhysics);

        MakePart(root.transform, "Net_Back", new Vector3(0f, h * 0.5f, d), new Vector3(w + 2f * t, h + t, n), net, netPhysics);
        MakePart(root.transform, "Net_Left", new Vector3(-(w * 0.5f + t * 0.5f), h * 0.5f, d * 0.5f), new Vector3(n, h + t, d), net, netPhysics);
        MakePart(root.transform, "Net_Right", new Vector3(w * 0.5f + t * 0.5f, h * 0.5f, d * 0.5f), new Vector3(n, h + t, d), net, netPhysics);
        MakePart(root.transform, "Net_Roof", new Vector3(0f, h + t * 0.5f, d * 0.5f), new Vector3(w + 2f * t, n, d), net, netPhysics);
    }

    private static void MakePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, PhysicsMaterial physics)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Collider c = go.GetComponent<Collider>();
        if (physics != null) c.sharedMaterial = physics;
    }

    // ------------------------------------------------------------ scoreboard

    private static bool BuildScoreboard()
    {
        TouchLookArea lookArea = Object.FindFirstObjectByType<TouchLookArea>();
        Canvas canvas = lookArea != null ? lookArea.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return false;
        canvas = canvas.rootCanvas;

        float scale = 1f;
        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs != null && cs.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            scale = Mathf.Max(0.5f, cs.referenceResolution.y / 1080f);

        // Panel at the top centre
        Transform existing = canvas.transform.Find("ScoreboardUI");
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
        }
        else
        {
            root = new GameObject("ScoreboardUI", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create scoreboard");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f * scale);
            rt.sizeDelta = new Vector2(660f, 96f) * scale;

            Image bg = root.GetComponent<Image>();
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;
        }

        // Score text
        TMP_Text score;
        Transform scoreT = root.transform.Find("ScoreText");
        if (scoreT == null)
        {
            GameObject go = new GameObject("ScoreText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create score text");
            go.transform.SetParent(root.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 54f * scale;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.text = "HOME   0 - 0   AWAY";
            score = tmp;
        }
        else
        {
            score = scoreT.GetComponent<TMP_Text>();
        }

        // GOAL! banner in the middle of the screen (hidden until a goal)
        TMP_Text banner;
        Transform bannerT = canvas.transform.Find("GoalBanner");
        if (bannerT == null)
        {
            GameObject go = new GameObject("GoalBanner", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create goal banner");
            go.transform.SetParent(canvas.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 140f * scale);
            rt.sizeDelta = new Vector2(1100f, 320f) * scale;
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 130f * scale;
            tmp.color = new Color(1f, 0.9f, 0.2f, 1f);
            tmp.raycastTarget = false;
            tmp.text = "GOAL!";
            go.SetActive(false);
            banner = tmp;
        }
        else
        {
            banner = bannerT.GetComponent<TMP_Text>();
        }

        ScoreboardUI board = root.GetComponent<ScoreboardUI>();
        if (board == null) board = Undo.AddComponent<ScoreboardUI>(root);
        SetRef(board, "scoreText", score);
        SetRef(board, "bannerText", banner);
        return true;
    }

    // ------------------------------------------------------------ assets

    private static void SetRef(Object target, string field, Object value)
    {
        if (target == null || value == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return;
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
        mat.SetFloat("_Smoothness", 0.4f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Semi-transparent net
    private static Material GetNetMaterial()
    {
        Material mat = GetMaterial("Goal_Net", new Color(1f, 1f, 1f, 0.28f));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static PhysicsMaterial GetNetPhysics()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = "Assets/Materials/NetPhysics.asset";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("NetPhysics");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0.6f;
        pm.staticFriction = 0.6f;
        pm.bounciness = 0.05f;   // a net swallows the ball
        EditorUtility.SetDirty(pm);
        return pm;
    }
}
