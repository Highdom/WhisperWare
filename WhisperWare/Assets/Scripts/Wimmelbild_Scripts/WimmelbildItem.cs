using UnityEngine;

namespace WhisperWare.Wimmelbild
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class WimmelbildItem : MonoBehaviour
    {
        [SerializeField] private string displayName;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public SpriteRenderer Renderer => GetComponent<SpriteRenderer>();
        public Collider2D HitArea => GetComponent<Collider2D>();
        public bool IsPlaced { get; private set; }

        private Vector3 startPosition;
        private Vector3 startScale;
        private Quaternion startRotation;
        private int startSortingOrder;
        private bool captured;

        public void CaptureStart()
        {
            if (captured) return;
            startPosition = transform.position;
            startScale = transform.localScale;
            startRotation = transform.rotation;
            startSortingOrder = Renderer.sortingOrder;
            captured = true;
        }

        public void BeginDrag()
        {
            CaptureStart();
            Renderer.sortingOrder = startSortingOrder + 1000;
        }

        public void ReturnToStart()
        {
            CaptureStart();
            transform.SetPositionAndRotation(startPosition, startRotation);
            transform.localScale = startScale;
            Renderer.sortingOrder = startSortingOrder;
            IsPlaced = false;
        }

        public void Place(WimmelbildSlot slot)
        {
            CaptureStart();
            transform.rotation = Quaternion.identity;
            transform.localScale = startScale;
            Vector3 size = Renderer.bounds.size;
            float largestSide = Mathf.Max(size.x, size.y);
            if (largestSide > slot.ItemSize)
                transform.localScale *= slot.ItemSize / largestSide;
            // Auch Sprites mit einem seitlich gesetzten Pivot landen mittig im Ziel.
            transform.position += slot.transform.position - Renderer.bounds.center;
            Renderer.sortingOrder = startSortingOrder;
            IsPlaced = true;
        }
    }
}
