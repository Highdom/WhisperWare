using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public int rotationSpeed = 5;
    private Collider2D[] collider2ds;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collider2ds = GetComponentsInChildren<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        bool overlapFound = false;

        foreach (Collider2D col1 in collider2ds)
        {
            foreach (Collider2D col2 in collider2ds)
            {
                if (col1 == col2)
                    continue;

                if (col1.bounds.Intersects(col2.bounds))
                {
                    overlapFound = true;
                    break;
                }
            }

            if (overlapFound)
                break;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && overlapFound)
        {
            Vector2 mousePos =
                Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D hit =
                Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("Clicked: " + hit.collider.name);
            }
        }
    }
}
