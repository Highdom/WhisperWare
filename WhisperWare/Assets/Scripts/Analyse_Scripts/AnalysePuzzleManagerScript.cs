using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnalysePuzzleManager : MonoBehaviour
{
    [Header("Solution sprites (ordered)")]
    public Transform[] solutions;

    [Header("Symbol sprites (will be shuffled)")]
    public Transform[] symbols;

    [Header("Animation")]
    public float delayBetweenItems = 0.3f;
    public float animationDuration = 0.4f;
    public float symbolSpacing = 1.3f;

    public GameObject[] highlights;

    private Camera cam;

    bool opened = false;

    void Start()
    {
        foreach (Transform t in solutions)
            t.gameObject.SetActive(false);

        foreach (Transform t in symbols)
            t.gameObject.SetActive(false);

        cam = Camera.main;

        foreach (GameObject highlight in highlights)
        {
            highlight.SetActive(false);
        }
    }

    void Update()
    {
        if (opened)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                opened = true;
                StartCoroutine(ShowPuzzle());
            }
        }

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

    IEnumerator ShowPuzzle()
    {
        // show solutions in order
        foreach (Transform solution in solutions)
        {
            yield return StartCoroutine(ShowAnimated(solution));
            yield return new WaitForSeconds(delayBetweenItems);
        }

        Shuffle(symbols);

        float startX =
            transform.position.x -
            ((symbols.Length - 1) * symbolSpacing) / 2f;

        float y =
            transform.position.y -
            2.0f;

        // place symbols below Analyse_Puzzle
        for (int i = 0; i < symbols.Length; i++)
        {
            symbols[i].position =
                new Vector3(
                    startX + i * symbolSpacing,
                    y,
                    0);

            yield return StartCoroutine(ShowAnimated(symbols[i]));
            yield return new WaitForSeconds(delayBetweenItems);
        }
    }

    IEnumerator ShowAnimated(Transform obj)
    {
        obj.gameObject.SetActive(true);

        Vector3 targetScale = obj.localScale;

        obj.localScale = Vector3.zero;

        float t = 0;

        while (t < animationDuration)
        {
            t += Time.deltaTime;

            float p = t / animationDuration;

            obj.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    p);

            yield return null;
        }

        obj.localScale = targetScale;
    }

    void Shuffle(Transform[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex =
                Random.Range(i, array.Length);

            Transform temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
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