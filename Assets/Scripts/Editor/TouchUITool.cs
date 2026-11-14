using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the phone controls:
///   FPPFootball > Build Touch Buttons And Floating Joystick
///
/// - A joystick that appears where your left thumb touches (replaces the old fixed stick).
/// - Three round action buttons whose NAME and FUNCTION change with the ball:
///       ball with me:            SHOOT / PASS / THROUGH
///       ball loose or opponent:  TACKLE / DEFEND / CLEAR
///   They talk straight to the game, so they do not depend on the virtual gamepad.
/// - The look area is moved behind everything else, so it can never steal taps from the buttons.
/// - The old fixed stick and the old six switching buttons are switched off (not deleted).
///   Sprint keeps working as it was.
/// Safe to run again.
/// </summary>
public static class TouchUITool
{
    [MenuItem("FPPFootball/Build Touch Buttons And Floating Joystick")]
    public static void Build()
    {
        TouchLookArea lookArea = Object.FindFirstObjectByType<TouchLookArea>();
        Canvas canvas = lookArea != null ? lookArea.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("FPPFootball",
                "No Canvas found in the open scene. Open the scene that has your touch controls first.", "OK");
            return;
        }
        canvas = canvas.rootCanvas;
        float scale = UiScale(canvas);

        StringBuilder report = new StringBuilder();

        // ---- 1. Look area goes to the very back ----
        Transform lookTop = null;
        if (lookArea != null)
        {
            Transform top = lookArea.transform;
            while (top.parent != null && top.parent != canvas.transform) top = top.parent;
            if (top.parent == canvas.transform)
            {
                top.SetAsFirstSibling();
                lookTop = top;
                report.AppendLine("Look area moved behind all the buttons.");
            }
        }

        // ---- 2. Floating joystick (just above the look area, so it gets the left-half touches) ----
        BuildJoystick(canvas.transform, scale, lookTop);
        report.AppendLine("Floating joystick built (left half of the screen).");

        // ---- 3. Three smart action buttons ----
        Vector2[] anchors = { new Vector2(0.745f, 0.17f), new Vector2(0.675f, 0.40f), new Vector2(0.80f, 0.50f) };
        float[] sizes = { 220f, 190f, 190f };
        TouchAction[] withBall = { TouchAction.Shoot, TouchAction.Pass, TouchAction.Through };
        TouchAction[] otherwise = { TouchAction.Tackle, TouchAction.Defend, TouchAction.Clear };
        for (int i = 0; i < 3; i++)
            BuildActionButton(canvas.transform, "ActionButton_" + (i + 1), anchors[i], sizes[i] * scale, withBall[i], otherwise[i]);
        report.AppendLine("Action buttons built: SHOOT/TACKLE, PASS/DEFEND, THROUGH/CLEAR.");

