using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class PresentScene : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextScene;
    
    [Header("Cutscene Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float additionalDelayAfterVideo = 0f; // Delay tambahan setelah video selesai (opsional)
    
    [Header("Audio Settings")]
    [SerializeField] private float volumeFadeDuration = 2f; // Durasi fade in volume (dalam detik)
    [SerializeField] private float targetVolume = 1f; // Volume maksimal yang ingin dicapai
    
    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey1 = KeyCode.Space;
    [SerializeField] private KeyCode skipKey2 = KeyCode.Escape;
    
    private bool sceneChangeTriggered = false;
    private AudioSource videoAudioSource;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer belum di-assign! Scene tidak akan berganti.");
            return;
        }
        
        // Dapatkan AudioSource dari VideoPlayer
        videoAudioSource = videoPlayer.GetComponent<AudioSource>();
        if (videoAudioSource == null)
        {
            Debug.LogWarning("VideoPlayer tidak memiliki AudioSource component!");
        }
        else
        {
            // Set volume awal ke 0
            videoAudioSource.volume = 0f;
            Debug.Log("Audio volume dimulai dari 0, akan fade in ke " + targetVolume);
        }
        
        // Subscribe ke event ketika video selesai
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // Subscribe ke event ketika video siap (untuk URL)
        videoPlayer.prepareCompleted += OnVideoPrepared;
        
        // Jika video sudah ada clip, langsung play
        if (videoPlayer.clip != null)
        {
            videoPlayer.Play();
            Debug.Log($"Memutar cutscene (Durasi: {videoPlayer.clip.length} detik)");
            
            // Mulai fade in audio
            if (videoAudioSource != null)
            {
                StartCoroutine(FadeInAudio());
            }
        }
        // Jika menggunakan URL, prepare dulu
        else if (!string.IsNullOrEmpty(videoPlayer.url))
        {
            videoPlayer.Prepare();
            Debug.Log("Mempersiapkan video dari URL...");
        }
        else
        {
            Debug.LogError("VideoPlayer tidak memiliki video clip atau URL!");
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"Video siap. Durasi: {vp.length} detik");
        vp.Play();
        
        // Mulai fade in audio setelah video siap
        if (videoAudioSource != null)
        {
            StartCoroutine(FadeInAudio());
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Cutscene selesai!");
        
        // Tunggu delay tambahan (jika ada) lalu load scene
        if (additionalDelayAfterVideo > 0)
        {
            StartCoroutine(LoadSceneAfterDelay());
        }
        else
        {
            LoadNextScene();
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(additionalDelayAfterVideo);
        LoadNextScene();
    }
    
    private IEnumerator FadeInAudio()
    {
        if (videoAudioSource == null)
        {
            yield break;
        }
        
        float elapsedTime = 0f;
        float startVolume = 0f;
        
        videoAudioSource.volume = startVolume;
        
        while (elapsedTime < volumeFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / volumeFadeDuration);
            videoAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        // Pastikan volume mencapai target akhir
        videoAudioSource.volume = targetVolume;
        Debug.Log($"Audio fade in selesai. Volume sekarang: {targetVolume}");
    }

    private void Update()
    {
        // Skip cutscene jika diizinkan
        if (allowSkip && !sceneChangeTriggered)
        {
            if (Input.GetKeyDown(skipKey1) || Input.GetKeyDown(skipKey2))
            {
                SkipCutscene();
            }
        }
    }

    private void SkipCutscene()
    {
        Debug.Log("Cutscene di-skip oleh player!");
        
        // Stop video jika sedang playing
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Langsung load scene
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (sceneChangeTriggered) return;
        
        sceneChangeTriggered = true;
        
        if (!string.IsNullOrEmpty(nextScene))
        {
            Debug.Log($"Loading scene: {nextScene}");
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogError("Next scene name belum diatur di Inspector!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe dari events untuk menghindari memory leak
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}