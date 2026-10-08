using UnityEngine;

public class SpriteShufleforAnalyseScript : MonoBehaviour
{
    private SpriteRenderer[] sr;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponentsInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
