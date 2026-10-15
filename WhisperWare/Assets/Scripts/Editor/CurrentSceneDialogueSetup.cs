#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SimpleDialogue.Editor
{
    /// <summary>Adds the dialogue UI to Start while retaining the existing manager and scene.</summary>
    public static class CurrentSceneDialogueSetup
    {
        private const string ScenePath = "Assets/Scenes/Start.unity";
        private const string AssetFolder = "Assets/Dialogue";
        private static readonly Color Ink = new Color(0.055f, 0.085f, 0.13f, 0.97f);
        private static readonly Color Text = new Color(0.94f, 0.97f, 0.99f);
        private static readonly Color Accent = new Color(0.25f, 0.88f, 0.74f);

        [MenuItem("Tools/Simple Dialogue/In aktuelle Szene einbauen", false, 110)]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Dialog-Einbau: Beende zuerst den Play-Modus.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                Debug.LogError("Dialog-Einbau: Öffne und aktiviere zuerst " + ScenePath + ".");
                return;
            }

            DialogueManager manager = FindExistingManager(scene);
            if (manager == null)
            {
                Debug.LogError("Dialog-Einbau: Der bestehende DialogueManager auf 'DialogMenager' fehlt oder ist nicht eindeutig.");
                return;
            }

            SerializedObject managerSettings = new SerializedObject(manager);
            SerializedProperty currentPanel = managerSettings.FindProperty("dialoguePanel");
            if (currentPanel == null)
                throw new InvalidOperationException("DialogueManager hat kein Feld 'dialoguePanel'.");
            if (currentPanel.objectReferenceValue != null)
            {
                Selection.activeGameObject = manager.gameObject;
                Debug.Log("Dialog-Einbau: Der Manager ist bereits mit einem Dialogfenster verbunden; es wurde nichts doppelt erstellt.", manager);
                return;
            }

            TMP_FontAsset font = TMP_Settings.instance == null ? null : TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("Dialog-Einbau: In den TMP Settings muss eine Default Font Asset zugewiesen sein.");
                return;
            }

            bool hasEventSystem = false;
            foreach (EventSystem events in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (events.isActiveAndEnabled && events.GetComponent<BaseInputModule>() != null)
                    hasEventSystem = true;
            if (!hasEventSystem)
            {
                Debug.LogError("Dialog-Einbau: Ein aktives EventSystem mit Eingabemodul wird benötigt.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Dialog in aktuelle Szene einbauen");
            string dialoguePath = null;
            bool setupComplete = false;
            try
            {
                EnsureFolder(AssetFolder);
                dialoguePath = AssetDatabase.GenerateUniqueAssetPath(AssetFolder + "/StartDialog.asset");
                DialogueAsset dialogue = CreateDialogue(dialoguePath);

                GameObject canvasObject = CreateObject("DialogueCanvas", null, scene,
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                RectTransform panel = CreateImage("DialoguePanel", canvas.transform, scene, Ink);
                panel.anchorMin = new Vector2(0.06f, 0.06f);
                panel.anchorMax = new Vector2(0.94f, 0.06f);
                panel.pivot = new Vector2(0.5f, 0f);
                panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(0f, 360f);
                panel.GetComponent<Image>().raycastTarget = true;

                RectTransform accent = CreateImage("Accent", panel, scene, Accent);
                accent.anchorMin = Vector2.zero;
                accent.anchorMax = new Vector2(0f, 1f);
                accent.pivot = new Vector2(0f, 0.5f);
                accent.anchoredPosition = Vector2.zero;
                accent.sizeDelta = new Vector2(4f, 0f);

                TMP_Text speaker = CreateText("SpeakerText", panel, scene, font, "Wache", 23f, Accent);
                SetTopStretch(speaker.rectTransform, 32f, 24f, 32f, 36f);
                speaker.fontStyle = FontStyles.Bold;
                TMP_Text body = CreateText("BodyText", panel, scene, font,
                    "Der Dialog startet im Play-Modus. Du kannst ihn auch über 'Dialog starten' erneut öffnen.", 23f, Text);
                SetTopStretch(body.rectTransform, 32f, 76f, 32f, 106f);
                body.lineSpacing = 4f;

                TMP_Text continueLabel;
                Button continueButton = CreateButton("ContinueButton", panel, scene, font, "Weiter", out continueLabel);
                Place(continueButton.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                    new Vector2(-28f, 24f), new Vector2(184f, 50f));

                RectTransform choices = CreateRect("ChoicesContainer", panel, scene);
                choices.anchorMin = Vector2.zero;
                choices.anchorMax = new Vector2(1f, 0f);
                choices.pivot = Vector2.zero;
                choices.offsetMin = new Vector2(32f, 24f);
                choices.offsetMax = new Vector2(-32f, 24f);
                VerticalLayoutGroup layout = choices.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.LowerLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                ContentSizeFitter fitter = choices.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                TMP_Text choiceLabel;
                Button choiceTemplate = CreateButton("ChoiceButtonTemplate", choices, scene, font, "Antwort", out choiceLabel);
                choiceTemplate.GetComponent<Image>().color = new Color(0.09f, 0.20f, 0.26f);
                LayoutElement choiceSize = choiceTemplate.gameObject.AddComponent<LayoutElement>();
                choiceSize.minHeight = 52f;
                choiceSize.preferredHeight = 52f;
                choiceLabel.alignment = TextAlignmentOptions.MidlineLeft;
                choiceTemplate.gameObject.SetActive(false);

                DialogueTrigger trigger = manager.GetComponent<DialogueTrigger>();
                if (trigger == null)
                    trigger = Undo.AddComponent<DialogueTrigger>(manager.gameObject);
                Undo.RecordObject(manager, "Dialog-Oberfläche verbinden");
                managerSettings.Update();
                SetReference(managerSettings, "dialoguePanel", panel.gameObject);
                SetReference(managerSettings, "speakerText", speaker);
                SetReference(managerSettings, "bodyText", body);
                SetReference(managerSettings, "portraitImage", null);
                SetReference(managerSettings, "continueButton", continueButton);
                SetReference(managerSettings, "continueButtonText", continueLabel);
                SetReference(managerSettings, "choicesContainer", choices);
                SetReference(managerSettings, "choiceButtonPrefab", choiceTemplate);
                managerSettings.FindProperty("charactersPerSecond").floatValue = 42f;
                managerSettings.ApplyModifiedProperties();

                Undo.RecordObject(trigger, "Dialog zuweisen");
                SerializedObject triggerSettings = new SerializedObject(trigger);
                SetReference(triggerSettings, "manager", manager);
                SetReference(triggerSettings, "dialogue", dialogue);
                triggerSettings.FindProperty("playOnStart").boolValue = true;
                triggerSettings.ApplyModifiedProperties();

                TMP_Text startLabel;
                Button startButton = CreateButton("StartDialogueButton", canvas.transform, scene, font,
                    "Dialog starten", out startLabel);
                Place(startButton.GetComponent<RectTransform>(), Vector2.one,
                    new Vector2(-36f, -30f), new Vector2(188f, 46f));
                UnityEventTools.AddPersistentListener(startButton.onClick, manager.EndDialogue);
                UnityEventTools.AddPersistentListener(startButton.onClick, trigger.StartDialogue);

                ValidateWiring(managerSettings, manager, dialogue, panel, choices, choiceTemplate);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(trigger);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                setupComplete = true;
                if (!EditorSceneManager.SaveScene(scene))
                {
                    Debug.LogError("Dialog wurde eingebaut, aber Start konnte nicht gespeichert werden. Bitte die Szene speichern.");
                    return;
                }
                Selection.activeGameObject = manager.gameObject;
                Debug.Log("SIMPLE_DIALOGUE_INSTALLED | Scene=" + scene.path + " | Manager=" + manager.name +
                    " | Canvas=" + canvasObject.name + " | Dialogue=" + dialoguePath +
                    " | UI=valid | Nodes=3 | EventSystem=reused | PlayOnStart=true", manager);
            }
            catch (Exception exception)
            {
                if (!setupComplete)
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    if (!string.IsNullOrEmpty(dialoguePath))
                        AssetDatabase.DeleteAsset(dialoguePath);
                }
                Debug.LogException(exception);
            }
        }

        private static DialogueManager FindExistingManager(Scene scene)
        {
            DialogueManager match = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (DialogueManager candidate in root.GetComponentsInChildren<DialogueManager>(true))
            {
                if (candidate.gameObject.name != "DialogMenager") continue;
                if (match != null) return null;
                match = candidate;
            }
            return match;
        }

        private static DialogueAsset CreateDialogue(string path)
        {
            DialogueAsset dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.startNodeId = "start";
            dialogue.nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    id = "start", speakerName = "Wache",
                    text = "Willkommen! Was möchtest du wissen?",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Hast du eine Aufgabe für mich?", nextNodeId = "info" },
                        new DialogueChoice { text = "Ich schaue mich erst einmal um.", nextNodeId = "ende" }
                    }
                },
                new DialogueNode
                {
                    id = "info", speakerName = "Wache",
                    text = "Sprich mit der Händlerin am Marktplatz. Sie braucht Hilfe bei einer vermissten Lieferung.",
                    nextNodeId = "ende", choices = new List<DialogueChoice>()
                },
                new DialogueNode
                {
                    id = "ende", speakerName = "Wache", text = "Viel Erfolg auf deiner Reise!",
                    nextNodeId = string.Empty, choices = new List<DialogueChoice>()
                }
            };
            AssetDatabase.CreateAsset(dialogue, path);
            return dialogue;
        }

        private static GameObject CreateObject(string name, Transform parent, Scene scene, params Type[] components)
        {
            GameObject result = new GameObject(name, components);
            if (parent == null) SceneManager.MoveGameObjectToScene(result, scene);
            else result.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(result, "Dialog-Oberfläche erstellen");
            return result;
        }

        private static RectTransform CreateRect(string name, Transform parent, Scene scene)
        {
            return CreateObject(name, parent, scene, typeof(RectTransform)).GetComponent<RectTransform>();
        }

        private static RectTransform CreateImage(string name, Transform parent, Scene scene, Color color)
        {
            RectTransform rect = CreateRect(name, parent, scene);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(string name, Transform parent, Scene scene, TMP_FontAsset font,
            string text, float size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, scene);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, Scene scene, TMP_FontAsset font,
            string text, out TMP_Text label)
        {
            RectTransform rect = CreateImage(name, parent, scene, new Color(0.06f, 0.35f, 0.30f));
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.82f, 1f, 0.95f);
            colors.selectedColor = new Color(0.82f, 1f, 0.95f);
            colors.pressedColor = new Color(0.63f, 0.82f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            label = CreateText("Label", rect, scene, font, text, 20f, Text);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(18f, 8f);
            label.rectTransform.offsetMax = new Vector2(-18f, -8f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 20f;
            return button;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopStretch(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetReference(SerializedObject settings, string field, Object value)
        {
            SerializedProperty property = settings.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Benötigtes Dialogfeld fehlt: " + field);
            property.objectReferenceValue = value;
        }

        private static void ValidateWiring(SerializedObject settings, DialogueManager manager,
            DialogueAsset dialogue, RectTransform panel, RectTransform choices, Button template)
        {
            settings.Update();
            foreach (string field in new[] { "dialoguePanel", "speakerText", "bodyText", "continueButton",
                "continueButtonText", "choicesContainer", "choiceButtonPrefab" })
            {
                SerializedProperty property = settings.FindProperty(field);
                if (property == null || property.objectReferenceValue == null)
                    throw new InvalidOperationException("Dialog-Verbindung fehlt: " + field);
            }
            if (manager.transform.IsChildOf(panel))
                throw new InvalidOperationException("Der DialogueManager darf nicht im Dialogfenster liegen.");
            if (!choices.IsChildOf(panel) || !template.transform.IsChildOf(choices) || template.gameObject.activeSelf)
                throw new InvalidOperationException("Die Antwortvorlage ist nicht korrekt eingerichtet.");
            Dictionary<string, DialogueNode> lookup;
            string error;
            if (!dialogue.TryBuildLookup(out lookup, out error)) throw new InvalidOperationException(error);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(current + "/" + parts[i]))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current += "/" + parts[i];
            }
        }
    }
}
#endif
