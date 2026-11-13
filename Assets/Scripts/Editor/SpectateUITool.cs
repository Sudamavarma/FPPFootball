using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a working look-around ("spectate") slider on the touch canvas, wired up correctly:
///   FPPFootball > Build Spectate Slider UI
///
/// - A pill-shaped track at the bottom centre with a round knob. Hold and slide left or right to swing
///   the camera; let go and the knob springs back.
/// - Any older SpectateSlider in the scene is switched off (not deleted).
/// - The Player's PlayerInputReader is pointed at the new slider.
/// - An EventSystem is added if the scene has none.
/// Safe to run again: it reuses the slider it made before.
/// </summary>
public static class SpectateUITool
{
    private const string SliderName = "SpectateSliderUI";

    [MenuItem("FPPFootball/Build Spectate Slider UI")]
    public static void Build()
    {
        // ---- 1. Find the touch canvas ----
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

        // ---- 2. Create (or reuse) the slider ----
        Transform existing = canvas.transform.Find(SliderName);
        GameObject track = existing != null ? existing.gameObject : CreateTrack(canvas.transform);

        SpectateSlider slider = track.GetComponent<SpectateSlider>();
        if (slider == null) slider = Undo.AddComponent<SpectateSlider>(track);

        Transform knob = track.transform.Find("Knob");
        if (knob == null) knob = CreateKnob(track.transform);
        SetRef(slider, "knob", knob);

        // Last in the list = drawn on top of the look area, so it gets the touches
        track.transform.SetAsLastSibling();

        // ---- 3. Switch off any older sliders ----
        string disabledNames = "";
        SpectateSlider[] all = Object.FindObjectsByType<SpectateSlider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpectateSlider other in all)
        {
            if (other == slider || !other.gameObject.activeSelf) continue;
            Undo.RecordObject(other.gameObject, "Disable old spectate slider");
            other.gameObject.SetActive(false);
            EditorUtility.SetDirty(other.gameObject);
            disabledNames += " " + other.gameObject.name;
        }

        // ---- 4. Point the Player's input reader at the new slider ----
        PlayerInputReader reader = Object.FindFirstObjectByType<PlayerInputReader>();
        if (reader != null)
        {
            SerializedObject so = new SerializedObject(reader);
            SerializedProperty p = so.FindProperty("spectateSlider");
            if (p != null)
            {
                p.objectReferenceValue = slider;
                so.ApplyModifiedProperties();
            }
        }
        else
        {
            Debug.LogWarning("SpectateUITool: no PlayerInputReader found. Open the scene that has the Player.");
        }

        // ---- 5. Make sure touches can reach the UI ----
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            Debug.LogWarning("SpectateUITool: the EventSystem has no 'Input System UI Input Module'. " +
                             "Select it and use the Replace with InputSystemUIInputModule button.", eventSystem);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = track;
        EditorGUIUtility.PingObject(track);

        string message = "Spectate slider built under '" + canvas.name + "'.";
        if (disabledNames.Length > 0) message += "\nOld slider(s) switched off:" + disabledNames + ".";
        if (PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))
            message += "\n\nThe canvas is a prefab. To keep this slider in the prefab (so every scene gets it), " +
                       "select the canvas in the Hierarchy, open the Overrides dropdown in the Inspector and click Apply All.";
        message += "\n\nPress Ctrl+S to save the scene.";
        EditorUtility.DisplayDialog("FPPFootball", message, "OK");
    }

    // ------------------------------------------------------------ builders

    private static GameObject CreateTrack(Transform parent)
    {
        GameObject track = new GameObject(SliderName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(track, "Create spectate slider");
        track.transform.SetParent(parent, false);

        // Bottom centre, 560 x 90, centre pivot (the slider script needs the pivot in the middle)
        RectTransform rt = track.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta = new Vector2(560f, 90f);

        Image image = track.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.35f);
        image.raycastTarget = true; // the track receives the touches

        // Small label in the middle
        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(track.transform, false);
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI text = Undo.AddComponent<TextMeshProUGUI>(label);
        text.text = "LOOK";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;
        text.color = new Color(1f, 1f, 1f, 0.8f);
        text.raycastTarget = false;

        return track;
    }

    private static Transform CreateKnob(Transform parent)
    {
        GameObject knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(knob, "Create spectate knob");
        knob.transform.SetParent(parent, false);

        RectTransform rt = knob.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(110f, 110f);

        Image image = knob.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        image.color = new Color(1f, 1f, 1f, 0.95f);
        image.raycastTarget = false; // must stay off, the track gets the touches

        knob.transform.SetAsLastSibling(); // on top of the label
        return knob.transform;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        if (target == null || value == null) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
