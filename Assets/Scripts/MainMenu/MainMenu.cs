using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Image fadePanel;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource menuAudioSource;
    [SerializeField] private AudioClip menuBackgroundMusic;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private bool loopMusic = true;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private Color fadeColor = Color.black;
    
    private bool isTransitioning = false;
    
    private void Start()
    {
        // Setup fade panel
        InitializeFadePanel();
        
        // Aktifkan menu panel langsung
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        
        // Setup audio source jika belum ada
        if (menuAudioSource == null)
        {
            menuAudioSource = GetComponent<AudioSource>();
            if (menuAudioSource == null)
            {
                menuAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Konfigurasi audio source
        if (menuAudioSource != null)
        {
            menuAudioSource.clip = menuBackgroundMusic;
            menuAudioSource.volume = musicVolume;
            menuAudioSource.loop = loopMusic;
            menuAudioSource.playOnAwake = false;
        }
        
        // Putar background music langsung
        PlayMenuMusic();
        
        // Fade in dari blackscreen saat awal game
        StartCoroutine(FadeIn());
    }
    
    private void InitializeFadePanel()
    {
        // Cek apakah fade panel sudah ada
        if (fadePanel == null)
        {
            // Buat GameObject untuk fade panel
            GameObject fadeObject = new GameObject("FadePanel");
            fadePanel = fadeObject.AddComponent<Image>();
            
            // Set parent ke canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            fadeObject.transform.SetParent(canvas.transform, false);
            
            // Setup RectTransform
            RectTransform rectTransform = fadePanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            
            // Setup Image
            fadePanel.color = fadeColor;
            fadePanel.raycastTarget = false;
        }
        
        // Set fade panel ke fully opaque di awal
        Color color = fadePanel.color;
        color.a = 1f;
        fadePanel.color = color;
        fadePanel.gameObject.SetActive(true);
    }
    
    private IEnumerator FadeIn()
    {
        fadePanel.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        Color color = fadePanel.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        
        color.a = 0f;
        fadePanel.color = color;
        fadePanel.gameObject.SetActive(false);
    }
    
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;
        
        // Aktifkan fade panel
        fadePanel.gameObject.SetActive(true);
        
        // Fade out ke blackscreen
        float elapsedTime = 0f;
        Color color = fadePanel.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadePanel.color = color;
        
        // Tunggu sebentar di blackscreen
        yield return new WaitForSeconds(0.2f);
        
        // Stop music dan load scene
        StopMenuMusic();
        SceneManager.LoadScene(sceneName);
        
        isTransitioning = false;
    }
    
    private IEnumerator FadeOutAndQuit()
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;
        
        // Aktifkan fade panel
        fadePanel.gameObject.SetActive(true);
        
        // Fade out ke blackscreen
        float elapsedTime = 0f;
        Color color = fadePanel.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            color.a = alpha;
            fadePanel.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadePanel.color = color;
        
        // Tunggu sebentar di blackscreen
        yield return new WaitForSeconds(0.2f);
        
        // Quit game
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        
        isTransitioning = false;
    }
    
    private void PlayMenuMusic()
    {
        if (menuAudioSource != null && menuBackgroundMusic != null)
        {
            if (!menuAudioSource.isPlaying)
            {
                menuAudioSource.Play();
                Debug.Log("Menu background music dimulai");
            }
        }
        else if (menuBackgroundMusic == null)
        {
            Debug.LogWarning("Menu background music clip belum diassign!");
        }
    }
    
    public void StopMenuMusic()
    {
        if (menuAudioSource != null && menuAudioSource.isPlaying)
        {
            menuAudioSource.Stop();
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (menuAudioSource != null)
        {
            menuAudioSource.volume = musicVolume;
        }
    }
    
    // Menu button functions - MODIFIED WITH FADE EFFECT
    public void Play()
    {
        StartCoroutine(FadeOutAndLoadScene("Stage1"));
    }

    public void Chapter1()
    {
        StartCoroutine(FadeOutAndLoadScene("Stage1"));
    }

    public void Chapter2()
    {
        StartCoroutine(FadeOutAndLoadScene("Stage2"));
    }
    
    public void Chapter()
    {
        // Implementasi chapter menu
        // Jika ada scene tertentu, gunakan StartCoroutine(FadeOutAndLoadScene("SceneName"));
    }

    public void Setting()
    {
        // Implementasi setting menu
        // Jika ada scene setting, gunakan StartCoroutine(FadeOutAndLoadScene("SettingScene"));
    }

    public void Credit()
    {
        // Implementasi credit menu
        // Jika ada scene credit, gunakan StartCoroutine(FadeOutAndLoadScene("CreditScene"));
    }
    
    public void Exit()
    {
        StartCoroutine(FadeOutAndQuit());
    }
    
    // Public method untuk fade yang bisa dipanggil dari luar
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }
    
    // Method untuk mendapatkan status transition
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}