using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueKeycard : MonoBehaviour
{
    public enum InteractionType
    {
        PressE,
        AutoTrigger
    }
    
    [Header("Interaction Settings")]
    [SerializeField] private InteractionType interactionType = InteractionType.PressE;
    
    [Header("Buat Press E Aja")]
    [SerializeField] private GameObject eventDetect;

    [Header("Dialogue Settings")]
    public Dialogue dialogue;
    [SerializeField] private Dialogue successDialogue;

    [Header("Keycard Settings")]
    [SerializeField] private string keycardName = "Security Card";

    [Header("Pickup Settings")]
    [SerializeField] private GameObject itemModel;
    [SerializeField] private GameObject keycardPopup;
    [SerializeField] private float popupDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip keycardPickupSFX;
    [SerializeField] private AudioClip popupShowSFX;
    [SerializeField] private float sfxVolume = 0.7f;

    [Header("Puzzle Settings")]
    [SerializeField] private PuzzleController puzzleController;

    [Header("Instruction Settings")]
    [SerializeField] private GameObject instructionToDisable;  // Kolom 1: Instruction yang akan dimatikan
    [SerializeField] private GameObject instructionToEnable;   // Kolom 2: Instruction yang akan dihidupkan

    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool popupActive = false;
    private Coroutine popupCoroutine;
    private GameObject currentPopup;
    private Coroutine interactionCoroutine;
    private CanvasGroup currentCanvasGroup;

    // Static property untuk kontrol player movement
    public static bool IsAnyPopupActive { get; private set; }

    private void Start()
    {
        // Cari PuzzleController jika belum diassign
        if (puzzleController == null)
        {
            puzzleController = FindObjectOfType<PuzzleController>();
        }
        
        // Jika sudah punya keycard, sembunyikan model
        if (GameManager.Instance != null && GameManager.Instance.HasKeycard && itemModel != null)
        {
            itemModel.SetActive(false);
            GetComponent<Collider2D>().enabled = false;
        }
        
        // Setup visual
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        // Nonaktifkan popup di awal
        if (keycardPopup != null)
        {
            keycardPopup.SetActive(false);
            EnsureCanvasGroup(keycardPopup);
        }
    }

    private void EnsureCanvasGroup(GameObject popup)
    {
        if (popup != null)
        {
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = popup.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = true;
        
        if (interactionType == InteractionType.PressE && eventDetect != null)
        {
            eventDetect.SetActive(true);
        }

        if (interactionType == InteractionType.AutoTrigger && !hasTriggered)
        {
            hasTriggered = true;
            TriggerDialogue();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = false;
        
        if (interactionType == InteractionType.PressE && eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
    }

    private void Update()
    {
        if (interactionType == InteractionType.PressE && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            HandleInteraction();
        }
        
        if (popupActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            CloseCurrentPopupWithFade();
        }
    }

    private void HandleInteraction()
    {
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
        }
        
        interactionCoroutine = StartCoroutine(KeycardPickupSequence());
    }

    // ========== SEQUENCE UNTUK KEYCARD PICKUP ==========
    private IEnumerator KeycardPickupSequence()
    {
        Debug.Log("Memulai KeycardPickupSequence");
        
        bool alreadyHasKeycard = false;
        if (GameManager.Instance != null)
        {
            alreadyHasKeycard = GameManager.Instance.HasKeycard;
        }
        
        if (!alreadyHasKeycard)
        {
            Debug.Log("STEP 1: Menampilkan dialog...");
            
            // Tunggu dialog selesai
            yield return StartCoroutine(ShowDialogueSimple(dialogue));
            
            Debug.Log("Dialog SELESAI! Sekarang membuka puzzle...");
            
            // Buka puzzle setelah dialog
            if (puzzleController != null)
            {
                puzzleController.OpenPuzzle();
                Debug.Log("Puzzle dibuka");
            }
            else
            {
                Debug.LogError("PuzzleController tidak ditemukan!");
                // Fallback: langsung beri keycard jika tidak ada puzzle
                GiveKeycard();
            }
        }
        else
        {
            TriggerDialogue();
        }
        
        interactionCoroutine = null;
    }

    // ========== METHOD UNTUK DIPANGGIL DARI PUZZLECONTROLLER ==========
    public void ShowKeycardPopupAfterPuzzle()
    {
        Debug.Log("=== DIALOGUEKEYCARD: ShowKeycardPopupAfterPuzzle() dipanggil ===");
        
        // Beri keycard setelah puzzle selesai
        GiveKeycard();
        
        // Tampilkan popup
        if (keycardPopup != null)
        {
            StartCoroutine(ShowPopupWithDelay(keycardPopup, $"✓ {keycardName} terbuka!", true));
            Debug.Log("Popup keycard ditampilkan");
        }
        else
        {
            Debug.LogWarning("KeycardPopup null");
        }
    }

    // ========== METHOD UNTUK MEMBERI KEYCARD ==========
    private void GiveKeycard()
    {
        Debug.Log("=== MEMBERI KEYCARD ===");
        
        // Simpan status ke GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetKeycard(true, keycardName);
            Debug.Log($"Keycard diberikan: {keycardName}");
        }
        else
        {
            Debug.LogError("GameManager tidak ditemukan!");
        }
        
        // PUTAR SFX KEYCARD PICKUP
        PlaySFX(keycardPickupSFX);
        
        // Sembunyikan model item
        if (itemModel != null)
        {
            itemModel.SetActive(false);
        }
        
        // Nonaktifkan interaksi
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        // Disable collider untuk mencegah interaksi ulang
        GetComponent<Collider2D>().enabled = false;
        
        // ========== UBAH INSTRUCTION ==========
        ChangeInstruction();
        
        Debug.Log($"Player mendapatkan {keycardName}");
    }

    // ========== METHOD UNSTRUKSI ==========
    private void ChangeInstruction()
    {
        if (instructionToDisable != null)
        {
            instructionToDisable.SetActive(false);
            Debug.Log($"Instruction dimatikan: {instructionToDisable.name}");

        }
        
        if (instructionToEnable != null)
        {
            instructionToEnable.SetActive(true);
            Debug.Log($"Instruction dihidupkan: {instructionToEnable.name}");
        }
        else
        {
            Debug.LogWarning("instructionToEnable null - tidak ada instruction baru yang diaktifkan");
        }
    }

    // ========== DIALOGUE SYSTEM ==========
    private IEnumerator ShowDialogueSimple(Dialogue dialogueToShow)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance NULL!");
            yield break;
        }
        
        DialogueManager.Instance.StartDialogue(dialogueToShow);
        
        yield return new WaitForSeconds(0.1f);
        
        while (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
        {
            yield return null;
        }
        
        Debug.Log("Dialogue selesai");
    }

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance NULL!");
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogue);
    }

    // ========== POPUP SYSTEM ==========
    private IEnumerator ShowPopupWithDelay(GameObject popup, string message, bool useFade = true)
    {
        Debug.Log($"Menampilkan popup: {popup.name}");
        
        yield return null;
        
        Text textComponent = popup.GetComponentInChildren<Text>(true);
        if (textComponent != null)
        {
            textComponent.text = message;
        }
        
        popup.SetActive(true);
        currentPopup = popup;
        popupActive = true;
        IsAnyPopupActive = true;
        
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }
        currentCanvasGroup = canvasGroup;
        
        if (useFade)
        {
            yield return StartCoroutine(FadePopup(canvasGroup, 0f, 1f, 0.3f));
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
        
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        PlaySFX(popupShowSFX);
        
        Debug.Log($"Popup aktif. Global status: {IsAnyPopupActive}");
        
        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);
        
        popupCoroutine = StartCoroutine(AutoClosePopupWithFade(canvasGroup, useFade));
    }

    private IEnumerator FadePopup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = toAlpha;
    }

    private IEnumerator AutoClosePopupWithFade(CanvasGroup canvasGroup, bool useFade = true)
    {
        yield return new WaitForSeconds(popupDuration);
        
        if (useFade && canvasGroup != null)
        {
            yield return StartCoroutine(FadePopup(canvasGroup, 1f, 0f, fadeOutDuration));
        }
        
        CloseCurrentPopup();
    }

    private void CloseCurrentPopupWithFade()
    {
        if (currentPopup != null && currentCanvasGroup != null)
        {
            StartCoroutine(ClosePopupWithFadeCoroutine());
        }
        else
        {
            CloseCurrentPopup();
        }
    }

    private IEnumerator ClosePopupWithFadeCoroutine()
    {
        if (currentCanvasGroup != null)
        {
            yield return StartCoroutine(FadePopup(currentCanvasGroup, 1f, 0f, fadeOutDuration));
        }
        
        CloseCurrentPopup();
    }

    private void CloseCurrentPopup()
    {
        if (currentPopup != null)
        {
            if (currentCanvasGroup != null)
            {
                currentCanvasGroup.alpha = 0f;
                currentCanvasGroup.interactable = false;
                currentCanvasGroup.blocksRaycasts = false;
            }
            
            currentPopup.SetActive(false);
            popupActive = false;
            IsAnyPopupActive = false;
            
            currentPopup = null;
            currentCanvasGroup = null;
        }
        
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
    }

    // ========== AUDIO METHODS ==========
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
}