using UnityEngine;

namespace SimpleDialogue
{
    [DisallowMultipleComponent]
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueManager manager;
        [SerializeField] private DialogueAsset dialogue;
        [Tooltip("Zum Testen: Dialog automatisch beim Szenenstart beginnen.")]
        [SerializeField] private bool playOnStart;

        private void Start()
        {
            if (playOnStart)
                StartDialogue();
        }

        // An einen UI-Button oder die vorhandene NPC-Interaktion anschliessen.
        // Dadurch ist kein bestimmtes Input-System vorgeschrieben.
        public void StartDialogue()
        {
            if (manager == null)
            {
                Debug.LogError("DialogueTrigger: Manager im Inspector zuweisen.", this);
                return;
            }

            manager.StartDialogue(dialogue);
        }
    }
}
