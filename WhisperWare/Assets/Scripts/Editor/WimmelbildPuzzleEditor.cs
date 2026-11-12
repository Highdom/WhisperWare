using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using WhisperWare.Wimmelbild;

[CustomEditor(typeof(WimmelbildPuzzle))]
public sealed class WimmelbildPuzzleEditor : Editor
{
    private ReorderableList solutionList;

    private void OnEnable()
    {
        solutionList = new ReorderableList(serializedObject, serializedObject.FindProperty("solutionOrder"), true, true, true, true);
        solutionList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Gesuchte Objekte - von oben nach unten");
        solutionList.drawElementCallback = (rect, index, active, focused) =>
        {
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, solutionList.serializedProperty.GetArrayElementAtIndex(index), new GUIContent("Ziel " + (index + 1)));
        };
        solutionList.onAddCallback = list =>
        {
            int index = list.serializedProperty.arraySize++;
            list.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("+ / - bestimmt die Anzahl. Ziehe die Zeilen am linken Griff in die gewuenschte Reihenfolge. Ziehe Objekte aus der Hierarchy in die Felder. Zielplaetze werden automatisch erzeugt.", MessageType.Info);
        solutionList.DoLayoutList();
        EditorGUILayout.LabelField("Benoetigte Objekte", solutionList.count.ToString());
        Draw("enforceSequence", "Nacheinander ablegen");
        Draw("showNames", "Objektnamen an Zielen anzeigen");
        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script", "solutionOrder", "enforceSequence", "showNames", "slots");
        serializedObject.ApplyModifiedProperties();

        var puzzle = (WimmelbildPuzzle)target;
        bool valid = puzzle.ValidateConfiguration(out string error);
        if (!valid) EditorGUILayout.HelpBox(error, MessageType.Warning);
        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button(Application.isPlaying ? "Aenderungen anwenden / Neu starten" : "Zielvorschau aktualisieren"))
            {
                if (Application.isPlaying) puzzle.ResetPuzzle();
                else
                {
                    Undo.RegisterFullObjectHierarchyUndo(puzzle.gameObject, "Wimmelbild-Ziele aktualisieren");
                    puzzle.RebuildSlots();
                    EditorSceneManager.MarkSceneDirty(puzzle.gameObject.scene);
                    SceneView.RepaintAll();
                }
            }
        }
        if (Application.isPlaying)
            EditorGUILayout.LabelField("Fortschritt", puzzle.PlacedCount + " / " + puzzle.RequiredCount);
    }

    private void Draw(string field, string label)
    {
        var property = serializedObject.FindProperty(field);
        EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip));
    }
}
