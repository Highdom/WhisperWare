using UnityEngine;

public class DialogMenager : MonoBehaviour
{
    [Header("Dialog und Ja/Nein")]
    TMPro.TMP_Text dialogText;
    TMPro.TMP_Text Ja;
    TMPro.TMP_Text Nein;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (dialogText == null)
        {
            dialogText = GameObject.Find("DialogText").GetComponent<TMPro.TMP_Text>();
        }

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
