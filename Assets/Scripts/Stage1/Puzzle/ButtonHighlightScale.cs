using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHighlightScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float highlightScale = 1.2f;
    [SerializeField] private float scaleSpeed = 10f;
    
    [Header("SFX Settings")]
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip winSFX;
    [SerializeField] private AudioClip loseSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    
    [Header("Button Type")]
    [SerializeField] private bool isScissorsButton = false;
    [SerializeField] private bool isRockButton = false;
    [SerializeField] private bool isPaperButton = false;
    
    [Header("Puzzle Controller")]
    [SerializeField] private PuzzleController puzzleController;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isInteractable = true;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetScale = originalScale;
        
        // Cari PuzzleController jika tidak diassign
        if (puzzleController == null)
        {
            puzzleController = FindObjectOfType<PuzzleController>();
        }
    }
    
    void Update()
    {
        // Smooth scale animation
        if (rectTransform.localScale != targetScale)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale, 
                targetScale, 
                Time.deltaTime * scaleSpeed
            );
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        targetScale = originalScale * highlightScale;
        
        // Play hover SFX
        if (hoverSFX != null)
        {
            PlaySFX(hoverSFX);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        targetScale = originalScale;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        if (isScissorsButton)
        {
            Debug.Log("Scissors selected - WIN!");
            PlaySFX(winSFX);
            
            // Panggil Win() di PuzzleController
            if (puzzleController != null)
            {
                puzzleController.Win();
            }
            
            // Nonaktifkan semua interaksi
            DisableAllButtons();
        }
        else if (isRockButton || isPaperButton)
        {
            Debug.Log("Wrong selection - LOSE!");
            PlaySFX(loseSFX);
            
            // Anda bisa tambahkan efek untuk pilihan salah di sini
            // Misal: shake animation, red flash, dll.
        }
    }
    
    private void DisableAllButtons()
    {
        isInteractable = false;
        
        // Nonaktifkan semua button di scene yang sama
        ButtonHighlightScale[] allButtons = FindObjectsOfType<ButtonHighlightScale>();
        foreach (var button in allButtons)
        {
            button.isInteractable = false;
            button.targetScale = button.originalScale;
        }
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        GameObject audioObject = new GameObject("SFX");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = sfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(audioObject, clip.length + 0.1f);
    }
    
    void OnDisable()
    {
        // Reset scale saat disabled
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
            targetScale = originalScale;
        }
    }
}