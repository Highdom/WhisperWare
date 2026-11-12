using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WhisperWare.Wimmelbild;

/// <summary>Integrationstest im Play-Modus; stellt die urspruengliche Konfiguration wieder her.</summary>
public static class WimmelbildVerification
{
    [MenuItem("Tools/Wimmelbild/Spielregeln pruefen (Play-Modus) %#k", false, 121)]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Wimmelbild-Tests: Zuerst Play starten."); return; }
        WimmelbildPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<WimmelbildPuzzle>();
        if (puzzle == null) throw new InvalidOperationException("Kein WimmelbildPuzzle geladen.");
        var data = new SerializedObject(puzzle);
        WimmelbildItem[] original = puzzle.SolutionOrder.ToArray();
        bool originalSequence = data.FindProperty("enforceSequence").boolValue;
        var report = new List<string>();
        int completionCount = 0;
        var completion = (UnityEvent)typeof(WimmelbildPuzzle).GetField("onPuzzleCompleted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(puzzle);
        UnityAction countCompletion = () => completionCount++;
        completion.AddListener(countCompletion);

        void Check(bool condition, string description)
        {
            report.Add((condition ? "PASS " : "FAIL ") + description);
            if (!condition) throw new InvalidOperationException(description);
        }
        void Configure(WimmelbildItem[] order, bool sequential)
        {
            data.Update();
            var list = data.FindProperty("solutionOrder");
            list.arraySize = order.Length;
            for (int i = 0; i < order.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = order[i];
            data.FindProperty("enforceSequence").boolValue = sequential;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        try
        {
            Check(original.Length >= 2, "Testszene besitzt mindestens zwei Objekte");
            Configure(original, true);
            puzzle.ResetPuzzle();
            Vector3 firstPosition = original[0].transform.position;
            Vector3 firstScale = original[0].transform.localScale;
            Check(puzzle.Slots.Count == original.Length && puzzle.PlacedCount == 0, "Zielanzahl entspricht der Liste");
            Check(!puzzle.TryPlace(original[1], puzzle.Slots[1]) && puzzle.PlacedCount == 0, "Spaeteres Objekt wird zuerst abgewiesen");
            Check(!puzzle.TryPlace(original[0], puzzle.Slots[1]), "Falsches Ziel wird abgewiesen");
            original[0].transform.position = new Vector3(8f, 8f, 0f);
            Check(!puzzle.TryPlace(original[0], null) && original[0].transform.position == firstPosition, "Ablage ausserhalb der Ziele setzt Position zurueck");
            WimmelbildItem distractor = puzzle.GetComponentsInChildren<WimmelbildItem>().First(item => !original.Contains(item));
            Check(!puzzle.TryPlace(distractor, puzzle.Slots[0]), "Ablenkungsobjekt wird abgewiesen");
            for (int i = 0; i < original.Length; i++)
                Check(puzzle.TryPlace(original[i], puzzle.Slots[i]), "Richtige Ablage " + (i + 1));
            Check(puzzle.IsCompleted && completionCount == 1, "Vollstaendige Loesung loest genau ein Abschlussereignis aus");
            Check(!puzzle.TryPlace(original[0], puzzle.Slots[0]) && completionCount == 1, "Bereits abgelegte Objekte zaehlen nicht erneut");
            puzzle.ResetPuzzle();
            Check(!puzzle.IsCompleted && puzzle.PlacedCount == 0 && original[0].transform.position == firstPosition && original[0].transform.localScale == firstScale, "Neustart stellt Zustand, Position und Groesse wieder her");

            Configure(new[] { original.Last() }, true);
            puzzle.ResetPuzzle();
            Check(puzzle.Slots.Count == 1 && puzzle.TryPlace(original.Last(), puzzle.Slots[0]) && puzzle.IsCompleted, "Ein einziges konfiguriertes Objekt funktioniert");
            Configure(new[] { original[1], original[0] }, true);
            puzzle.ResetPuzzle();
            Check(puzzle.Slots.Count == 2 && !puzzle.TryPlace(original[0], puzzle.Slots[0]) && puzzle.TryPlace(original[1], puzzle.Slots[0]) && puzzle.TryPlace(original[0], puzzle.Slots[1]), "Verkleinerte und umsortierte Liste bestimmt die Loesung");
            Configure(original, false);
            puzzle.ResetPuzzle();
            for (int i = original.Length - 1; i >= 0; i--)
                Check(puzzle.TryPlace(original[i], puzzle.Slots[i]), "Freie zeitliche Reihenfolge: Ziel " + (i + 1));
            Check(puzzle.IsCompleted, "Freie Reihenfolge endet ebenfalls erfolgreich");

            WimmelbildItem[] expanded = puzzle.GetComponentsInChildren<WimmelbildItem>().Take(8).ToArray();
            Configure(expanded, true);
            puzzle.ResetPuzzle();
            Check(puzzle.Slots.Count == expanded.Length && puzzle.Slots[6].transform.position.y < puzzle.Slots[0].transform.position.y, "Erweiterung auf acht Objekte erzeugt eine zweite Zielreihe");
            Configure(new WimmelbildItem[0], true);
            Check(!puzzle.ValidateConfiguration(out _), "Leere Liste wird als Konfigurationsfehler erkannt");
            Configure(new[] { original[0], original[0] }, true);
            Check(!puzzle.ValidateConfiguration(out _), "Doppelte Objektverweise werden erkannt");
            Configure(new WimmelbildItem[] { null }, true);
            Check(!puzzle.ValidateConfiguration(out _), "Fehlender Objektverweis wird erkannt");

            Configure(original, true);
            puzzle.ResetPuzzle();
            Camera camera = (Camera)data.FindProperty("gameCamera").objectReferenceValue;
            Vector2 point = camera.WorldToScreenPoint(original[0].Renderer.bounds.center);
            var raycastHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, raycastHits);
            report.Add("INPUT mouse=" + (Mouse.current != null) + " update=" + InputSystem.settings.updateMode + " screen=" + Screen.width + "x" + Screen.height + " pick=" + point + " UI=" + string.Join(",", raycastHits.Select(hit => hit.gameObject.name)));
            typeof(WimmelbildPuzzle).GetMethod("StartDrag", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(puzzle, new object[] { point, false });
            var picked = (WimmelbildItem)typeof(WimmelbildPuzzle).GetField("draggedItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(puzzle);
            Check(picked == original[0], "Zeigerposition greift das sichtbare Objekt mit Collider");

            puzzle.ResetPuzzle();
            Vector2 savedMousePosition = Mouse.current.position.ReadValue();
            Vector2 destination = camera.WorldToScreenPoint(puzzle.Slots[0].transform.position);
            // Regression: Druecken, Bewegen und Loslassen koennen im selben Frame ankommen.
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point });
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = destination }.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = destination });
            InputSystem.Update();
            typeof(WimmelbildPuzzle).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(puzzle, null);
            bool fastDragWorked = original[0].IsPlaced && puzzle.PlacedCount == 1;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = savedMousePosition });
            InputSystem.Update();
            Check(fastDragWorked, "Schnelles Ziehen innerhalb eines Frames behaelt die urspruengliche Klickposition");
            report.Add("RESULT: ALL PASSED");
            Debug.Log("WIMMELBILD_TESTS_PASSED | " + report.Count + " checks/diagnostics");
        }
        catch (Exception error)
        {
            report.Add("RESULT: FAILED | " + error);
            Debug.LogException(error);
        }
        finally
        {
            completion.RemoveListener(countCompletion);
            Configure(original, originalSequence);
            puzzle.ResetPuzzle();
            Directory.CreateDirectory("Temp");
            File.WriteAllLines("Temp/WimmelbildVerification.txt", report);
        }
    }
}
