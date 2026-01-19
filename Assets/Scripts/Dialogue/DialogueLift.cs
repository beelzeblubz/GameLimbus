using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // Tambahkan ini untuk VideoPlayer

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
    [SerializeField] private GameObject fogMerah;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackScreenDuration = 3f;
    [SerializeField] private float dialogueDelay = 1f;
    [SerializeField] private float dialogueDuration = 1f;
    [SerializeField] private float sceneLoadDelay = 0.5f;

    [Header("Cutscene Settings")]
    [SerializeField] private GameObject cutsceneObject; // GameObject yang mengandung VideoPlayer
    [SerializeField] private bool playCutscene = true;
    [SerializeField] private float cutsceneDelay = 0.5f; // Delay sebelum memulai cutscene
    [SerializeField] private bool disableBlackScreenDuringCutscene = true;

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
    private Coroutine transitionCoroutine;
    private Coroutine interactionCoroutine;
    private Coroutine cutsceneCoroutine;
    private CanvasGroup blackScreenCanvasGroup;
    private AudioSource[] backgroundAudioSources;
    private float[] originalAudioVolumes;
    
    // Cutscene components
    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;

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
        
        // Setup cutscene object
        InitializeCutscene();
        
        // Cari semua background audio di scene
        FindBackgroundAudioSources();
        
        // Pastikan meja nonaktif di awal jika tidak null
        if (meja != null)
        {
            meja.SetActive(false);
        }
    }

    private void InitializeCutscene()
    {
        if (cutsceneObject != null)
        {
            // Cari VideoPlayer di cutsceneObject
            videoPlayer = cutsceneObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = cutsceneObject.GetComponentInChildren<VideoPlayer>();
            }
            
            // Cari AudioSource untuk video
            videoAudioSource = cutsceneObject.GetComponent<AudioSource>();
            if (videoAudioSource == null)
            {
                videoAudioSource = cutsceneObject.GetComponentInChildren<AudioSource>();
            }
            
            // Nonaktifkan cutscene di awal
            cutsceneObject.SetActive(false);
            
            Debug.Log($"Cutscene initialized: VideoPlayer={(videoPlayer != null)}, AudioSource={(videoAudioSource != null)}");
        }
        else
        {
            Debug.LogWarning("CutsceneObject belum di-assign di Inspector!");
        }
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
        
        // Skip cutscene dengan tombol Escape atau Space
        if (cutsceneObject != null && cutsceneObject.activeSelf && 
            (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipCutscene();
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
                    if (instructionToDisable2 != null)
                    {
                        instructionToDisable2.SetActive(false);
                    }
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
                fogMerah.SetActive(false);
                
                // STEP 4: Play cutscene (jika ada)
                if (playCutscene && cutsceneObject != null)
                {
                    Debug.Log("STEP 4: Memainkan cutscene...");
                    yield return StartCoroutine(PlayCutscene());
                }
                
                // STEP 5: Load scene
                Debug.Log("STEP 5: Loading scene...");
                StartCoroutine(LoadNextScene());
            }
            else
            {
                // ========== SAAT LIFT TERKUNCI ==========
                Debug.Log("Lift terkunci, player belum punya keycard");
                
                // Tampilkan dialog terkunci
                if (lockedDialogue != null && DialogueManager.Instance != null)
                {
                    yield return StartCoroutine(ShowDialogueAndWait(lockedDialogue));
                }
                else
                {
                    ShowSimpleMessage($"Lift terkunci. Butuh {requiredKeycardName} untuk membukanya.");
                }
                
                // ========== UBAH INSTRUCTION ==========
                ChangeInstruction();
                
                // ========== AKTIFKAN meja ==========
                ActivateMeja();
                
                Debug.Log($"Lift terkunci. Objective diubah, meja diaktifkan");
            }
        }
        else
        {
            // Lift sudah terbuka
            ShowSimpleMessage("Naik lift ke Stage 2...");
            yield return new WaitForSeconds(1.5f);
            
            // Mulai transition
            yield return StartCoroutine(StartTransition());
            
            // Play cutscene (jika ada)
            if (playCutscene && cutsceneObject != null)
            {
                yield return StartCoroutine(PlayCutscene());
            }
            
            // Load scene
            StartCoroutine(LoadNextScene());
        }
        
        interactionCoroutine = null;
    }

    // ========== CUTSCENE SYSTEM ==========
    private IEnumerator PlayCutscene()
    {
        Debug.Log("Memulai cutscene...");
        
        // Tunggu sebentar sebelum memulai cutscene
        yield return new WaitForSeconds(cutsceneDelay);
        
        // Aktifkan cutscene object

        yield return new WaitForSeconds(0.2f);

        // Nonaktifkan black screen jika diatur
        if (disableBlackScreenDuringCutscene && blackScreen != null)
        {
            blackScreen.SetActive(false);
            Debug.Log("Black screen dinonaktifkan selama cutscene");
        }

        cutsceneObject.SetActive(true);
        // yield return new WaitForSeconds(0.2f);
        
        
        // Setup VideoPlayer jika ada
        if (videoPlayer != null)
        {
            // if (disableBlackScreenDuringCutscene && blackScreen != null)
            // {
            //     blackScreen.SetActive(false);
            //     Debug.Log("Black screen dinonaktifkan selama cutscene");
            // }

            // Pastikan video di-play
            videoPlayer.Play();
            
            // Tunggu sampai video selesai
            yield return new WaitForSeconds((float)videoPlayer.length);
            
            // Atau tunggu sampai video selesai dengan cara yang lebih akurat
            // while (videoPlayer.isPlaying)
            // {
            //     yield return null;
            // }
        }
        else
        {
            // Jika tidak ada VideoPlayer, tunggu beberapa detik
            Debug.LogWarning("Tidak ada VideoPlayer ditemukan, cutscene akan berlangsung 5 detik");
            yield return new WaitForSeconds(5f);
        }
        
        // Nonaktifkan cutscene setelah selesai
        cutsceneObject.SetActive(false);
        
        // Aktifkan kembali black screen jika dinonaktifkan sebelumnya
        if (disableBlackScreenDuringCutscene && blackScreen != null)
        {
            blackScreen.SetActive(true);
            Debug.Log("Black screen diaktifkan kembali setelah cutscene");
        }
        
        Debug.Log("Cutscene selesai");
    }
    
    private void SkipCutscene()
    {
        if (cutsceneCoroutine != null)
        {
            StopCoroutine(cutsceneCoroutine);
        }
        
        // Hentikan video jika sedang diputar
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Nonaktifkan cutscene object
        cutsceneObject.SetActive(false);
        
        // Aktifkan kembali black screen jika perlu
        if (disableBlackScreenDuringCutscene && blackScreen != null)
        {
            blackScreen.SetActive(true);
        }
        
        Debug.Log("Cutscene di-skip");
        
        // Langsung lanjut ke load scene
        StartCoroutine(LoadNextScene());
    }

    // ========== METHOD UNTUK MENGUBAH INSTRUCTION ==========
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

    // ========== METHOD UNTUK MENGAKTIFKAN meja ==========
    private void ActivateMeja()
    {
        if (meja != null)
        {
            meja.SetActive(true);
            Debug.Log($"Meja Mika diaktifkan: {meja.name}");
        }
        else
        {
            Debug.LogWarning("meja GameObject null - tidak ada meja yang diaktifkan");
        }
    }

    // ========== TRANSITION SYSTEM DENGAN FADE OUT AUDIO ==========
    private IEnumerator StartTransition()
    {
        if (blackScreen == null)
        {
            Debug.LogError("BlackScreen tidak diatur!");
            yield break;
        }
        
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
        IsAnyTransitionActive = true;
        
        Debug.Log("Memulai transition dengan fade out audio...");
        
        Coroutine audioFadeCoroutine = null;
        if (backgroundAudioSources.Length > 0 && audioFadeOutDuration > 0)
        {
            audioFadeCoroutine = StartCoroutine(FadeOutAllBackgroundAudio());
        }
        
        blackScreen.SetActive(true);
        
        Canvas blackScreenCanvas = blackScreen.GetComponentInParent<Canvas>();
        if (blackScreenCanvas != null)
        {
            blackScreenCanvas.sortingOrder = 99;
        }
        
        yield return StartCoroutine(FadeCanvasGroup(blackScreenCanvasGroup, 0f, 1f, fadeInDuration));
        
        if (audioFadeCoroutine != null)
        {
            yield return audioFadeCoroutine;
            Debug.Log("Fade out audio selesai");
        }
        
        PlaySFX(transitionSFX);
        
        yield return new WaitForSeconds(dialogueDelay);
        
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
        
        float elapsedTime = fadeInDuration + dialogueDelay + dialogueDuration;
        if (elapsedTime < blackScreenDuration)
        {
            float remainingTime = blackScreenDuration - elapsedTime;
            Debug.Log($"Menunggu {remainingTime} detik sebelum memulai cutscene...");
            yield return new WaitForSeconds(remainingTime);
        }
        
        if (dialogueCanvas != null)
        {
            dialogueCanvas.sortingOrder = originalDialogueSortOrder;
            Debug.Log($"Dialogue canvas sort order dikembalikan: 100 -> {originalDialogueSortOrder}");
        }
        
        Debug.Log("Transition selesai, siap untuk cutscene!");
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