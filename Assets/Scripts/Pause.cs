using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private GameObject resumeIcon;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject disableInstruction;

    public void pause()
    {
        pauseIcon.SetActive(false);
        resumeIcon.SetActive(true);
        pauseMenuUI.SetActive(true);
        disableInstruction.SetActive(false);
        Time.timeScale = 0f; // Hentikan waktu dalam game
    }
    public void resume()
    {
        pauseIcon.SetActive(true);
        resumeIcon.SetActive(false);
        pauseMenuUI.SetActive(false);
        disableInstruction.SetActive(true);
        Time.timeScale = 1f; // Lanjutkan waktu dalam game
    }

    public void BacktoMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
