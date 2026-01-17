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
    public Dialogue dialogue;
    [SerializeField] private Dialogue successDialogue;
    [SerializeField] private Dialogue lockedDialogue;
    [SerializeField] private Dialogue transitionDialogue;

    [Header("Keycard Settings")]
    [SerializeField] private string requiredKeycardName = "Security Card";

    [Header("Lift Settings")]
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject unlockedIndicator;
    [SerializeField] private string nextSceneName = "Stage2";
    
    [Header("Transition Settings")]
    [SerializeField] private GameObject blackScreen;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackScreenDuration = 3f;
    [SerializeField] private float dialogueDelay = 1f;
    [SerializeField] private float dialogueDuration = 1f;
    [SerializeField] private float sceneLoadDelay = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip transitionSFX;
    [SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private float audioFadeOutDuration = 1f; // TAMBAHKAN: Durasi fade out backsound

    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool isTransitionActive = false;
    private bool isLiftLocked = true;
    private Coroutine transitionCoroutine;
    private Coroutine interactionCoroutine;
    private CanvasGroup blackScreenCanvasGroup;
    private AudioSource[] backgroundAudioSources; // Untuk menyimpan semua background audio
    private float[] originalAudioVolumes; // Untuk menyimpan volume asli

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
    }

    private void FindBackgroundAudioSources()
    {
        // Cari semua AudioSource di scene
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        List<AudioSource> bgmList = new List<AudioSource>();
        List<float> volumeList = new List<float>();
        
        foreach (AudioSource audioSource in allAudioSources)
        {
            // Filter: hanya ambil yang loop (backsound) dan bukan SFX
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
    }

    private void HandleInteraction()
    {
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
        }
        
        interactionCoroutine = StartCoroutine(LiftSequence());
    }

    private IEnumerator LiftSequence()
    {
        Debug.Log("Memulai LiftSequence");
        
        if (isLiftLocked)
        {
            bool playerHasKeycard = false;
            if (gameManager != null)
            {
                playerHasKeycard = gameManager.HasKeycard;
            }
            
            if (playerHasKeycard)
            {
                // STEP 1: Dialog "Lift terbuka!"
                Debug.Log("STEP 1: Menampilkan dialog lift terbuka...");
                if (successDialogue != null && DialogueManager.Instance != null)
                {
                    yield return StartCoroutine(ShowDialogueAndWait(successDialogue));
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
                
                // STEP 3: Mulai transition dengan layar hitam
                Debug.Log("STEP 3: Memulai transition...");
                yield return StartCoroutine(StartTransition());
                
                // STEP 4: Load scene
                Debug.Log("STEP 4: Loading scene...");
                StartCoroutine(LoadNextScene());
            }
            else
            {
                if (lockedDialogue != null && DialogueManager.Instance != null)
                {
                    yield return StartCoroutine(ShowDialogueAndWait(lockedDialogue));
                }
                else
                {
                    ShowSimpleMessage($"Lift terkunci. Butuh {requiredKeycardName} untuk membukanya.");
                }
                Debug.Log("Lift terkunci, butuh keycard");
            }
        }
        else
        {
            // Lift sudah terbuka
            ShowSimpleMessage("Naik lift ke Stage 2...");
            yield return new WaitForSeconds(1.5f);
            
            // Mulai transition
            yield return StartCoroutine(StartTransition());
            
            // Load scene
            StartCoroutine(LoadNextScene());
        }
        
        interactionCoroutine = null;
    }

    // ========== TRANSITION SYSTEM DENGAN FADE OUT AUDIO ==========
    private IEnumerator StartTransition()
    {
        if (blackScreen == null)
        {
            Debug.LogError("BlackScreen tidak diatur!");
            yield break;
        }
        
        // Simpan sorting order canvas dialogue asli
        int originalDialogueSortOrder = 0;
        Canvas dialogueCanvas = null;
        
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
        
        // Mulai transition
        isTransitionActive = true;
        IsAnyTransitionActive = true;
        
        Debug.Log("Memulai transition dengan fade out audio...");
        
        // STEP A: FADE OUT BACKGROUND AUDIO BERSAMAAN DENGAN FADE IN BLACK SCREEN
        Coroutine audioFadeCoroutine = null;
        if (backgroundAudioSources.Length > 0 && audioFadeOutDuration > 0)
        {
            audioFadeCoroutine = StartCoroutine(FadeOutAllBackgroundAudio());
        }
        
        // STEP B: FADE IN BLACK SCREEN (bersamaan dengan fade out audio)
        blackScreen.SetActive(true);
        
        Canvas blackScreenCanvas = blackScreen.GetComponentInParent<Canvas>();
        if (blackScreenCanvas != null)
        {
            blackScreenCanvas.sortingOrder = 99;
        }
        
        yield return StartCoroutine(FadeCanvasGroup(blackScreenCanvasGroup, 0f, 1f, fadeInDuration));
        
        // STEP C: Tunggu fade out audio selesai (jika lebih lama dari fade in)
        if (audioFadeCoroutine != null)
        {
            yield return audioFadeCoroutine;
            Debug.Log("Fade out audio selesai");
        }
        
        // PUTAR SFX TRANSITION
        PlaySFX(transitionSFX);
        
        // TUNGGU sebelum dialogue muncul
        yield return new WaitForSeconds(dialogueDelay);
        
        // TAMPILKAN TRANSITION DIALOGUE (jika ada)
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
        
        // TUNGGU sisa waktu black screen
        float elapsedTime = fadeInDuration + dialogueDelay + dialogueDuration;
        if (elapsedTime < blackScreenDuration)
        {
            float remainingTime = blackScreenDuration - elapsedTime;
            Debug.Log($"Menunggu {remainingTime} detik sebelum load scene...");
            yield return new WaitForSeconds(remainingTime);
        }
        
        // KEMBALIKAN sorting order dialogue canvas ke semula
        if (dialogueCanvas != null)
        {
            dialogueCanvas.sortingOrder = originalDialogueSortOrder;
            Debug.Log($"Dialogue canvas sort order dikembalikan: 100 -> {originalDialogueSortOrder}");
        }
        
        Debug.Log("Transition selesai!");
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
            float volumeMultiplier = 1f - t; // Dari 1 ke 0
            
            // Apply fade ke semua background audio
            for (int i = 0; i < backgroundAudioSources.Length; i++)
            {
                if (backgroundAudioSources[i] != null && i < originalAudioVolumes.Length)
                {
                    backgroundAudioSources[i].volume = originalAudioVolumes[i] * volumeMultiplier;
                }
            }
            
            yield return null;
        }
        
        // Pastikan volume 0 di akhir
        for (int i = 0; i < backgroundAudioSources.Length; i++)
        {
            if (backgroundAudioSources[i] != null)
            {
                backgroundAudioSources[i].volume = 0f;
                backgroundAudioSources[i].Stop(); // Optional: stop audio setelah fade out
                Debug.Log($"Audio {backgroundAudioSources[i].gameObject.name} di-stop setelah fade out");
            }
        }
        
        Debug.Log("Semua background audio telah di-fade out");
    }

    // ========== DIALOGUE METHODS ==========
    private IEnumerator ShowDialogueAndWait(Dialogue dialogueToShow)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance NULL!");
            yield break;
        }
        
        DialogueManager.Instance.StartDialogue(dialogueToShow);
        
        yield return new WaitUntil(() => DialogueManager.Instance.isDialoguesActive);
        yield return new WaitWhile(() => DialogueManager.Instance.isDialoguesActive);
        
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

    // ========== SCENE LOADING ==========
    private IEnumerator LoadNextScene()
    {
        Debug.Log($"Menunggu {sceneLoadDelay} detik sebelum load scene...");
        yield return new WaitForSeconds(sceneLoadDelay);
        
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Loading scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("Next scene name belum diatur!");
        }
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
}