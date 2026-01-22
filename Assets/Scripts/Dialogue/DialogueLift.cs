using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DialogueLift : MonoBehaviour
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
    public Dialogue dialogue; // Dialog pertama kali interact
    [SerializeField] private Dialogue successDialogue; // Dialog saat lift terbuka
    [SerializeField] private Dialogue lockedDialogue; // Dialog saat lift terkunci (interact kedua dan seterusnya)
    [SerializeField] private Dialogue transitionDialogue; // Dialog selama transisi

    [Header("Keycard Settings")]
    [SerializeField] private string requiredKeycardName = "Security Card";

    [Header("Lift Settings")]
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject unlockedIndicator;
    [SerializeField] private string nextSceneName = "Stage2";
    
    [Header("Transition Settings")]
    [SerializeField] private GameObject blackScreen;
    [SerializeField] private GameObject fogMerah;
    [SerializeField] private GameObject instruction;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackScreenDuration = 3f;
    [SerializeField] private float dialogueDelay = 1f;
    [SerializeField] private float dialogueDuration = 1f;
    [SerializeField] private float sceneLoadDelay = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip transitionSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private float audioFadeOutDuration = 1f;

    [Header("Instruction Settings")]
    [SerializeField] private GameObject instructionToDisable;  // Kolom 1: Instruction yang akan dimatikan
    [SerializeField] private GameObject instructionToDisable2;
    [SerializeField] private GameObject instructionToEnable;   // Kolom 2: Instruction yang akan dihidupkan

    [Header("Object Activation Settings")]
    [SerializeField] private GameObject meja;  // Meja Mika yang akan diaktifkan saat lift terkunci

    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool isLiftLocked = true;
    private int interactionCount = 0; // Menghitung berapa kali player berinteraksi
    private bool canInteract = true; // Flag untuk mengontrol apakah player bisa berinteraksi
    private Coroutine transitionCoroutine;
    private Coroutine interactionCoroutine;
    private CanvasGroup blackScreenCanvasGroup;
    private AudioSource[] backgroundAudioSources;
    private float[] originalAudioVolumes;
    
    // GameManager reference
    private GameManager gameManager;

    // Static property untuk kontrol player movement
    public static bool IsAnyTransitionActive { get; private set; }

    private void Start()
    {
        gameManager = GameManager.Instance;
        
        if (gameManager != null && gameManager.IsLiftUnlocked)
        {
            isLiftLocked = false;
            UpdateVisuals();
        }
        
        UpdateVisuals();
        
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        // Setup black screen
        if (blackScreen != null)
        {
            blackScreen.SetActive(false);
            EnsureCanvasGroup(blackScreen);
            blackScreenCanvasGroup = blackScreen.GetComponent<CanvasGroup>();
        }
        
        // Cari semua background audio di scene
        FindBackgroundAudioSources();
        
        // Pastikan meja nonaktif di awal jika tidak null
        if (meja != null)
        {
            meja.SetActive(false);
        }
        
        // Reset status interaksi
        interactionCount = 0;
        canInteract = true;
    }

    private void FindBackgroundAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        List<AudioSource> bgmList = new List<AudioSource>();
        List<float> volumeList = new List<float>();
        
        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource != null && audioSource.loop && audioSource.isPlaying)
            {
                bgmList.Add(audioSource);
                volumeList.Add(audioSource.volume);
                Debug.Log($"Found background audio: {audioSource.gameObject.name}, Volume: {audioSource.volume}");
            }
        }
        
        backgroundAudioSources = bgmList.ToArray();
        originalAudioVolumes = volumeList.ToArray();
        
        Debug.Log($"Total background audio found: {backgroundAudioSources.Length}");
    }

    private void EnsureCanvasGroup(GameObject uiElement)
    {
        if (uiElement != null)
        {
            CanvasGroup canvasGroup = uiElement.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = uiElement.AddComponent<CanvasGroup>();
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
        
        if (interactionType == InteractionType.PressE && eventDetect != null && canInteract)
        {
            eventDetect.SetActive(true);
        }

        if (interactionType == InteractionType.AutoTrigger && !hasTriggered && canInteract)
        {
            hasTriggered = true;
            TriggerFirstDialogue();
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
        // Hanya proses input jika bisa berinteraksi dan player dalam range
        if (interactionType == InteractionType.PressE && playerInRange && Input.GetKeyDown(KeyCode.E) && canInteract)
        {
            HandleInteraction();
        }
    }

    private void HandleInteraction()
    {
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
        }
        
        // Nonaktifkan interaksi sementara
        canInteract = false;
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
        
        interactionCoroutine = StartCoroutine(LiftSequence());
    }

    private IEnumerator LiftSequence()
    {
        Debug.Log($"Memulai LiftSequence - Interaction ke-{interactionCount + 1}");
        
        // Tambah count interaksi
        interactionCount++;
        
        if (isLiftLocked)
        {
            bool playerHasKeycard = false;
            if (gameManager != null)
            {
                playerHasKeycard = gameManager.HasKeycard;
            }
            
            if (playerHasKeycard)
            {
                // STEP 1: BLOCK PLAYER MOVEMENT SAAT DIALOG DIMULAI
                Debug.Log("STEP 1: Block player movement untuk success dialogue...");
                PlayerMovement.SetLiftTransitionBlock(true);
                
                // Tampilkan success dialogue
                Debug.Log("Menampilkan dialog lift terbuka...");
                if (successDialogue != null && DialogueManager.Instance != null)
                {
                    if (instructionToDisable2 != null)
                    {
                        instructionToDisable2.SetActive(false);
                        instruction.SetActive(false);
                    }
                    
                    // Tampilkan success dialogue
                    DialogueManager.Instance.StartDialogue(successDialogue);
                    
                    // Tunggu sampai dialog selesai
                    yield return new WaitUntil(() => DialogueManager.Instance.isDialoguesActive);
                    yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
                    
                    Debug.Log("Success dialogue selesai");
                }
                else
                {
                    ShowSimpleMessage($"Lift terbuka! Menggunakan {requiredKeycardName}");
                    yield return new WaitForSeconds(2f);
                }
                
                // STEP 2: Unlock lift
                Debug.Log("STEP 2: Membuka lift...");
                isLiftLocked = false;
                UpdateVisuals();
                
                if (gameManager != null)
                {
                    gameManager.SetLiftUnlocked(true);
                }
                
                // STEP 3: TETAP BLOCK MOVEMENT DAN LANGSUNG MULAI TRANSITION
                Debug.Log("STEP 3: Tetap block movement dan mulai transition...");
                // fogMerah.SetActive(false);
                instruction.SetActive(false);
                
                // Mulai transition (player movement tetap diblokir)
                yield return StartCoroutine(StartTransition());
                fogMerah.SetActive(false);
                
                // STEP 4: Load scene setelah semua transisi selesai
                Debug.Log("STEP 4: Loading scene...");
                yield return new WaitForSeconds(sceneLoadDelay);
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // ========== SAAT LIFT TERKUNCI ==========
                Debug.Log($"Lift terkunci, player belum punya keycard (Interaction ke-{interactionCount})");
                
                // Tentukan dialog mana yang akan ditampilkan berdasarkan interaction count
                if (interactionCount == 1)
                {
                    // Interaksi pertama: tampilkan dialogue utama
                    Debug.Log("Interaksi pertama: Menampilkan dialogue utama");
                    if (dialogue != null && DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(dialogue);
                        
                        // Tunggu sampai dialog selesai
                        yield return new WaitUntil(() => DialogueManager.Instance.isDialoguesActive);
                        yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
                    }
                    else
                    {
                        ShowSimpleMessage($"Ini lift menuju Stage 2. Tapi terkunci. Butuh {requiredKeycardName}.");
                        yield return new WaitForSeconds(2f);
                    }
                }
                else
                {
                    // Interaksi kedua dan seterusnya: tampilkan lockedDialogue
                    Debug.Log($"Interaksi ke-{interactionCount}: Menampilkan lockedDialogue");
                    if (lockedDialogue != null && DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.StartDialogue(lockedDialogue);
                        
                        // Tunggu sampai dialog selesai
                        yield return new WaitUntil(() => DialogueManager.Instance.isDialoguesActive);
                        yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
                    }
                    else
                    {
                        ShowSimpleMessage($"Lift masih terkunci. Cari {requiredKeycardName} dulu!");
                        yield return new WaitForSeconds(2f);
                    }
                }
                
                // ========== UBAH INSTRUCTION ==========
                ChangeInstruction();
                
                // ========== AKTIFKAN meja ==========
                ActivateMeja();
                
                Debug.Log($"Lift terkunci. Objective diubah, meja diaktifkan");
                
                // ========== KEMBALIKAN INTERAKSI ==========
                canInteract = true;
                if (playerInRange && eventDetect != null)
                {
                    eventDetect.SetActive(true);
                }
            }
        }
        else
        {
            // Lift sudah terbuka dari awal
            Debug.Log("Lift sudah terbuka, block movement dan mulai transition...");
            
            // Block player movement
            PlayerMovement.SetLiftTransitionBlock(true);
            
            // Mulai transition langsung
            yield return StartCoroutine(StartTransition());
            
            // Load scene
            yield return new WaitForSeconds(sceneLoadDelay);
            SceneManager.LoadScene(nextSceneName);
        }
        
        interactionCoroutine = null;
    }

    // Method untuk AutoTrigger - hanya memanggil dialog pertama
    private void TriggerFirstDialogue()
    {
        if (!canInteract) return;
        
        if (interactionCount == 0)
        {
            // Interaksi pertama dengan AutoTrigger
            interactionCount++;
            
            if (dialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogue);
            }
            else
            {
                ShowSimpleMessage($"Ini lift menuju Stage 2. Tapi terkunci. Butuh {requiredKeycardName}.");
            }
        }
        else
        {
            // Interaksi kedua dan seterusnya dengan AutoTrigger
            if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
            else
            {
                ShowSimpleMessage($"Lift masih terkunci. Cari {requiredKeycardName} dulu!");
            }
        }
    }

    // ========== TRANSITION SYSTEM DENGAN DIALOG TRANSI ==========
    private IEnumerator StartTransition()
    {
        if (blackScreen == null)
        {
            Debug.LogError("BlackScreen tidak diatur!");
            yield break;
        }
        
        PlayerMovement.SetLiftTransitionBlock(true);
        int originalDialogueSortOrder = 0;
        Canvas dialogueCanvas = null;
        
        // Atur sorting order untuk dialog agar tampil di atas black screen
        if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueBox != null)
        {
            dialogueCanvas = DialogueManager.Instance.dialogueBox.GetComponentInParent<Canvas>();
            if (dialogueCanvas != null)
            {
                originalDialogueSortOrder = dialogueCanvas.sortingOrder;
                dialogueCanvas.sortingOrder = 100;
                Debug.Log($"Dialogue canvas sort order diubah: {originalDialogueSortOrder} -> 100");
            }
        }
        
        IsAnyTransitionActive = true;
        
        Debug.Log("Memulai transition dengan fade out audio...");
        
        // Mulai fade out audio
        Coroutine audioFadeCoroutine = null;
        if (backgroundAudioSources.Length > 0 && audioFadeOutDuration > 0)
        {
            audioFadeCoroutine = StartCoroutine(FadeOutAllBackgroundAudio());
        }
        
        // Aktifkan black screen
        blackScreen.SetActive(true);
        
        // Atur sorting order black screen
        Canvas blackScreenCanvas = blackScreen.GetComponentInParent<Canvas>();
        if (blackScreenCanvas != null)
        {
            blackScreenCanvas.sortingOrder = 99; // Di bawah dialog
        }
        
        // Fade in black screen
        yield return StartCoroutine(FadeCanvasGroup(blackScreenCanvasGroup, 0f, 1f, fadeInDuration));
        
        // Tunggu fade out audio selesai
        if (audioFadeCoroutine != null)
        {
            yield return audioFadeCoroutine;
            Debug.Log("Fade out audio selesai");
        }
        
        // Mainkan SFX transisi
        PlaySFX(transitionSFX);
        
        // Tunggu sebelum menampilkan dialog transisi
        yield return new WaitForSeconds(dialogueDelay);
        
        // Tampilkan dialog transisi jika ada
        if (transitionDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log("Menampilkan transition dialogue...");
            DialogueManager.Instance.StartDialogue(transitionDialogue);
            
            float dialogueTimer = 0f;
            while (DialogueManager.Instance.isDialoguesActive && dialogueTimer < dialogueDuration)
            {
                dialogueTimer += Time.deltaTime;
                yield return null;
            }
            
            if (DialogueManager.Instance.isDialoguesActive)
            {
                yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
            }
        }
        else if (transitionDialogue == null)
        {
            yield return new WaitForSeconds(dialogueDuration);
        }
        
        Debug.Log("Transition dialogue selesai...");
        
        // Hitung waktu yang sudah berlalu
        float elapsedTime = fadeInDuration + dialogueDelay + dialogueDuration;
        
        // Tunggu sisa waktu black screen jika perlu
        if (elapsedTime < blackScreenDuration)
        {
            float remainingTime = blackScreenDuration - elapsedTime;
            Debug.Log($"Menunggu {remainingTime} detik sebelum load scene...");
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Kembalikan sorting order dialog ke semula
        if (dialogueCanvas != null)
        {
            dialogueCanvas.sortingOrder = originalDialogueSortOrder;
            Debug.Log($"Dialogue canvas sort order dikembalikan: 100 -> {originalDialogueSortOrder}");
        }
        
        Debug.Log("Transition selesai, siap untuk load scene!");
    }

    // ========== AUDIO FADE OUT SYSTEM ==========
    private IEnumerator FadeOutAllBackgroundAudio()
    {
        Debug.Log($"Memulai fade out {backgroundAudioSources.Length} background audio...");
        
        if (backgroundAudioSources.Length == 0 || audioFadeOutDuration <= 0)
        {
            yield break;
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < audioFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / audioFadeOutDuration);
            float volumeMultiplier = 1f - t;
            
            for (int i = 0; i < backgroundAudioSources.Length; i++)
            {
                if (backgroundAudioSources[i] != null && i < originalAudioVolumes.Length)
                {
                    backgroundAudioSources[i].volume = originalAudioVolumes[i] * volumeMultiplier;
                }
            }
            
            yield return null;
        }
        
        for (int i = 0; i < backgroundAudioSources.Length; i++)
        {
            if (backgroundAudioSources[i] != null)
            {
                backgroundAudioSources[i].volume = 0f;
                backgroundAudioSources[i].Stop();
                Debug.Log($"Audio {backgroundAudioSources[i].gameObject.name} di-stop setelah fade out");
            }
        }
        
        Debug.Log("Semua background audio telah di-fade out");
    }

    // ========== METHOD UNTUK MENGUBAH INSTRUCTION ==========
    private void ChangeInstruction()
    {
        // Hanya ubah instruction saat interaksi pertama
        if (interactionCount == 1)
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
    }

    // ========== METHOD UNTUK MENGAKTIFKAN meja ==========
    private void ActivateMeja()
    {
        // Hanya aktifkan meja saat interaksi pertama
        if (interactionCount == 1 && meja != null)
        {
            meja.SetActive(true);
            Debug.Log($"Meja Mika diaktifkan: {meja.name}");
        }
        else if (meja == null)
        {
            Debug.LogWarning("meja GameObject null - tidak ada meja yang diaktifkan");
        }
    }

    private void ShowSimpleMessage(string message)
    {
        Dialogue simpleDialogue = new Dialogue
        {
            dialogueLines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    line = message,
                    character = new DialogueCharacter 
                    { 
                        name = "System",
                        charIcon = null 
                    }
                }
            }
        };
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(simpleDialogue);
        }
    }

    // ========== FADE ANIMATION ==========
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        canvasGroup.alpha = fromAlpha;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = toAlpha;
    }

    // ========== VISUAL UPDATES ==========
    private void UpdateVisuals()
    {
        if (lockedIndicator != null)
            lockedIndicator.SetActive(isLiftLocked);
        
        if (unlockedIndicator != null)
            unlockedIndicator.SetActive(!isLiftLocked);
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

    // Method untuk reset transition status (bisa dipanggil dari scene lain)
    public static void ResetTransitionStatus()
    {
        IsAnyTransitionActive = false;
    }
    
    // Method untuk mendapatkan interaction count (untuk debugging)
    public int GetInteractionCount()
    {
        return interactionCount;
    }
}