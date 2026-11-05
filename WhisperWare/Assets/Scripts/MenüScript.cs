using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenüScript : MonoBehaviour
{
    public GameObject fortsetzen;
    public GameObject NGame;

    public void Quit()
    {
        Application.Quit();
    }

    public void Play()
    {
        fortsetzen.SetActive(true);
        NGame.SetActive(true);

    }

    public void Einstellung()
    {
        SceneManager.LoadScene("Einstellung");
    }
}
