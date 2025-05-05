using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void PlayAgainstAI()
    {
        SceneManager.LoadScene(3);
    }
    public void Analysis()
    {
        SceneManager.LoadScene(2);
    }
        public void PlayOffline()
    {
            SceneManager.LoadScene(1);
    }
}
