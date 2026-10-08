using UnityEngine;
using UnityEngine.InputSystem;

public class DraggableSprite : MonoBehaviour
{
    private bool isDragging;
    private Vector3 offset;

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
            Mouse.current.position.ReadValue());

        mouseWorldPos.z = 0;

        // Start dragging
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

            if (hit != null && hit.gameObject == gameObject)
            {
                isDragging = true;
                offset = transform.position - mouseWorldPos;
            }
        }

        // Drag
        if (isDragging)
        {
            transform.position = mouseWorldPos + offset;
        }

        // Stop dragging
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}