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
    [SerializeField] private Dialogue successDialogue;  // Untuk setelah mendapatkan keycard

    [Header("Keycard Settings")]
    [SerializeField] private string keycardName = "Security Card";

    [Header("Pickup Settings")]
    [SerializeField] private GameObject itemModel;      // Model item yang akan hilang saat diambil
    [SerializeField] private GameObject keycardPopup;   // UI popup saat dapat KEYCARD
    [SerializeField] private float popupDuration = 2f;  // Durasi popup tampil
    [SerializeField] private float fadeOutDuration = 0.5f; // Durasi fade out

    [Header("Audio Settings")]
    [SerializeField] private AudioClip keycardPickupSFX; // SFX saat dapat keycard
    [SerializeField] private AudioClip popupShowSFX;     // SFX saat popup muncul
    [SerializeField] private float sfxVolume = 0.7f;     // Volume SFX

    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool popupActive = false;
    private Coroutine popupCoroutine;
    private GameObject currentPopup;
    private Coroutine interactionCoroutine;
    private CanvasGroup currentCanvasGroup;

    // GameManager reference
    private GameManager gameManager;

    // Static property untuk kontrol player movement
    public static bool IsAnyPopupActive { get; private set; }

    private void Start()
    {
        // Cari GameManager di scene
        gameManager = GameManager.Instance;
        
        // Jika ada GameManager, sinkronkan status keycard
        if (gameManager != null)
        {
            // Jika sudah punya keycard, sembunyikan model
            if (gameManager.HasKeycard && itemModel != null)
            {
                itemModel.SetActive(false);
                // Nonaktifkan interaksi juga
                GetComponent<Collider2D>().enabled = false;
            }
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
        
        // PressE
        if (interactionType == InteractionType.PressE && eventDetect != null)
        {
            eventDetect.SetActive(true);
        }

        // AutoTrigger
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
        
        // PressE
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
        
        // Close popup dengan E atau Space jika popup aktif
        if (popupActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            CloseCurrentPopupWithFade();
        }
    }

    private void HandleInteraction()
    {
        // Hentikan coroutine sebelumnya jika ada
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
        }
        
        // Mulai sequence
        interactionCoroutine = StartCoroutine(KeycardPickupSequence());
    }

    // ========== SEQUENCE UNTUK KEYCARD PICKUP ==========
    private IEnumerator KeycardPickupSequence()
    {
        Debug.Log("Memulai KeycardPickupSequence");
        
        // Cek apakah sudah punya keycard
        bool alreadyHasKeycard = false;
        if (gameManager != null)
        {
            alreadyHasKeycard = gameManager.HasKeycard;
        }
        
        if (!alreadyHasKeycard)
        {
            // STEP 1: Tampilkan dialog dulu
            Debug.Log("STEP 1: Menampilkan dialog...");
            
            // Gunakan method yang benar untuk menunggu dialog selesai
            yield return StartCoroutine(ShowDialogueAndWait(dialogue));
            
            // STEP 2: Beri keycard dan tampilkan popup (SETELAH dialog selesai)
            Debug.Log("Dialog SELESAI! Sekarang memberi keycard...");
            GiveKeycard();
        }
        else
        {
            // Jika sudah punya, tampilkan dialog saja
            TriggerDialogue();
        }
        
        interactionCoroutine = null;
    }

    // ========== METHOD YANG BENAR UNTUK MENUNGGU DIALOG ==========
    private IEnumerator ShowDialogueAndWait(Dialogue dialogueToShow)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance NULL!");
            yield break;
        }
        
        // Reset flag dialog aktif
        // bool isDialogueFinished = false;
        
        // Subscribe ke event atau cara lain untuk tahu kapan dialog selesai
        // Karena DialogueManager mungkin tidak punya event, kita buat workaround
        
        // Cara 1: Simpan status awal
        bool wasDialogueActive = DialogueManager.Instance.isDialoguesActive;
        
        // Mulai dialogue
        DialogueManager.Instance.StartDialogue(dialogueToShow);
        
        // Tunggu sampai dialogue mulai
        yield return new WaitUntil(() => DialogueManager.Instance.isDialoguesActive);
        Debug.Log("Dialogue dimulai...");
        
        // Sekarang tunggu sampai dialogue selesai
        yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
        Debug.Log("Dialogue SELESAI sepenuhnya!");
        
        // Tunggu 1 frame tambahan untuk memastikan
        yield return null;
    }

    // ========== ALTERNATIF LEBIH SIMPLE ==========
    private IEnumerator ShowDialogueSimple(Dialogue dialogueToShow)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance NULL!");
            yield break;
        }
        
        // Mulai dialogue
        DialogueManager.Instance.StartDialogue(dialogueToShow);
        
        // Tunggu 0.1 detik untuk memastikan dialog sudah mulai
        yield return new WaitForSeconds(0.1f);
        
        // Sekarang tunggu sampai dialogue selesai
        // Kita check status setiap frame
        while (true)
        {
            // Jika DialogueManager masih ada dan dialog aktif, tunggu
            if (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
            {
                yield return null; // Tunggu frame berikutnya
            }
            else
            {
                // Dialog selesai
                break;
            }
        }
        
        Debug.Log("Dialogue selesai, melanjutkan...");
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

    private void GiveKeycard()
    {
        Debug.Log("=== GIVE KEYCARD ===");
        
        // Simpan status ke GameManager
        if (gameManager != null)
        {
            gameManager.SetKeycard(true, keycardName);
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
        
        Debug.Log($"Player mendapatkan {keycardName}");
        
        // Tampilkan popup - INI DIPANGGIL SETELAH DIALOG SELESAI
        ShowKeycardPopup();
    }

    private void ShowKeycardPopup()
    {
        if (keycardPopup != null)
        {
            StartCoroutine(ShowPopupWithDelay(keycardPopup, $"✓ Mendapatkan {keycardName}!"));
        }
        else
        {
            Debug.LogWarning("KeycardPopup null, skip menampilkan popup");
        }
    }

    // ========== POPUP SYSTEM ==========
    private IEnumerator ShowPopupWithDelay(GameObject popup, string message, bool useFade = true)
    {
        Debug.Log($"Menampilkan popup: {popup.name}");
        
        yield return null;
        
        // Update teks jika ada
        Text textComponent = popup.GetComponentInChildren<Text>(true);
        if (textComponent != null)
        {
            textComponent.text = message;
        }
        
        // Aktifkan GameObject
        popup.SetActive(true);
        currentPopup = popup;
        popupActive = true;
        IsAnyPopupActive = true; // SET STATIC PROPERTY
        
        // Dapatkan CanvasGroup
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }
        currentCanvasGroup = canvasGroup;
        
        // FADE IN
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
        
        // PUTAR SFX POPUP SHOW
        PlaySFX(popupShowSFX);
        
        Debug.Log($"Popup {popup.name} aktif. Global status: {IsAnyPopupActive}");
        
        // Mulai auto close
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
        
        // FADE OUT
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
            IsAnyPopupActive = false; // RESET STATIC PROPERTY
            
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