        // ---- 4. Switch off the old controls ----
        int hidden = 0;
        foreach (OnScreenButton b in Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.controlPath == "<Gamepad>/rightTrigger") continue;   // Sprint stays
            if (!b.gameObject.activeSelf) continue;
            Undo.RecordObject(b.gameObject, "Disable old button");
            b.gameObject.SetActive(false);
            EditorUtility.SetDirty(b.gameObject);
            hidden++;
        }
        foreach (OnScreenStick st in Object.FindObjectsByType<OnScreenStick>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject toHide = st.gameObject;
            Transform parent = st.transform.parent;
            if (parent != null && parent != canvas.transform && parent.GetComponent<Image>() != null
                && parent.GetComponentsInChildren<OnScreenControl>(true).Length == 1)
                toHide = parent.gameObject;
            if (!toHide.activeSelf) continue;
            Undo.RecordObject(toHide, "Disable old joystick");
            toHide.SetActive(false);
            EditorUtility.SetDirty(toHide);
            hidden++;
        }
        report.AppendLine("Old buttons and fixed stick switched off: " + hidden + " (Sprint kept).");

        // ---- 5. Order: buttons and slider on top ----
        for (int i = 1; i <= 3; i++)
        {
            Transform t = canvas.transform.Find("ActionButton_" + i);
            if (t != null) t.SetAsLastSibling();
        }
        Transform slider = canvas.transform.Find("SpectateSliderUI");
        if (slider != null) slider.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string message = report.ToString();
        if (PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))
            message += "\nThe canvas is a prefab. Select TouchControlsCanvas, open Overrides in the Inspector and click Apply All so every scene gets these controls.";
        message += "\nPress Ctrl+S to save the scene.";
        EditorUtility.DisplayDialog("FPPFootball", message, "OK");
    }

    // ------------------------------------------------------------ builders

    private static float UiScale(Canvas canvas)
    {
        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs != null && cs.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return Mathf.Max(0.5f, cs.referenceResolution.y / 1080f);
        return 1f;
    }

    private static GameObject FindOrCreate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void BuildJoystick(Transform canvas, float scale, Transform lookTop)
    {
        GameObject areaGo = FindOrCreate(canvas, "FloatingJoystickArea");
        RectTransform area = areaGo.GetComponent<RectTransform>();
        area.anchorMin = new Vector2(0f, 0f);
        area.anchorMax = new Vector2(0.5f, 1f);
        area.pivot = new Vector2(0.5f, 0.5f);
        area.offsetMin = Vector2.zero;
        area.offsetMax = Vector2.zero;

        Image areaImage = areaGo.GetComponent<Image>();
        areaImage.color = new Color(1f, 1f, 1f, 0.01f);   // practically invisible, but never fully transparent
        areaImage.raycastTarget = true;
        CanvasRenderer areaRenderer = areaGo.GetComponent<CanvasRenderer>();
        if (areaRenderer != null) areaRenderer.cullTransparentMesh = false;   // a culled mesh cannot catch touches

        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject baseGo = FindOrCreate(areaGo.transform, "Base");
        RectTransform baseRt = baseGo.GetComponent<RectTransform>();
        baseRt.anchorMin = baseRt.anchorMax = new Vector2(0.5f, 0.5f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.sizeDelta = new Vector2(300f, 300f) * scale;
        Image baseImage = baseGo.GetComponent<Image>();
        baseImage.sprite = circle;
        baseImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        baseImage.raycastTarget = false;

        GameObject knobGo = FindOrCreate(baseGo.transform, "Knob");
        RectTransform knobRt = knobGo.GetComponent<RectTransform>();
        knobRt.anchorMin = knobRt.anchorMax = new Vector2(0.5f, 0.5f);
        knobRt.pivot = new Vector2(0.5f, 0.5f);
        knobRt.anchoredPosition = Vector2.zero;
        knobRt.sizeDelta = new Vector2(130f, 130f) * scale;
        Image knobImage = knobGo.GetComponent<Image>();
        knobImage.sprite = circle;
        knobImage.color = new Color(1f, 1f, 1f, 0.95f);
        knobImage.raycastTarget = false;

        FloatingJoystick joystick = areaGo.GetComponent<FloatingJoystick>();
        if (joystick == null) joystick = Undo.AddComponent<FloatingJoystick>(areaGo);
        SetRef(joystick, "joystickBase", baseRt);
        SetRef(joystick, "knob", knobRt);
        SetFloat(joystick, "radius", 110f * scale);
        SetVector2(joystick, "idleMargin", new Vector2(280f, 280f) * scale);

        // Directly above the look area (which is at the back), below every button
        if (lookTop != null && lookTop.parent == canvas)
            areaGo.transform.SetSiblingIndex(lookTop.GetSiblingIndex() + 1);
        else
            areaGo.transform.SetAsFirstSibling();
    }

    private static void BuildActionButton(Transform canvas, string name, Vector2 anchor, float size,
        TouchAction withBall, TouchAction otherwise)
    {
        GameObject go = FindOrCreate(canvas, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);

        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        image.color = new Color(1f, 1f, 1f, 0.85f);
        image.raycastTarget = true;

        TMP_Text text;
        Transform labelT = go.transform.Find("Label");
        if (labelT == null)
        {
            GameObject label = new GameObject("Label", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(label, "Create button label");
            label.transform.SetParent(go.transform, false);
            RectTransform lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 10f);
            lrt.offsetMax = new Vector2(-10f, -10f);
            TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(label);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 44f;
            tmp.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            tmp.raycastTarget = false;
            tmp.text = withBall.ToString().ToUpperInvariant();
            text = tmp;
        }
        else
        {
            text = labelT.GetComponent<TMP_Text>();
        }

        TouchActionButton button = go.GetComponent<TouchActionButton>();
        if (button == null) button = Undo.AddComponent<TouchActionButton>(go);
        SetRef(button, "label", text);
        SetRef(button, "background", image);
        SetEnum(button, "ballWithMeAction", (int)withBall);
        SetEnum(button, "otherwiseAction", (int)otherwise);
    }

    // ------------------------------------------------------------ serialized helpers

    private static void SetRef(Object target, string field, Object value)
    {
        if (target == null || value == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetEnum(Object target, string field, int index)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.enumValueIndex = index;
        so.ApplyModifiedProperties();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.floatValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetVector2(Object target, string field, Vector2 value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) return;
        p.vector2Value = value;
        so.ApplyModifiedProperties();
    }
}
