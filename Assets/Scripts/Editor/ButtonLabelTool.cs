using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fixes the text on the touch buttons so it matches what each button really does.
///   FPPFootball > Fix Touch Button Labels
///
/// Each on-screen button is bound to one gamepad button, and PlayerInputReader decides what that
/// gamepad button means:
///   South = Shoot    East = Pass       North = Through      (ball with me)
///   West  = Tackle   LeftShoulder = Defend   RightShoulder = Clear   (loose or opponent has it)
///   RightTrigger = Sprint
/// This tool reads each button's binding and sets its text to the matching name, then shows a report
/// of every button it found (and which button set it belongs to).
/// Safe to run again.
/// </summary>
public static class ButtonLabelTool
{
    [MenuItem("FPPFootball/Fix Touch Button Labels")]
    public static void Fix()
    {
        OnScreenButton[] buttons = Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (buttons.Length == 0)
        {
            EditorUtility.DisplayDialog("FPPFootball",
                "No on-screen buttons found in the open scene. Open the scene that has your touch controls.", "OK");
            return;
        }

        // The two button sets used by TouchButtonSetSwitcher (may be empty if not assigned)
        CanvasGroup attackGroup = null;
        CanvasGroup defendGroup = null;
        TouchButtonSetSwitcher switcher = Object.FindFirstObjectByType<TouchButtonSetSwitcher>(FindObjectsInactive.Include);
        if (switcher != null)
        {
            SerializedObject so = new SerializedObject(switcher);
            attackGroup = so.FindProperty("attackingButtons")?.objectReferenceValue as CanvasGroup;
            defendGroup = so.FindProperty("defendingButtons")?.objectReferenceValue as CanvasGroup;
        }

        StringBuilder report = new StringBuilder();
        int changed = 0;

        foreach (OnScreenButton button in buttons)
        {
            string path = button.controlPath;
            string label = LabelFor(path);
            string set = SetNameOf(button.transform, attackGroup, defendGroup);

            if (label == null)
            {
                report.AppendLine("[" + set + "] " + button.name + ": unknown binding '" + path + "' (left alone)");
                continue;
            }

            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            Text legacy = tmp == null ? button.GetComponentInChildren<Text>(true) : null;

            string before;
            if (tmp != null)
            {
                before = tmp.text;
                if (before != label)
                {
                    Undo.RecordObject(tmp, "Fix button label");
                    tmp.text = label;
                    EditorUtility.SetDirty(tmp);
                    changed++;
                }
            }
            else if (legacy != null)
            {
                before = legacy.text;
                if (before != label)
                {
                    Undo.RecordObject(legacy, "Fix button label");
                    legacy.text = label;
                    EditorUtility.SetDirty(legacy);
                    changed++;
                }
            }
            else
            {
                report.AppendLine("[" + set + "] " + button.name + ": no text found inside this button");
                continue;
            }

            report.AppendLine("[" + set + "] " + button.name + ": '" + before + "' -> '" + label + "'");
        }

        string warnings = "";
        if (switcher == null)
            warnings += "\nNo TouchButtonSetSwitcher found, so the two button sets will not swap.";
        else if (attackGroup == null || defendGroup == null)
            warnings += "\nThe TouchButtonSetSwitcher is missing its Attacking Buttons or Defending Buttons group.";

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Touch button labels:\n" + report);

        string summary = changed + " label(s) changed.\n\n" + report + warnings;
        if (PrefabUtility.IsPartOfPrefabInstance(buttons[0].gameObject))
            summary += "\n\nThe buttons are part of a prefab. Select TouchControlsCanvas, open Overrides in the Inspector and click Apply All to keep these labels in the prefab.";
        summary += "\n\nPress Ctrl+S to save the scene.";
        EditorUtility.DisplayDialog("FPPFootball", summary, "OK");
    }

    private static string LabelFor(string controlPath)
    {
        if (string.IsNullOrEmpty(controlPath)) return null;
        switch (controlPath)
        {
            case "<Gamepad>/buttonSouth": return "SHOOT";
            case "<Gamepad>/buttonEast": return "PASS";
            case "<Gamepad>/buttonNorth": return "THROUGH";
            case "<Gamepad>/buttonWest": return "TACKLE";
            case "<Gamepad>/leftShoulder": return "DEFEND";
            case "<Gamepad>/rightShoulder": return "CLEAR";
            case "<Gamepad>/rightTrigger": return "SPRINT";
        }
        return null;
    }

    private static string SetNameOf(Transform t, CanvasGroup attack, CanvasGroup defend)
    {
        if (attack != null && t.IsChildOf(attack.transform)) return "attack set";
        if (defend != null && t.IsChildOf(defend.transform)) return "defend set";
        return "always visible";
    }
}
