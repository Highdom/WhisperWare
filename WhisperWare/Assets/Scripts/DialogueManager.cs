using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleDialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueManager : MonoBehaviour
    {
        [Header("Canvas-Oberflaeche")]
        [Tooltip("Nur dieses Fenster wird ausgeblendet. Manager ausserhalb platzieren!")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;
        [SerializeField] private Transform choicesContainer;
        [Tooltip("Button-Prefab oder inaktive Vorlage mit einem TMP-Text als Kind. On Click leer lassen.")]
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Textanzeige")]
        [Tooltip("0 zeigt den gesamten Text sofort an.")]
        [Min(0f)]
        [SerializeField] private float charactersPerSecond = 40f;

        [Header("Ereignisse")]
        [SerializeField] private UnityEvent onDialogueStarted = new UnityEvent();
        [SerializeField] private UnityEvent onDialogueEnded = new UnityEvent();

        public bool IsDialogueActive { get; private set; }
        public bool IsTyping { get { return isTyping; } }
        public DialogueAsset CurrentDialogue { get; private set; }
        public string CurrentNodeId { get { return currentNode == null ? null : currentNode.id; } }
        public UnityEvent OnDialogueStarted { get { return onDialogueStarted; } }
        public UnityEvent OnDialogueEnded { get { return onDialogueEnded; } }

        private readonly List<Button> spawnedChoices = new List<Button>();
        private Dictionary<string, DialogueNode> nodeLookup;
        private DialogueNode currentNode;
        private bool isTyping;
        private int characterCount;
        private float revealedCharacters;
        private int nodeVersion;

        private void Awake()
        {
            // Den Manager niemals zusammen mit seinem Dialogfenster deaktivieren.
            if (dialoguePanel != null && !transform.IsChildOf(dialoguePanel.transform))
                dialoguePanel.SetActive(false);

            // Eine Vorlage in der Szene bleibt unsichtbar; nur Kopien werden gezeigt.
            if (choiceButtonPrefab != null && choicesContainer != null &&
                choiceButtonPrefab.transform.IsChildOf(choicesContainer))
                choiceButtonPrefab.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(ContinueDialogue);
        }

        private void OnDisable()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(ContinueDialogue);

            EndDialogue();
        }

        private void Update()
        {
            if (!IsDialogueActive || !isTyping)
                return;

            if (charactersPerSecond <= 0f)
            {
                CompleteTyping();
                return;
            }

            // Laeuft auch dann weiter, wenn das Spiel mit timeScale = 0 pausiert ist.
            revealedCharacters += Time.unscaledDeltaTime * charactersPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealedCharacters));

            if (revealedCharacters >= characterCount)
                CompleteTyping();
        }

        /// <summary>Startet einen gueltigen Dialog. Ein laufender Dialog wird nicht ersetzt.</summary>
        public bool StartDialogue(DialogueAsset dialogue)
        {
            if (IsDialogueActive)
                return false;

            if (!isActiveAndEnabled)
            {
                Debug.LogWarning("Der DialogueManager muss aktiv sein.", this);
                return false;
            }

            string error;
            if (!ValidateUI(out error))
            {
                Debug.LogError(error, this);
                return false;
            }

            if (dialogue == null)
            {
                Debug.LogError("Kein DialogueAsset zugewiesen.", this);
                return false;
            }

            Dictionary<string, DialogueNode> lookup;
            if (!dialogue.TryBuildLookup(out lookup, out error))
            {
                Debug.LogError("Dialog '" + dialogue.name + "': " + error, dialogue);
                return false;
            }

            dialoguePanel.SetActive(true);
            if (!dialoguePanel.activeInHierarchy)
            {
                dialoguePanel.SetActive(false);
                Debug.LogError("Ein uebergeordnetes Objekt des Dialogfensters ist deaktiviert.", this);
                return false;
            }

            nodeLookup = lookup;
            CurrentDialogue = dialogue;
            IsDialogueActive = true;
            ShowNode(dialogue.startNodeId);
            onDialogueStarted.Invoke();
            return true;
        }

        /// <summary>Erst Text vervollstaendigen, dann weiter. Antworten werden per Button gewaehlt.</summary>
        public void ContinueDialogue()
        {
            if (!IsDialogueActive)
                return;

            if (isTyping)
            {
                CompleteTyping();
                return;
            }

            if (HasChoices())
                return;

            GoToNodeOrEnd(currentNode.nextNodeId);
        }

        /// <summary>Kann z. B. an einen Schliessen-Button angeschlossen werden.</summary>
        public void EndDialogue()
        {
            bool wasActive = IsDialogueActive;
            IsDialogueActive = false;
            isTyping = false;
            currentNode = null;
            CurrentDialogue = null;
            nodeLookup = null;
            nodeVersion++;
            ClearChoices();

            if (dialoguePanel != null && !transform.IsChildOf(dialoguePanel.transform))
            {
                EventSystem events = EventSystem.current;
                if (events != null && events.currentSelectedGameObject != null &&
                    events.currentSelectedGameObject.transform.IsChildOf(dialoguePanel.transform))
                    events.SetSelectedGameObject(null);

                dialoguePanel.SetActive(false);
            }

            // Zustand zuerst aufraeumen: Ein Ereignis darf direkt den naechsten Dialog starten.
            if (wasActive)
                onDialogueEnded.Invoke();
        }

        private void ShowNode(string nodeId)
        {
            DialogueNode node;
            if (nodeLookup == null || !nodeLookup.TryGetValue(nodeId, out node))
            {
                Debug.LogError("Dialogknoten fehlt: " + nodeId, this);
                EndDialogue();
                return;
            }

            nodeVersion++;
            ClearChoices();
            currentNode = node;
            speakerText.text = node.speakerName ?? string.Empty;

            if (portraitImage != null)
            {
                portraitImage.sprite = node.portrait;
                portraitImage.gameObject.SetActive(node.portrait != null);
            }

            // Der volle String bleibt erhalten: Rich-Text-Tags werden nicht zerschnitten.
            bodyText.text = node.text ?? string.Empty;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate(true, true);
            characterCount = bodyText.textInfo.characterCount;
            revealedCharacters = 0f;
            isTyping = true;
            SetContinueButton(true, "Anzeigen");

            if (charactersPerSecond <= 0f || characterCount == 0)
                CompleteTyping();
            else
                SelectButton(continueButton);
        }

        private void CompleteTyping()
        {
            isTyping = false;
            bodyText.maxVisibleCharacters = int.MaxValue;

            if (HasChoices())
            {
                SetContinueButton(false, string.Empty);
                BuildChoices();
            }
            else
            {
                SetContinueButton(true, string.IsNullOrWhiteSpace(currentNode.nextNodeId) ? "Beenden" : "Weiter");
                SelectButton(continueButton);
            }
        }

        private bool HasChoices()
        {
            return currentNode != null && currentNode.choices != null && currentNode.choices.Count > 0;
        }

        private void BuildChoices()
        {
            ClearChoices();
            int version = nodeVersion;

            foreach (DialogueChoice choice in currentNode.choices)
            {
                // Jede Schaltflaeche erhaelt ihr eigenes Ziel und die aktuelle Knotenversion.
                string target = choice.nextNodeId;
                Button button = Instantiate(choiceButtonPrefab, choicesContainer);
                button.GetComponentInChildren<TMP_Text>(true).text = choice.text;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    if (IsDialogueActive && !isTyping && version == nodeVersion)
                        GoToNodeOrEnd(target);
                });
                button.interactable = true;
                button.gameObject.SetActive(true);
                spawnedChoices.Add(button);
            }

            if (spawnedChoices.Count > 0)
                SelectButton(spawnedChoices[0]);
        }

        private void GoToNodeOrEnd(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                EndDialogue();
            else
                ShowNode(nodeId);
        }

        private void ClearChoices()
        {
            // Nur eigene Kopien entfernen; andere Kinder und die Vorlage bleiben erhalten.
            foreach (Button button in spawnedChoices)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            spawnedChoices.Clear();
        }

        private void SetContinueButton(bool visible, string label)
        {
            continueButton.gameObject.SetActive(visible);
            if (continueButtonText != null)
                continueButtonText.text = label;
        }

        private static void SelectButton(Button button)
        {
            if (EventSystem.current != null && button != null && button.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private bool ValidateUI(out string error)
        {
            error = null;
            if (dialoguePanel == null || speakerText == null || bodyText == null ||
                continueButton == null || choicesContainer == null || choiceButtonPrefab == null)
            {
                error = "DialogueManager: UI-Felder zuweisen. Nur Portrait Image und Continue Button Text sind optional.";
                return false;
            }

            if (transform.IsChildOf(dialoguePanel.transform))
            {
                error = "DialogueManager ausserhalb des Dialogue Panel platzieren, z. B. auf dem Canvas.";
                return false;
            }

            if (!speakerText.transform.IsChildOf(dialoguePanel.transform) ||
                !bodyText.transform.IsChildOf(dialoguePanel.transform) ||
                !continueButton.transform.IsChildOf(dialoguePanel.transform) ||
                !choicesContainer.IsChildOf(dialoguePanel.transform))
            {
                error = "Sprechertext, Dialogtext, Weiter-Button und Choices Container muessen im Dialogue Panel liegen.";
                return false;
            }

            if (choicesContainer.IsChildOf(choiceButtonPrefab.transform))
            {
                error = "Choices Container darf nicht auf oder unter der Antwortvorlage liegen.";
                return false;
            }

            if (choiceButtonPrefab.GetComponentInChildren<TMP_Text>(true) == null)
            {
                error = "Das Antwort-Button-Prefab braucht einen TMP-Text als Kind.";
                return false;
            }

            return true;
        }
    }
}
