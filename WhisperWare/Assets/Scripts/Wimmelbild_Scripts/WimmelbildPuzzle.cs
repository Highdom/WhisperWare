using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WhisperWare.Wimmelbild
{
    [DisallowMultipleComponent]
    public sealed class WimmelbildPuzzle : MonoBehaviour
    {
        [Header("Loesung: Anzahl und Reihenfolge hier festlegen")]
        [Tooltip("Ein Eintrag pro gesuchtem Objekt. Die Listengroesse ist die Anzahl. Von oben nach unten: Ziel 1, Ziel 2, ... Jedes Objekt nur einmal eintragen.")]
        [SerializeField] private List<WimmelbildItem> solutionOrder = new List<WimmelbildItem>();
        [Tooltip("Aktiv: Erst Objekt 1, dann 2 usw. ablegen. Inaktiv: Die richtigen Zielplaetze duerfen in beliebiger zeitlicher Reihenfolge belegt werden.")]
        [SerializeField] private bool enforceSequence = true;
        [Tooltip("Namen der gesuchten Objekte an den Zielplaetzen anzeigen.")]
        [SerializeField] private bool showNames = true;

        [Header("Spielfeld")]
        [SerializeField] private Camera gameCamera;
        [Tooltip("Enthaelt alle verschiebbaren Objekte, auch Ablenkungsobjekte, die nicht zur Loesung gehoeren.")]
        [SerializeField] private Transform itemsRoot;
        [Tooltip("Mitte der ersten Zielreihe. Die Zielplaetze werden automatisch aus der Loesungsliste erzeugt.")]
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private WimmelbildSlot slotTemplate;
        [SerializeField, Min(1)] private int columns = 6;
        [SerializeField] private Vector2 slotSpacing = new Vector2(2.2f, 2f);

        [Header("Rueckmeldung")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private UnityEvent onCorrectPlacement = new UnityEvent();
        [SerializeField] private UnityEvent onWrongPlacement = new UnityEvent();
        [SerializeField] private UnityEvent onPuzzleCompleted = new UnityEvent();

        [SerializeField, HideInInspector] private List<WimmelbildSlot> slots = new List<WimmelbildSlot>();
        private readonly List<WimmelbildItem> roundOrder = new List<WimmelbildItem>();
        private WimmelbildItem[] items = new WimmelbildItem[0];
        private WimmelbildItem draggedItem;
        private Vector3 dragOffset;
        private bool touchDrag;
        private bool ready;
        private bool roundEnforcesSequence;
        private InputAction mousePress;
        private InputAction touchPress;
        private bool pressPending;
        private bool pendingTouch;
        private Vector2 pressPosition;

        public int RequiredCount => roundOrder.Count;
        public int PlacedCount { get; private set; }
        public bool IsCompleted { get; private set; }
        public IReadOnlyList<WimmelbildSlot> Slots => slots;
        public IReadOnlyList<WimmelbildItem> SolutionOrder => solutionOrder;

        private void Start() => ResetPuzzle();

        private void OnEnable()
        {
            mousePress = new InputAction("Wimmelbild Maus", InputActionType.Button, "<Mouse>/leftButton");
            touchPress = new InputAction("Wimmelbild Touch", InputActionType.Button, "<Touchscreen>/primaryTouch/press");
            mousePress.performed += CaptureMousePress;
            touchPress.performed += CaptureTouchPress;
            mousePress.Enable();
            touchPress.Enable();
        }

        private void CaptureMousePress(InputAction.CallbackContext context)
        {
            if (!ready || draggedItem != null || (pressPending && pendingTouch)) return;
            // Die Position beim Druecken merken, bevor weitere Bewegungsereignisse
            // desselben Frames die aktuelle Mausposition veraendern.
            pressPosition = ((Mouse)context.control.device).position.ReadValue();
            pressPending = true;
            pendingTouch = false;
        }

        private void CaptureTouchPress(InputAction.CallbackContext context)
        {
            if (!ready || draggedItem != null) return;
            pressPosition = ((Touchscreen)context.control.device).primaryTouch.startPosition.ReadValue();
            pressPending = true;
            pendingTouch = true;
        }

        public bool ValidateConfiguration(out string error)
        {
            error = string.Empty;
            if (solutionOrder == null || solutionOrder.Count == 0)
                error = "Trage mindestens ein Objekt in Solution Order ein.";
            else if (itemsRoot == null || slotsRoot == null || slotTemplate == null || gameCamera == null)
                error = "Kamera, Items Root, Slots Root und Slot Template muessen zugewiesen sein.";
            else if (itemsRoot == slotsRoot || itemsRoot.IsChildOf(slotsRoot) || slotsRoot.IsChildOf(itemsRoot))
                error = "Objekte und Zielplaetze benoetigen getrennte Container.";
            else if (slotTemplate.transform.IsChildOf(slotsRoot))
                error = "Die Zielvorlage muss ausserhalb von Slots Root liegen.";
            else if (columns < 1 || slotSpacing.x <= 0f || slotSpacing.y <= 0f)
                error = "Spaltenzahl und Zielabstaende muessen groesser als null sein.";

            if (error.Length > 0) return false;
            var seen = new HashSet<WimmelbildItem>();
            for (int i = 0; i < solutionOrder.Count; i++)
            {
                WimmelbildItem item = solutionOrder[i];
                string prefix = "Eintrag " + (i + 1) + ": ";
                if (item == null) error = prefix + "Objekt fehlt.";
                else if (!seen.Add(item)) error = prefix + "Objekt ist doppelt eingetragen.";
                else if (!item.transform.IsChildOf(itemsRoot)) error = prefix + "Objekt muss unter Items Root liegen.";
                else if (!item.isActiveAndEnabled || !item.Renderer.enabled || item.Renderer.sprite == null || !item.HitArea.enabled)
                    error = prefix + "Objekt, SpriteRenderer und Collider2D muessen aktiv sein und einen Sprite besitzen.";
                if (error.Length > 0) return false;
            }
            return true;
        }

        [ContextMenu("Raetsel neu starten")]
        public void ResetPuzzle()
        {
            if (!Application.isPlaying) return;
            ready = false;
            CancelDrag();
            foreach (WimmelbildItem item in items)
                if (item != null) item.ReturnToStart();
            PlacedCount = 0;
            IsCompleted = false;
            roundOrder.Clear();

            if (!ValidateConfiguration(out string error))
            {
                ClearSlots();
                SetFeedback("Konfiguration pruefen: " + error);
                if (progressText != null) progressText.text = "Nicht eingerichtet";
                Debug.LogWarning("Wimmelbild: " + error, this);
                return;
            }

            roundOrder.AddRange(solutionOrder);
            roundEnforcesSequence = enforceSequence;
            items = itemsRoot.GetComponentsInChildren<WimmelbildItem>(true);
            foreach (WimmelbildItem item in items) item.ReturnToStart();
            RebuildSlots();
            ready = true;
            Physics2D.SyncTransforms();
            UpdateProgress();
            SetFeedback("Finde die Objekte und ziehe sie auf die nummerierten Zielplaetze.");
        }

        // Kann auch vom Editor fuer eine Vorschau aufgerufen werden.
        public void RebuildSlots()
        {
            ClearSlots();
            if (!ValidateConfiguration(out _)) return;
            slotTemplate.gameObject.SetActive(false);
            for (int i = 0; i < solutionOrder.Count; i++)
            {
                WimmelbildSlot slot = Instantiate(slotTemplate, slotsRoot);
                int row = i / columns;
                int rowLength = Mathf.Min(columns, solutionOrder.Count - row * columns);
                slot.transform.localPosition = new Vector3((i % columns - (rowLength - 1) * 0.5f) * slotSpacing.x, -row * slotSpacing.y, 0f);
                slot.Configure(i, solutionOrder[i], showNames);
                slot.gameObject.SetActive(true);
                slots.Add(slot);
            }
            if (slots.Count > 0) slots[0].SetState(false, enforceSequence);
        }

        private void ClearSlots()
        {
            foreach (WimmelbildSlot slot in slots)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(slot.gameObject);
                else DestroyImmediate(slot.gameObject);
            }
            slots.Clear();
        }

        private void Update()
        {
            if (!ready || IsCompleted) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelDrag();
                return;
            }

            if (draggedItem == null && pressPending) StartDrag(pressPosition, pendingTouch);
            pressPending = false;
            if (draggedItem == null) return;

            Vector2 screenPosition;
            bool released;
            if (touchDrag)
            {
                if (Touchscreen.current == null) { CancelDrag(); return; }
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                released = !Touchscreen.current.primaryTouch.press.isPressed;
            }
            else
            {
                if (Mouse.current == null) { CancelDrag(); return; }
                screenPosition = Mouse.current.position.ReadValue();
                released = !Mouse.current.leftButton.isPressed;
            }

            if (!TryWorldPosition(screenPosition, draggedItem.transform.position.z, out Vector3 point))
            {
                CancelDrag();
                return;
            }
            draggedItem.transform.position = point + dragOffset;
            if (!released) return;

            WimmelbildItem releasedItem = draggedItem;
            draggedItem = null;
            WimmelbildSlot target = null;
            foreach (WimmelbildSlot slot in slots)
                if (slot != null && slot.HitArea.OverlapPoint(point)) { target = slot; break; }
            TryPlace(releasedItem, target);
        }

        private void StartDrag(Vector2 screenPosition, bool touch)
        {
            // UI-Buttons duerfen kein Objekt hinter sich greifen.
            if (EventSystem.current != null)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, hits);
                if (hits.Count > 0) return;
            }
            Physics2D.SyncTransforms();
            WimmelbildItem picked = null;
            Vector3 pickedPoint = default;
            foreach (WimmelbildItem item in items)
            {
                if (item == null || !item.isActiveAndEnabled || item.IsPlaced || !item.Renderer.enabled || !item.HitArea.enabled) continue;
                if (!TryWorldPosition(screenPosition, item.transform.position.z, out Vector3 point) || !item.HitArea.OverlapPoint(point)) continue;
                if (picked != null && !IsInFront(item.Renderer, picked.Renderer)) continue;
                picked = item;
                pickedPoint = point;
            }
            if (picked == null) return;
            draggedItem = picked;
            dragOffset = picked.transform.position - pickedPoint;
            touchDrag = touch;
            picked.BeginDrag();
        }

        private static bool IsInFront(SpriteRenderer candidate, SpriteRenderer other)
        {
            int layer = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            int otherLayer = SortingLayer.GetLayerValueFromID(other.sortingLayerID);
            if (layer != otherLayer) return layer > otherLayer;
            if (candidate.sortingOrder != other.sortingOrder) return candidate.sortingOrder > other.sortingOrder;
            return candidate.transform.position.z <= other.transform.position.z;
        }

        private bool TryWorldPosition(Vector2 screenPosition, float z, out Vector3 point)
        {
            point = default;
            if (gameCamera == null) return false;
            Ray ray = gameCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        public bool TryPlace(WimmelbildItem item, WimmelbildSlot target)
        {
            if (!ready || !isActiveAndEnabled || IsCompleted || item == null || item.IsPlaced || System.Array.IndexOf(items, item) < 0) return false;
            int index = target == null ? -1 : slots.IndexOf(target);
            string error = null;
            if (index < 0) error = "Ziehe das Objekt auf einen nummerierten Zielplatz.";
            else if (roundOrder[index] != item) error = "Dieses Objekt gehoert nicht auf Ziel " + (index + 1) + ".";
            else if (roundEnforcesSequence && index != PlacedCount) error = "Zuerst Ziel " + (PlacedCount + 1) + " belegen.";

            if (error != null)
            {
                item.ReturnToStart();
                SetFeedback(error);
                onWrongPlacement.Invoke();
                return false;
            }

            item.Place(target);
            PlacedCount++;
            IsCompleted = PlacedCount == roundOrder.Count;
            UpdateProgress();
            SetFeedback(IsCompleted ? "Geschafft! Alle Objekte sind richtig eingeordnet." : "Richtig! " + item.DisplayName + " ist an seinem Platz.");
            // Zustand und Anzeige zuerst aktualisieren, dann externe Spielereignisse ausloesen.
            onCorrectPlacement.Invoke();
            if (IsCompleted) onPuzzleCompleted.Invoke();
            return true;
        }

        private void UpdateProgress()
        {
            if (progressText != null) progressText.text = PlacedCount + " / " + roundOrder.Count + " gefunden";
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetState(roundOrder[i].IsPlaced, roundEnforcesSequence && i == PlacedCount && !IsCompleted);
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
        }

        private void CancelDrag()
        {
            if (draggedItem != null) draggedItem.ReturnToStart();
            draggedItem = null;
            pressPending = false;
        }

        private void OnDisable()
        {
            CancelDrag();
            mousePress?.Dispose();
            touchPress?.Dispose();
            mousePress = null;
            touchPress = null;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
    }
}
