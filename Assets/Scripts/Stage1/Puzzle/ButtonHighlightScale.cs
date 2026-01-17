using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class ButtonHighlightScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale Settings")]
    public float highlightScale = 1.2f;
    public float scaleSpeed = 10f;
    
    [Header("SFX Settings")]
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip clickSFX;
    public float sfxVolume = 0.7f;
    
    [Header("Visual Settings")]
    public bool useColorChange = false;
    public Color highlightColor = Color.yellow;
    
    // References
    private PuzzleController puzzleController;
    private Action onClickAction;
    
    // State variables
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;
    private bool isInteractable = true;
    private bool isHovered = false;
    
    // Components
    private Image image;
    private SpriteRenderer spriteRenderer;
    private ScaleProtector scaleProtector;
    
    void Awake()
    {
        Debug.Log($"ButtonHighlightScale Awake untuk {name}");
        
        // Initialize ScaleProtector
        InitializeScaleProtector();
        
        // Set original scale
        if (scaleProtector != null)
        {
            originalScale = scaleProtector.GetProtectedScale();
        }
        else
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one;
                transform.localScale = originalScale;
            }
        }
        
        targetScale = originalScale;
        
        // Get visual components
        image = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Save original color
        if (image != null)
        {
            originalColor = image.color;
            useColorChange = true;
        }
        else if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            useColorChange = true;
        }
    }
    
    void Start()
    {
        Debug.Log($"ButtonHighlightScale Start untuk {name}");
        
        // Ensure ScaleProtector exists
        if (scaleProtector == null)
        {
            InitializeScaleProtector();
        }
        
        // Find PuzzleController if not set
        if (puzzleController == null)
        {
            puzzleController = FindObjectOfType<PuzzleController>();
        }
    }
    
    private void InitializeScaleProtector()
    {
        // Get or add ScaleProtector
        scaleProtector = GetComponent<ScaleProtector>();
        if (scaleProtector == null)
        {
            scaleProtector = gameObject.AddComponent<ScaleProtector>();
            Debug.Log($"Added ScaleProtector to {name}");
        }
    }
    
    void Update()
    {
        // Smooth scale animation
        if (transform.localScale != targetScale)
        {
            Vector3 newScale = Vector3.Lerp(
                transform.localScale, 
                targetScale, 
                Time.deltaTime * scaleSpeed
            );
            
            // Set scale safely
            SetScaleSafely(newScale);
        }
    }
    
    // Public methods untuk setup dari PuzzleController
    public void SetPuzzleController(PuzzleController controller)
    {
        puzzleController = controller;
    }
    
    public void SetClickAction(Action action)
    {
        onClickAction = action;
    }
    
    public void SetClickSFX(AudioClip sfx)
    {
        clickSFX = sfx;
    }
    
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        
        if (!interactable && isHovered)
        {
            // Reset jika sedang hover
            targetScale = originalScale;
            ResetColor();
            isHovered = false;
        }
    }
    
    public void ResetVisuals()
    {
        Debug.Log($"ResetVisuals untuk {name}");
        
        targetScale = originalScale;
        
        // Set scale dengan safety check
        SetScaleSafely(originalScale);
        
        ResetColor();
        isHovered = false;
        isInteractable = true;
    }
    
    private void SetScaleSafely(Vector3 scale)
    {
        if (scaleProtector != null)
        {
            scaleProtector.SetProtectedScale(scale);
        }
        else
        {
            // Fallback jika ScaleProtector null
            if (scale == Vector3.zero)
            {
                scale = Vector3.one;
            }
            transform.localScale = scale;
        }
    }
    
    // Pointer events
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        isHovered = true;
        targetScale = originalScale * highlightScale;
        
        // Change color if enabled
        if (useColorChange)
        {
            if (image != null)
            {
                image.color = highlightColor;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = highlightColor;
            }
        }
        
        // Play hover SFX
        if (hoverSFX != null && isInteractable)
        {
            PlaySFX(hoverSFX);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        isHovered = false;
        targetScale = originalScale;
        ResetColor();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        Debug.Log($"ButtonHighlightScale: {name} diklik");
        
        // Play click SFX
        if (clickSFX != null)
        {
            PlaySFX(clickSFX);
        }
        
        // Call custom click action jika ada
        if (onClickAction != null)
        {
            onClickAction.Invoke();
        }
        // Fallback: panggil PuzzleController
        else if (puzzleController != null)
        {
            puzzleController.HandleObjectClick(gameObject);
        }
        else
        {
            Debug.LogWarning($"PuzzleController tidak ditemukan untuk {name}");
        }
    }
    
    private void ResetColor()
    {
        if (image != null)
        {
            image.color = originalColor;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        GameObject audioObject = new GameObject("SFX_" + clip.name);
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = sfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(audioObject, clip.length + 0.5f);
    }
    
    void OnDisable()
    {
        // Reset saat disabled
        SetScaleSafely(originalScale);
        ResetColor();
        isHovered = false;
    }
}