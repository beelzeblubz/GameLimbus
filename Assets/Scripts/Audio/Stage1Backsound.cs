using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Backsound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 2f;    // Durasi fade in
    [SerializeField] private float targetVolume = 1f;      // Volume akhir setelah fade in
    [SerializeField] private bool startWithFadeIn = true;  // Mulai dengan fade in
    
    private float originalVolume;
    private Coroutine fadeCoroutine;
    
    private void Start()
    {
        // Pastikan AudioSource ada dan diassign di Inspector
        if (audioSource == null)
        {
            Debug.LogError("AudioSource belum diassign di Inspector!");
            return;
        }
        
        // Simpan volume asli
        originalVolume = audioSource.volume;
        
        // Mulai memutar backsound dengan fade in
        if (startWithFadeIn)
        {
            StartWithFadeIn();
        }
        else
        {
            PlayBackgroundMusic();
        }
    }
    
    // Mulai dengan fade in
    public void StartWithFadeIn()
    {
        if (audioSource == null) return;
        
        // Set volume ke 0 terlebih dahulu
        audioSource.volume = 0f;
        
        // Mulai memutar audio
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
        
        // Mulai fade in
        StartFadeIn();
    }
    
    // Mulai fade in
    public void StartFadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }
    
    // Coroutine untuk fade in
    private IEnumerator FadeInCoroutine()
    {
        Debug.Log("Memulai fade in BGM...");
        
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeInDuration);
            
            // Smooth fade menggunakan lerp
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            
            yield return null;
        }
        
        // Pastikan volume tepat di target
        audioSource.volume = targetVolume;
        
        Debug.Log($"Fade in selesai. Volume: {audioSource.volume}");
        fadeCoroutine = null;
    }
    
    // Fade out
    public void StartFadeOut(float duration = 1f)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
    }
    
    // Coroutine untuk fade out
    private IEnumerator FadeOutCoroutine(float duration)
    {
        Debug.Log("Memulai fade out BGM...");
        
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Smooth fade out
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            
            yield return null;
        }
        
        // Pastikan volume 0
        audioSource.volume = 0f;
        
        Debug.Log("Fade out selesai. BGM dimatikan.");
        fadeCoroutine = null;
        
        // Optional: Stop audio setelah fade out
        audioSource.Stop();
    }
    
    // Crossfade dari audio ini ke audio lain
    public void CrossfadeTo(AudioSource otherAudio, float duration = 1f)
    {
        StartCoroutine(CrossfadeCoroutine(otherAudio, duration));
    }
    
    // Coroutine untuk crossfade
    private IEnumerator CrossfadeCoroutine(AudioSource otherAudio, float duration)
    {
        Debug.Log("Memulai crossfade...");
        
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;
        
        // Mulai audio baru dengan volume 0
        if (otherAudio != null && !otherAudio.isPlaying)
        {
            otherAudio.volume = 0f;
            otherAudio.Play();
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Fade out audio ini
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            
            // Fade in audio baru
            if (otherAudio != null)
            {
                otherAudio.volume = Mathf.Lerp(0f, targetVolume, t);
            }
            
            yield return null;
        }
        
        // Pastikan volume tepat
        audioSource.volume = 0f;
        if (otherAudio != null)
        {
            otherAudio.volume = targetVolume;
        }
        
        // Stop audio ini
        audioSource.Stop();
        
        Debug.Log("Crossfade selesai");
    }
    
    // ========== METHOD ORIGINAL (TETAP ADA) ==========
    public void PlayBackgroundMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    public void PauseBackgroundMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    public void ResumeBackgroundMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
    
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    public void SetVolumeWithFade(float targetVolume, float duration = 1f)
    {
        StartCoroutine(FadeToVolumeCoroutine(targetVolume, duration));
    }
    
    private IEnumerator FadeToVolumeCoroutine(float targetVol, float duration)
    {
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            audioSource.volume = Mathf.Lerp(startVolume, targetVol, t);
            
            yield return null;
        }
        
        audioSource.volume = targetVol;
    }
    
    // Method untuk mendapatkan status fade
    public bool IsFading()
    {
        return fadeCoroutine != null;
    }
    
    // Method untuk mengatur fade settings
    public void SetFadeSettings(float fadeInTime, float finalVolume)
    {
        fadeInDuration = Mathf.Max(0.1f, fadeInTime);
        targetVolume = Mathf.Clamp01(finalVolume);
    }
}