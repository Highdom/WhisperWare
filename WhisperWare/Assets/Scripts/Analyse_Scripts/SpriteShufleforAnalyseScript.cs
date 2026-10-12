using UnityEngine;
using UnityEngine.InputSystem;

public class SpriteShufleforAnalyseScript : MonoBehaviour
{
    [Header("Lösung")]
    public SpriteRenderer[] sprite1;
    public SpriteRenderer[] sprite2;

    [Header("Symbole")]
    public SpriteRenderer[] sprite3;
    public SpriteRenderer[] sprite4;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //sr = GetComponentsInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("Clicked: " + hit.collider.name);
            }
        }
    }
}
