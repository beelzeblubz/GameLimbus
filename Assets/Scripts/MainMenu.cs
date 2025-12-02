using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("CutScene1");
    }

    public void Setting()
    {
        Debug.Log("Setting Click");
    }

    public void Credit()
    {
        Debug.Log("Credit Click");
    }

    public void Exit()
    {
        Debug.Log("Exit Click");
    }
}
