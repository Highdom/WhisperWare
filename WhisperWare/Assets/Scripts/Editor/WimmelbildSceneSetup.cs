using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WhisperWare.Wimmelbild;
using Object = UnityEngine.Object;

public static class WimmelbildSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Wimmelbild.unity";
    private const string MaterialPath = "Assets/Scripts/Wimmelbild_Scripts/WimmelbildUnlit.mat";

    [MenuItem("Tools/Wimmelbild/Szene einrichten", false, 120)]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
        {
            Debug.LogWarning("Wimmelbild: Zuerst Assets/Scenes/Wimmelbild.unity oeffnen und den Play-Modus beenden.");
            return;
        }
        WimmelbildPuzzle existing = FindInScene<WimmelbildPuzzle>(scene);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("Wimmelbild ist bereits eingerichtet. Aendere die Objektliste im Inspector.", existing);
            return;
        }

        var names = new[] { "Triangle", "Square", "Capsule", "Circle" };
        var sources = new List<SpriteRenderer>();
        foreach (string name in names)
        {
            SpriteRenderer sprite = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .FirstOrDefault(candidate => candidate.name == name);
            if (sprite == null || sprite.sprite == null)
                throw new InvalidOperationException("Wimmelbild: Vorhandenes Objekt fehlt: " + name);
            sources.Add(sprite);
        }
        Camera camera = FindInScene<Camera>(scene);
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (camera == null || font == null) throw new InvalidOperationException("Wimmelbild: Kamera oder TMP-Standardschrift fehlt.");

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Wimmelbild einrichten");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP Sprite-Unlit-Shader fehlt.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        var root = Create("WimmelbildPuzzle", null);
        var puzzle = Undo.AddComponent<WimmelbildPuzzle>(root);
        Transform objects = Create("Objekte", root.transform).transform;
        Transform targets = Create("Zielplaetze", root.transform).transform;
        targets.position = new Vector3(0f, 1.45f, 0f);
        var orderedItems = new List<WimmelbildItem>();
        var colors = new[] { new Color(1f, 0.72f, 0.35f), new Color(0.40f, 0.81f, 0.95f), new Color(0.80f, 0.62f, 1f), new Color(0.49f, 0.88f, 0.68f) };
        for (int i = 0; i < sources.Count; i++)
        {
            SpriteRenderer source = sources[i];
            Undo.SetTransformParent(source.transform, objects, "Wimmelbild-Objekte gruppieren");
            Undo.RecordObject(source, "Wimmelbild-Sprites sichtbar machen");
            source.sharedMaterial = material;
            source.color = colors[i];
            source.sortingOrder = 5;
            WimmelbildItem item = source.GetComponent<WimmelbildItem>();
            if (item == null) item = Undo.AddComponent<WimmelbildItem>(source.gameObject);
            var collider = item.GetComponent<BoxCollider2D>();
            Undo.RecordObject(collider, "Wimmelbild-Klickflaeche anpassen");
            collider.size = source.sprite.bounds.size;
            collider.offset = source.sprite.bounds.center;
            collider.isTrigger = true;
            orderedItems.Add(item);
        }

        // Vorhandene Runensprites dienen als austauschbare Ablenkungsobjekte.
        Sprite[] runes = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/2D-SymboleNoBackground.png").OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal).Take(9).ToArray();
        Vector2[] positions = { new Vector2(-7.1f, 0f), new Vector2(-4.8f, -3.2f), new Vector2(-4.6f, -0.1f),
            new Vector2(-1.9f, -3.3f), new Vector2(-0.9f, -0.3f), new Vector2(2.2f, -3.2f),
            new Vector2(3f, -0.1f), new Vector2(6.6f, -2.9f), new Vector2(6.8f, 0.2f) };
        for (int i = 0; i < runes.Length; i++)
        {
            var rune = Create("Ablenkung " + runes[i].name, objects);
            rune.transform.position = positions[i];
            var renderer = Undo.AddComponent<SpriteRenderer>(rune);
            renderer.sprite = runes[i];
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 3;
            renderer.color = new Color(0.73f, 0.79f, 0.87f);
            float extent = Mathf.Max(runes[i].bounds.size.x, runes[i].bounds.size.y);
            rune.transform.localScale = Vector3.one * (0.72f / extent);
            Undo.AddComponent<WimmelbildItem>(rune);
            var collider = rune.GetComponent<BoxCollider2D>();
            collider.size = runes[i].bounds.size;
            collider.offset = runes[i].bounds.center;
            collider.isTrigger = true;
        }

        var template = Create("Zielvorlage (automatisch kopiert)", root.transform);
        var slot = Undo.AddComponent<WimmelbildSlot>(template);
        template.GetComponent<BoxCollider2D>().size = new Vector2(1.65f, 1.65f);
        template.GetComponent<BoxCollider2D>().isTrigger = true;
        var tile = Create("Flaeche", template.transform);
        var background = Undo.AddComponent<SpriteRenderer>(tile);
        background.sprite = sources[1].sprite;
        background.sharedMaterial = material;
        background.sortingOrder = -5;
        tile.transform.localScale = new Vector3(1.65f / background.sprite.bounds.size.x, 1.65f / background.sprite.bounds.size.y, 1f);
        var labelObject = Create("Nummer und Objekt", template.transform, typeof(RectTransform));
        var label = Undo.AddComponent<TextMeshPro>(labelObject);
        label.font = font;
        label.fontSize = 2.8f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = new Color(0.90f, 0.94f, 1f);
        label.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
        label.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        label.GetComponent<MeshRenderer>().sortingOrder = 10;
        var slotData = new SerializedObject(slot);
        slotData.FindProperty("background").objectReferenceValue = background;
        slotData.FindProperty("label").objectReferenceValue = label;
        slotData.ApplyModifiedPropertiesWithoutUndo();
        template.SetActive(false);

        Undo.RecordObject(camera, "Wimmelbild-Hintergrund");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.075f, 0.12f);

        var canvasObject = Create("Spielanzeige", root.transform, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        TextMeshProUGUI title = Text("Titel", canvas.transform, font, "WIMMELBILD", 34f);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(38f, -25f), new Vector2(570f, 50f));
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI subtitle = Text("Anleitung", canvas.transform, font, "Finde die Formen. Ordne sie von links nach rechts ein.", 21f);
        Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -78f), new Vector2(1050f, 36f));
        TextMeshProUGUI progress = Text("Fortschritt", canvas.transform, font, "0 / 4 gefunden", 25f);
        progress.alignment = TextAlignmentOptions.MidlineRight;
        Place(progress.rectTransform, Vector2.one, new Vector2(-40f, -30f), new Vector2(320f, 45f));
        TextMeshProUGUI feedback = Text("Rueckmeldung", canvas.transform, font, "Ziehe die Objekte auf die nummerierten Zielplaetze.", 22f);
        Place(feedback.rectTransform, Vector2.zero, new Vector2(40f, 25f), new Vector2(1160f, 65f));

        var buttonObject = Create("Neu starten", canvas.transform, typeof(RectTransform), typeof(Image), typeof(Button));
        Place(buttonObject.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-40f, 35f), new Vector2(210f, 56f));
        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.22f, 0.34f, 0.48f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        var buttonLabel = Text("Text", button.transform, font, "Neu starten", 23f);
        buttonLabel.rectTransform.anchorMin = Vector2.zero;
        buttonLabel.rectTransform.anchorMax = Vector2.one;
        buttonLabel.rectTransform.offsetMin = Vector2.zero;
        buttonLabel.rectTransform.offsetMax = Vector2.zero;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        UnityEventTools.AddPersistentListener(button.onClick, puzzle.ResetPuzzle);

        if (FindInScene<EventSystem>(scene) == null)
            Create("EventSystem", root.transform, typeof(EventSystem), typeof(InputSystemUIInputModule));

        var data = new SerializedObject(puzzle);
        data.FindProperty("solutionOrder").arraySize = orderedItems.Count;
        for (int i = 0; i < orderedItems.Count; i++)
            data.FindProperty("solutionOrder").GetArrayElementAtIndex(i).objectReferenceValue = orderedItems[i];
        data.FindProperty("gameCamera").objectReferenceValue = camera;
        data.FindProperty("itemsRoot").objectReferenceValue = objects;
        data.FindProperty("slotsRoot").objectReferenceValue = targets;
        data.FindProperty("slotTemplate").objectReferenceValue = slot;
        data.FindProperty("progressText").objectReferenceValue = progress;
        data.FindProperty("feedbackText").objectReferenceValue = feedback;
        data.ApplyModifiedPropertiesWithoutUndo();
        puzzle.RebuildSlots();
        foreach (WimmelbildSlot generated in puzzle.Slots) Undo.RegisterCreatedObjectUndo(generated.gameObject, "Ziel erzeugen");
        EditorUtility.SetDirty(puzzle);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Wimmelbild konnte nicht gespeichert werden.");
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root;
        Debug.Log("WIMMELBILD_INSTALLED | Objects=4 | Targets=4 | Configuration=valid", puzzle);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        var result = new GameObject(name, components);
        if (parent != null) result.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(result, "Wimmelbild erstellen");
        return result;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string text, float size)
    {
        var result = Create(name, parent, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        result.font = font;
        result.text = text;
        result.fontSize = size;
        result.color = new Color(0.88f, 0.92f, 0.98f);
        result.raycastTarget = false;
        result.textWrappingMode = TextWrappingModes.Normal;
        result.alignment = TextAlignmentOptions.MidlineLeft;
        return result;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
