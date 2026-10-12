using UnityEngine;
using UnityEngine.InputSystem;

public class AnalysePuzzleManager : MonoBehaviour
{
    public AnalyseSymbolScript[] symbole;

    public GameObject[] highlights;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        foreach (GameObject highlight in highlights)
        {
            highlight.SetActive(false);
        }
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos =
                cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D hit =
                Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider == null)
                return;

            AnalyseSymbolScript symbol =
                hit.collider.GetComponent<AnalyseSymbolScript>();

            if (symbol == null)
                return;

            ShowHighlight(symbol.symbolID);
        }
    }

    void ShowHighlight(int id)
    {
        foreach (GameObject highlight in highlights)
        {
            highlights[id].SetActive(true);
        }               
    }
}