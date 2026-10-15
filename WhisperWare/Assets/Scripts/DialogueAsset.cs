using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDialogue
{
    [CreateAssetMenu(fileName = "NeuerDialog", menuName = "Simple Dialogue/Dialog")]
    public sealed class DialogueAsset : ScriptableObject
    {
        [Tooltip("ID des ersten Knotens. Gross-/Kleinschreibung beachten.")]
        public string startNodeId = "start";

        public List<DialogueNode> nodes = new List<DialogueNode>
        {
            new DialogueNode
            {
                id = "start",
                speakerName = "NPC",
                text = "Hallo! Hier beginnt dein Dialog."
            }
        };

        // Erst pruefen, dann starten: fehlerhafte Verzweigungen werden vorab erkannt.
        public bool TryBuildLookup(out Dictionary<string, DialogueNode> lookup, out string error)
        {
            lookup = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
            error = null;

            if (nodes == null || nodes.Count == 0)
            {
                error = "Der Dialog hat keine Knoten.";
                return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                DialogueNode node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    error = "Knoten " + i + " fehlt oder hat keine ID.";
                    return false;
                }

                if (node.id != node.id.Trim())
                {
                    error = "Die ID '" + node.id + "' hat Leerzeichen am Anfang oder Ende.";
                    return false;
                }

                if (lookup.ContainsKey(node.id))
                {
                    error = "Die Knoten-ID '" + node.id + "' ist doppelt vorhanden.";
                    return false;
                }

                lookup.Add(node.id, node);
            }

            if (string.IsNullOrWhiteSpace(startNodeId) || !lookup.ContainsKey(startNodeId))
            {
                error = "Der Startknoten '" + startNodeId + "' existiert nicht.";
                return false;
            }

            foreach (DialogueNode node in nodes)
            {
                if (node.choices != null && node.choices.Count > 0)
                {
                    for (int i = 0; i < node.choices.Count; i++)
                    {
                        DialogueChoice choice = node.choices[i];
                        if (choice == null || string.IsNullOrWhiteSpace(choice.text))
                        {
                            error = "Antwort " + i + " in '" + node.id + "' hat keinen Text.";
                            return false;
                        }

                        if (!HasValidTarget(choice.nextNodeId, lookup))
                        {
                            error = "Antwort " + i + " in '" + node.id + "' verweist auf den fehlenden Knoten '" + choice.nextNodeId + "'.";
                            return false;
                        }
                    }
                }
                else if (!HasValidTarget(node.nextNodeId, lookup))
                {
                    error = "Knoten '" + node.id + "' verweist auf den fehlenden Knoten '" + node.nextNodeId + "'.";
                    return false;
                }
            }

            return true;
        }

        private static bool HasValidTarget(string id, Dictionary<string, DialogueNode> lookup)
        {
            // Ein leeres Ziel beendet das Gespraech.
            return string.IsNullOrWhiteSpace(id) || lookup.ContainsKey(id);
        }

        [ContextMenu("Dialog pruefen")]
        private void ValidateDialogue()
        {
            Dictionary<string, DialogueNode> lookup;
            string error;
            if (TryBuildLookup(out lookup, out error))
                Debug.Log("Dialog ist gueltig: " + lookup.Count + " Knoten.", this);
            else
                Debug.LogError(error, this);
        }
    }

    [Serializable]
    public sealed class DialogueNode
    {
        [Tooltip("Eindeutige ID, z. B. start, auftrag oder ende.")]
        public string id = "node";

        public string speakerName = "NPC";
        public Sprite portrait;

        [TextArea(3, 10)]
        public string text;

        [Tooltip("Ohne Antworten: naechster Knoten. Leer = Gespraech beenden.")]
        public string nextNodeId;

        [Tooltip("Mit Antworten wird Next Node Id des Knotens ignoriert.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        [TextArea(1, 3)]
        public string text;

        [Tooltip("Ziel dieser Antwort. Leer = Gespraech beenden.")]
        public string nextNodeId;
    }
}
