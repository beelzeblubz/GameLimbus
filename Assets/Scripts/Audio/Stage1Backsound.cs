using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1Backsound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    
    private void Start()
    {
        // Pastikan AudioSource ada dan diassign di Inspector
        if (audioSource == null)
        {
            Debug.LogError("AudioSource belum diassign di Inspector!");
            return;
        }
        
        // Mulai memutar backsound
        PlayBackgroundMusic();
    }
    
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
}