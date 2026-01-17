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
    
    [Header("Clickable Objects")]
    [SerializeField] private List<GameObject> clickableObjects = new List<GameObject>();
    
    [Header("Settings")]
    [SerializeField] private float puzzleCloseDelay = 2f;
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip objectClickSFX;
    [SerializeField] private AudioClip puzzleCompleteSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    
    private CanvasGroup puzzleCanvasGroup;
    private DialogueKeycard dialogueKeycardRef;
    private bool puzzleActive = false;
    private int objectsRemaining;

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
        
        Debug.Log("=== MEMBUKA PUZZLE ===");
        
        // Nonaktifkan main canvas jika ada
        if (mainGameCanvas != null)
        {
            mainGameCanvas.SetActive(false);
        }
        
        // Setup semua benda yang bisa diklik
        SetupClickableObjects();
        
        // Reset state victory
        if (victory != null)
        {
            victory.SetActive(false);
        }
        
        // Reset alpha puzzle UI
        if (puzzleCanvasGroup != null)
        {
            puzzleCanvasGroup.alpha = 1f;
        }
        
        // Aktifkan puzzle UI
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true);
            Debug.Log($"Puzzle UI aktif");
        }
    }
    
    private void SetupClickableObjects()
    {
        objectsRemaining = clickableObjects.Count;
        Debug.Log($"Total benda yang bisa diklik: {objectsRemaining}");
        
        foreach (GameObject obj in clickableObjects)
        {
            if (obj != null)
            {
                // Aktifkan GameObject
                obj.SetActive(true);
                
                // Tambahkan ScaleProtector jika belum ada
                ScaleProtector scaleProtector = obj.GetComponent<ScaleProtector>();
                if (scaleProtector == null)
                {
                    scaleProtector = obj.AddComponent<ScaleProtector>();
                }
                
                // Force fix scale
                scaleProtector.ForceFixScale();
                
                // Tambahkan ButtonHighlightScale jika belum ada
                ButtonHighlightScale buttonScript = obj.GetComponent<ButtonHighlightScale>();
                if (buttonScript == null)
                {
                    buttonScript = obj.AddComponent<ButtonHighlightScale>();
                    
                    // Setup default
                    buttonScript.highlightScale = 1.2f;
                    buttonScript.scaleSpeed = 10f;
                    buttonScript.sfxVolume = sfxVolume;
                    
                    // Setup visual
                    Image image = obj.GetComponent<Image>();
                    if (image != null)
                    {
                        buttonScript.useColorChange = true;
                        buttonScript.highlightColor = Color.yellow;
                    }
                }
                
                // Setup button script
                buttonScript.SetPuzzleController(this);
                
                if (objectClickSFX != null)
                {
                    buttonScript.SetClickSFX(objectClickSFX);
                }
                
                // Setup click action
                System.Action clickAction = () => HandleObjectClick(obj);
                buttonScript.SetClickAction(clickAction);
                
                Debug.Log($"{obj.name} setup complete. Scale: {obj.transform.localScale}");
            }
        }
    }
    
    // Dipanggil oleh ButtonHighlightScale saat benda diklik
    public void HandleObjectClick(GameObject clickedObject)
    {
        if (!puzzleActive || clickedObject == null) return;
        
        Debug.Log($"Benda diklik: {clickedObject.name}");
        
        // Play SFX
        PlaySFX(objectClickSFX);
        
        // Hilangkan benda dengan coroutine
        StartCoroutine(HideObject(clickedObject));
        
        // Kurangi jumlah benda yang tersisa
        objectsRemaining--;
        Debug.Log($"Benda tersisa: {objectsRemaining}");
        
        // Cek jika semua benda sudah diklik
        if (objectsRemaining == 0)
        {
            Debug.Log("Semua benda sudah diklik! Menang!");
            StartCoroutine(DelayedWin());
        }
    }
    
    private IEnumerator HideObject(GameObject obj)
    {
        if (obj == null) yield break;
        
        // Nonaktifkan button script
        ButtonHighlightScale buttonScript = obj.GetComponent<ButtonHighlightScale>();
        if (buttonScript != null)
        {
            buttonScript.SetInteractable(false);
        }
        
        // Fade out
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }
        
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < 0.3f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / 0.3f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        // Nonaktifkan GameObject
        obj.SetActive(false);
    }
    
    private IEnumerator DelayedWin()
    {
        yield return new WaitForSeconds(0.5f);
        Win();
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
            
        // Play SFX kemenangan
        PlaySFX(puzzleCompleteSFX);
        
        // Mulai sequence untuk menutup puzzle
        StartCoroutine(WinSequence());
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
    
    void OnDisable()
    {
        // Reset state saat disabled
        PlayerMovement.IsMovementBlocked = false;
    }
}