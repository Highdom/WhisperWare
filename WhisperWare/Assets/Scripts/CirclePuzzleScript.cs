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
        collider2ds = transform.parent.GetComponentsInChildren<Collider2D>();
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
                Debug.Log(col1.name);
                Debug.Log(col2.name);
                if (col1 == col2)
                    continue;

                if (col1.bounds.Intersects(col2.bounds))
                {
                    overlapFound = true;
                    Debug.Log("Overlap gefunden");
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

            Collider2D[] hits =
                Physics2D.OverlapPointAll(mousePos);

            if (hits.Length >= 2)
            {
                Debug.Log("Überlappungsbereich angeklickt!");
            }
        }
    }
}
