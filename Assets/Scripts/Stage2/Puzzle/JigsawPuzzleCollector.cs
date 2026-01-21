using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JigsawPuzzleCollector : MonoBehaviour
{
    public enum InteractionType
    {
        PressE,
        AutoTrigger
    }
    
    [Header("Interaction Settings")]
    [SerializeField] private InteractionType interactionType = InteractionType.PressE;
    
    [Header("Press E Indicator")]
    [SerializeField] private GameObject eventDetect;

    [Header("Dialogue Settings")]
    public Dialogue dialogue;
    [SerializeField] private Dialogue successDialogue;

    [Header("Puzzle Piece Settings")]
    [SerializeField] private string pieceName = "Puzzle Piece";
    [SerializeField] private Sprite pieceImage; // Gambar puzzle piece untuk inventory
    [SerializeField] private int pieceID = 0; // ID unik untuk setiap piece

    [Header("Popup Settings")]
    [SerializeField] private GameObject puzzlePiecePopup;
    [SerializeField] private Image pieceImageUI;
    [SerializeField] private Text pieceNameText;
    [SerializeField] private float popupDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip piecePickupSFX;
    [SerializeField] private AudioClip popupShowSFX;
    [SerializeField] private float sfxVolume = 0.7f;

    [Header("Instruction Settings")]
    [SerializeField] private GameObject instructionToDisable;
    [SerializeField] private GameObject instructionToEnable;

    [Header("Object to Disable After Pickup")]
    [SerializeField] private GameObject lightingToDisable;
    [SerializeField] private float disableDelay = 0.2f;

    [Header("Puzzle Progress Settings")]
    [SerializeField] private bool isLastPiece = false; // Centang jika ini piece terakhir
    [SerializeField] private GameObject doorToOpen; // Pintu yang akan terbuka setelah puzzle selesai
    [SerializeField] private AudioClip doorOpenSFX;

    // Status
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool popupActive = false;
    private Coroutine popupCoroutine;
    private GameObject currentPopup;
    private Coroutine interactionCoroutine;
    private CanvasGroup currentCanvasGroup;
    private PlayerMovement playerMovement;

    // Static untuk kontrol global
    public static bool IsAnyPopupActive { get; private set; }

    private void Start()
    {
        // Cari PlayerMovement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        
        // Cek jika piece sudah dikumpulkan sebelumnya
        if (PuzzleInventory.Instance != null && PuzzleInventory.Instance.HasPiece(pieceID))
        {
            // Nonaktifkan object jika piece sudah didapat
            DisableObjectAfterPickup();
        }
        
        // Setup visual
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        // Setup popup - PASTIKAN SEMUA KOMPONEN NONAKTIF DI AWAL
        InitializePopup();
    }

    private void InitializePopup()
    {
        // Nonaktifkan popup utama dan SEMUA CHILDREN-nya
        if (puzzlePiecePopup != null)
        {
            // Pastikan popup dan semua children tidak aktif
            puzzlePiecePopup.SetActive(false);
            // lightingToDisable.SetActive(false);
            
            // Nonaktifkan komponen UI secara spesifik juga
            if (pieceImageUI != null)
            {
                pieceImageUI.gameObject.SetActive(false);
            }
            
            if (pieceNameText != null)
            {
                pieceNameText.gameObject.SetActive(false);
            }
            
            // HAPUS DEBUG LOG INI - inilah yang menyebabkan log berulang
            // Debug.Log($"Piece {pieceID} - Popup utama dinonaktifkan di Start: {puzzlePiecePopup.name}");
        }
        else
        {
            Debug.LogError($"Piece {pieceID} - Popup reference null!");
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
        
        interactionCoroutine = StartCoroutine(PuzzlePiecePickupSequence());
    }

    // ========== SEQUENCE PICKUP PUZZLE PIECE ==========
    private IEnumerator PuzzlePiecePickupSequence()
    {
        Debug.Log($"=== PICKUP SEQUENCE START: Piece {pieceID} ===");

        bool alreadyHasPiece = false;
        if (PuzzleInventory.Instance != null)
        {
            alreadyHasPiece = PuzzleInventory.Instance.HasPiece(pieceID);
        }
        else
        {
            Debug.LogError("PuzzleInventory.Instance is NULL!");
        }
        
        if (!alreadyHasPiece)
        {
            // Tampilkan dialog
            yield return StartCoroutine(ShowDialogueSimple(dialogue));
            
            // Berikan piece kepada player
            GivePuzzlePiece();
            
            // Tampilkan popup piece
            if (puzzlePiecePopup != null)
            {
                yield return StartCoroutine(ShowPiecePopup()); // ← UBAH jadi yield return
            }
            else
            {
                Debug.LogError("PuzzlePiecePopup is null!");
            }
            
            // ========== CEK JIKA INI PIECE TERAKHIR ==========
            if (PuzzleInventory.Instance != null && PuzzleInventory.Instance.IsPuzzleSolved())
            {
                Debug.Log("Ini piece terakhir! Menampilkan success dialog...");
                
                // Tunggu sebentar sebelum tampilkan success dialog
                yield return new WaitForSeconds(0.5f);
                
                // Tampilkan success dialogue
                if (successDialogue != null && DialogueManager.Instance != null)
                {
                    yield return StartCoroutine(ShowDialogueSimple(successDialogue));
                }
            }
        }
        else
        {
            TriggerDialogue();
        }
        
        Debug.Log($"=== PICKUP SEQUENCE END: Piece {pieceID} ===");
        interactionCoroutine = null;
    }

    // ========== METHOD UNTUK MEMBERI PUZZLE PIECE ==========
    private void GivePuzzlePiece()
    {
        Debug.Log($"=== MEMBERI PUZZLE PIECE: {pieceName} (ID: {pieceID}) ===");
        
        // Simpan ke inventory
        if (PuzzleInventory.Instance != null)
        {
            PuzzleInventory.Instance.AddPiece(pieceID, pieceName, pieceImage);
            
            // Cek jika puzzle sudah lengkap
            if (isLastPiece && PuzzleInventory.Instance.IsPuzzleComplete())
            {
                Debug.Log("PUZZLE LENGKAP! Membuka pintu...");
                OpenDoor();
            }
        }
        else
        {
            Debug.LogError("PuzzleInventory tidak ditemukan!");
        }
        
        // Play SFX
        PlaySFX(piecePickupSFX);
        
        // Nonaktifkan object
        DisableObjectAfterPickup();
        
        // Ubah instruksi
        ChangeInstruction();
    }

    // ========== METHOD UNTUK MENAMPILKAN POPUP PIECE ==========
    private IEnumerator ShowPiecePopup()
    {
        // CEGAH POPUP GANDA
        if (popupActive || IsAnyPopupActive) 
        {
            Debug.LogWarning("Popup sudah aktif, skip...");
            yield break;
        }
        
        // NONAKTIFKAN SEMUA POPUP LAINNYA SEBELUM MENAMPILKAN
        DeactivateAllOtherPopups();
        
        // Setup UI
        SetupPopupUI();
        
        if (puzzlePiecePopup == null)
        {
            Debug.LogError("Puzzle Piece Popup reference null!");
            yield break;
        }
        
        // AKTIFKAN POPUP UTAMA SAJA - Children akan otomatis aktif jika belum dinonaktifkan secara manual
        puzzlePiecePopup.SetActive(true);
        
        // Aktifkan komponen UI yang diperlukan
        if (pieceImageUI != null && !pieceImageUI.gameObject.activeSelf)
        {
            pieceImageUI.gameObject.SetActive(true);
            lightingToDisable.SetActive(false);
        }
        
        if (pieceNameText != null && !pieceNameText.gameObject.activeSelf)
        {
            pieceNameText.gameObject.SetActive(true);
        }
        
        currentPopup = puzzlePiecePopup;
        popupActive = true;
        IsAnyPopupActive = true;
        
        // Blokir pergerakan player
        BlockPlayerMovement();
        
        CanvasGroup canvasGroup = GetOrCreateCanvasGroup(puzzlePiecePopup);
        currentCanvasGroup = canvasGroup;
        
        // Fade in
        yield return StartCoroutine(FadePopup(canvasGroup, 0f, 1f, 0.3f));
        
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        PlaySFX(popupShowSFX);
        
        Debug.Log($"Popup aktif untuk piece {pieceID}");
        
        // ========== TUNGGU SAMPAI POPUP SELESAI ==========
        // Auto close setelah beberapa detik
        yield return new WaitForSeconds(popupDuration);
        
        // Fade out
        yield return StartCoroutine(FadePopup(canvasGroup, 1f, 0f, fadeOutDuration));
        
        // Close popup
        CloseCurrentPopup();
        
        Debug.Log($"Popup selesai untuk piece {pieceID}");
        // ================================================
    }

    private void SetupPopupUI()
    {
        // Set sprite untuk Image UI
        if (pieceImageUI != null && pieceImage != null)
        {
            pieceImageUI.sprite = pieceImage;
        }
        else if (pieceImageUI == null)
        {
            Debug.LogError($"Piece {pieceID}: pieceImageUI null!");
        }
        else if (pieceImage == null)
        {
            Debug.LogError($"Piece {pieceID}: pieceImage sprite null!");
        }
        
        // Set text untuk Name Text
        if (pieceNameText != null)
        {
            pieceNameText.text = $"✓ {pieceName} ditemukan!";
        }
        else
        {
            Debug.LogError($"Piece {pieceID}: pieceNameText null!");
        }
    }

    private CanvasGroup GetOrCreateCanvasGroup(GameObject popup)
    {
        if (popup == null) return null;
        
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        return canvasGroup;
    }

    private void DeactivateAllOtherPopups()
    {
        // Cari semua JigsawPuzzleCollector di scene
        JigsawPuzzleCollector[] allCollectors = FindObjectsOfType<JigsawPuzzleCollector>();
        
        foreach (var collector in allCollectors)
        {
            // Skip diri sendiri
            if (collector == this) continue;
            
            // Nonaktifkan popup collector lain
            if (collector.currentPopup != null)
            {
                collector.CloseCurrentPopup();
            }
            
            // Juga nonaktifkan puzzlePiecePopup langsung
            if (collector.puzzlePiecePopup != null && collector.puzzlePiecePopup.activeSelf)
            {
                collector.puzzlePiecePopup.SetActive(false);
            }
        }
    }

    // ========== METHOD UNTUK MEMBUKA PINTU ==========
    private void OpenDoor()
    {
        if (doorToOpen != null)
        {
            // Nonaktifkan atau animasi buka pintu
            doorToOpen.SetActive(false);
            
            // Play SFX buka pintu
            if (doorOpenSFX != null)
            {
                PlaySFX(doorOpenSFX);
            }
            
            Debug.Log($"Pintu terbuka: {doorToOpen.name}");
        }
        else
        {
            Debug.LogWarning("Tidak ada pintu yang diassign untuk dibuka");
        }
    }

    // ========== METHOD UNTUK MENONAKTIFKAN OBJECT SETELAH PICKUP ==========
    private void DisableObjectAfterPickup()
    {
        // Sembunyikan model jika ada
        if (GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().enabled = false;
        }
        
        // Nonaktifkan collider untuk mencegah interaksi ulang
        GetComponent<Collider2D>().enabled = false;
        
        // Nonaktifkan event detect
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        // Nonaktifkan meja/object setelah delay
        StartCoroutine(DisableTableAfterDelay());
    }

    private IEnumerator DisableTableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);
        
        if (lightingToDisable != null && lightingToDisable.activeSelf)
        {
            lightingToDisable.SetActive(false);
        }
    }

    // ========== METHOD UNTUK UBAH INSTRUCTION ==========
    private void ChangeInstruction()
    {
        if (instructionToDisable != null)
        {
            instructionToDisable.SetActive(false);
        }
        
        if (instructionToEnable != null)
        {
            instructionToEnable.SetActive(true);
        }
    }

    // ========== POPUP CONTROL METHODS ==========
    private void BlockPlayerMovement()
    {
        PlayerMovement.IsMovementBlocked = true;
        
        if (playerMovement != null)
        {
            if (playerMovement.GetType().GetMethod("BlockMovement") != null)
            {
                playerMovement.Invoke("BlockMovement", 0f);
            }
        }
    }

    private void UnblockPlayerMovement()
    {
        PlayerMovement.IsMovementBlocked = false;
        
        if (playerMovement != null)
        {
            if (playerMovement.GetType().GetMethod("UnblockMovement") != null)
            {
                playerMovement.Invoke("UnblockMovement", 0f);
            }
        }
    }

    // private IEnumerator AutoClosePopupWithFade(CanvasGroup canvasGroup, bool useFade = true)
    // {
    //     yield return new WaitForSeconds(popupDuration);
        
    //     if (useFade && canvasGroup != null)
    //     {
    //         yield return StartCoroutine(FadePopup(canvasGroup, 1f, 0f, fadeOutDuration));
    //     }
        
    //     CloseCurrentPopup();
    // }

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
            
            // Nonaktifkan popup utama SAJA, children akan otomatis ikut nonaktif
            currentPopup.SetActive(false);
            popupActive = false;
            IsAnyPopupActive = false;
            
            UnblockPlayerMovement();
            
            currentPopup = null;
            currentCanvasGroup = null;
            
            Debug.Log("Popup ditutup");
        }
        
        // Bersihkan coroutine reference
        if (popupCoroutine != null)
        {
            popupCoroutine = null;
        }
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

    // ========== DIALOGUE METHODS ==========
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