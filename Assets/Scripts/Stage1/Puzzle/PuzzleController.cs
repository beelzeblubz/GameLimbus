using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private GameObject victory;
    [SerializeField] private GameObject mainGameCanvas;
    
    [Header("Settings")]
    [SerializeField] private float puzzleCloseDelay = 2f;
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip puzzleCompleteSFX;
    [SerializeField] private AudioClip wrongSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    
    private CanvasGroup puzzleCanvasGroup;
    private DialogueKeycard dialogueKeycardRef;
    private bool puzzleActive = false;

    void Start()
    {
        // Setup CanvasGroup untuk fade effect
        if (puzzleUI != null)
        {
            puzzleCanvasGroup = puzzleUI.GetComponent<CanvasGroup>();
            if (puzzleCanvasGroup == null)
            {
                puzzleCanvasGroup = puzzleUI.AddComponent<CanvasGroup>();
            }
        }
        
        // Cari DialogueKeycard di scene
        dialogueKeycardRef = FindObjectOfType<DialogueKeycard>();
        
        // Pastikan puzzle UI nonaktif di awal
        if (puzzleUI != null)
            puzzleUI.SetActive(false);
            
        if (victory != null)
            victory.SetActive(false);
    }
    
    public void OpenPuzzle()
    {
        puzzleActive = true;
        PlayerMovement.IsMovementBlocked = true;
        
        // Nonaktifkan main canvas jika ada
        if (mainGameCanvas != null)
        {
            mainGameCanvas.SetActive(false);
        }
        
        // Reset state
        if (victory != null)
        {
            victory.SetActive(false);
        }
        
        // Reset alpha
        if (puzzleCanvasGroup != null)
        {
            puzzleCanvasGroup.alpha = 1f;
        }
        
        // Aktifkan puzzle
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
            Debug.Log("Puzzle dibuka");
        }
    }
    
    public void ClosePuzzle()
    {
        puzzleActive = false;
        PlayerMovement.IsMovementBlocked = false;
        
        if (puzzleUI != null)
            puzzleUI.SetActive(false);
            
        if (mainGameCanvas != null)
            mainGameCanvas.SetActive(true);
            
        Debug.Log("Puzzle ditutup");
    }

    public void Win()
    {
        Debug.Log("Puzzle solved!");
        
        if (victory != null)
            victory.SetActive(true);
            
        // Play SFX
        PlaySFX(puzzleCompleteSFX);
        
        // Mulai sequence untuk menutup puzzle
        StartCoroutine(WinSequence());
    }

    public void Wrong()
    {
        Debug.Log("Wrong selection in puzzle.");
        PlaySFX(wrongSFX);
    }
    
    private IEnumerator WinSequence()
    {
        // Tunggu delay
        yield return new WaitForSeconds(puzzleCloseDelay);
        
        // Fade out puzzle UI
        if (puzzleCanvasGroup != null)
        {
            float elapsedTime = 0f;
            float startAlpha = puzzleCanvasGroup.alpha;
            
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                puzzleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
        }
        
        // Nonaktifkan puzzle UI
        if (puzzleUI != null)
            puzzleUI.SetActive(false);
            
        puzzleActive = false;
        
        // Aktifkan kembali main game canvas
        if (mainGameCanvas != null)
        {
            mainGameCanvas.SetActive(true);
        }
        
        // Aktifkan kembali player movement
        PlayerMovement.IsMovementBlocked = false;
        
        // Tampilkan popup keycard jika ada
        if (dialogueKeycardRef != null)
        {
            Debug.Log("PuzzleController: Memanggil ShowKeycardPopupAfterPuzzle()");
            dialogueKeycardRef.ShowKeycardPopupAfterPuzzle();
        }
        else
        {
            Debug.LogError("DialogueKeycard reference null!");
        }
        
        Debug.Log("Puzzle selesai dan ditutup");
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        GameObject audioObject = new GameObject("TempAudio");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = sfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(audioObject, clip.length + 0.1f);
    }
    
    // Untuk mengecek apakah puzzle sedang aktif
    public bool IsPuzzleActive()
    {
        return puzzleActive;
    }
    
    void OnDisable()
    {
        // Reset state saat disabled
        PlayerMovement.IsMovementBlocked = false;
    }
}