using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;
    
    [Header("Video Settings")]
    [SerializeField] private GameObject videoObject;
    [SerializeField] private bool autoSkipAfterVideo = true;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource menuAudioSource;
    [SerializeField] private AudioClip menuBackgroundMusic;
    [SerializeField] private float musicVolume = 0.5f;
    [SerializeField] private bool loopMusic = true;
    
    private VideoPlayer videoPlayer;
    
    private void Start()
    {
        // Nonaktifkan menu panel di awal
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
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
        
        if (videoObject != null)
        {
            videoPlayer = videoObject.GetComponent<VideoPlayer>();
            
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += OnVideoFinished;
                
                videoObject.SetActive(true);
                videoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("VideoPlayer tidak ditemukan di GameObject!");
                ShowMenu();
            }
        }
        else
        {
            ShowMenu();
        }
    }
    
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (autoSkipAfterVideo)
        {
            ShowMenu();
        }
    }
    
    private void Update()
    {
        // Skip video dengan tombol S atau Space
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space))
        {
            SkipVideo();
        }
    }
    
    public void SkipVideo()
    {
        ShowMenu();
    }
    
    private void ShowMenu()
    {
        if (videoObject != null)
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
            
            videoObject.SetActive(false);
        }
        
        // Tampilkan menu panel
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        
        // Putar background music untuk menu
        PlayMenuMusic();
        
        // Unsubscribe event
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
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
    
    // Menu button functions
    public void Play()
    {
        // Optional: Stop music sebelum pindah scene
        StopMenuMusic();
        SceneManager.LoadScene("Stage1");
    }
    
    public void Chapter()
    {
        // Implementasi chapter menu
    }

    public void Setting()
    {
        // Implementasi setting menu
    }

    public void Credit()
    {
        // Implementasi credit menu
    }
    
    public void Exit()
    {
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void OnDestroy()
    {
        // Cleanup event untuk menghindari memory leak
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}