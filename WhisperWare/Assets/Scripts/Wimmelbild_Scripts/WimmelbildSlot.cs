using TMPro;
using UnityEngine;

namespace WhisperWare.Wimmelbild
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WimmelbildSlot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.1f)] private float itemSize = 0.9f;

        public int Index { get; private set; }
        public float ItemSize => itemSize;
        public Collider2D HitArea => GetComponent<Collider2D>();

        public void Configure(int index, WimmelbildItem item, bool showName)
        {
            Index = index;
            name = "Ziel " + (index + 1);
            if (label != null)
                label.text = (index + 1) + (showName ? "  " + item.DisplayName : string.Empty);
            SetState(false, false);
        }

        public void SetState(bool occupied, bool next)
        {
            if (background != null)
                background.color = occupied ? new Color(0.16f, 0.46f, 0.38f)
                    : next ? new Color(0.32f, 0.41f, 0.53f) : new Color(0.15f, 0.22f, 0.31f);
        }
    }
}
