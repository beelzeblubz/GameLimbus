using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // Tambahkan ini untuk VideoPlayer

public class EscapeDialogueTriggered : MonoBehaviour
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
    [SerializeField] private Dialogue afterPuzzleDialogue;  // Dialog setelah close puzzle
    [SerializeField] private Dialogue transitionDialogue;

    [Header("Puzzle Settings")]
    // [SerializeField] private GameObject mejaPuzzle;
    [SerializeField] private string requiredPuzzleName = "Escape Puzzle";
    
    [Header("Transition Settings")]
    [SerializeField] private GameObject blackScreen;
    [SerializeField] private GameObject fogMerah;
    [SerializeField] private GameObject toogleInventory;
    [SerializeField] private GameObject tooglePause;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackScreenDuration = 3f;
    [SerializeField] private float dialogueDelay = 1f;
    [SerializeField] private float dialogueDuration = 1f;
    [SerializeField] private float sceneLoadDelay = 0.5f;
    
    [SerializeField] private string nextSceneName = "MainMenu";

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
    [SerializeField] private GameObject instruction;
    [SerializeField] private GameObject instructionToDisable;
    [SerializeField] private GameObject instructionToDisable2;
    [SerializeField] private GameObject instructionToEnable;

    [Header("Object Activation Settings")]
    [SerializeField] private GameObject objectToActivate;

    [Header("Inventory Settings")]
    [SerializeField] private bool usePuzzleInventory = true;
    [SerializeField] private float inventoryCloseDelay = 1f;
    
    // Status game
    private bool playerInRange = false;
    private bool hasTriggeredDialogue = false;
    private bool hasOpenedInventory = false;
    private bool isPuzzleSolved = false;
    private bool isWaitingForInventoryClose = false;  // Menunggu player close inventory
    private Coroutine transitionCoroutine;
    private Coroutine interactionCoroutine;
    private Coroutine cutsceneCoroutine;
    private Coroutine waitingCoroutine;
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
        
        // Pastikan object nonaktif di awal jika tidak null
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
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

    private void Update()
    {
        // Cek jika puzzle sudah diselesaikan
        if (!isPuzzleSolved && PuzzleInventory.Instance != null && PuzzleInventory.Instance.IsPuzzleSolved())
        {
            OnPuzzleSolved();
        }
        
        // Cek jika sedang menunggu inventory close dan inventory sudah ditutup
        if (isWaitingForInventoryClose && PuzzleInventory.Instance != null && !PuzzleInventory.Instance.IsInventoryOpen)
        {
            OnInventoryClosed();
        }
        
        if (interactionType == InteractionType.PressE && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Jangan handle interaction jika sedang menunggu inventory close
            if (!isWaitingForInventoryClose)
            {
                HandleInteraction();
            }
        }
        
        // Skip cutscene dengan tombol Escape atau Space
        if (cutsceneObject != null && cutsceneObject.activeSelf && 
            (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipCutscene();
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

        if (interactionType == InteractionType.AutoTrigger && !hasTriggeredDialogue)
        {
            hasTriggeredDialogue = true;
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

    private void HandleInteraction()
    {
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
        }
        
        interactionCoroutine = StartCoroutine(EscapeSequence());
    }

    private IEnumerator EscapeSequence()
    {
        Debug.Log("Memulai EscapeSequence");
        
        if (!hasTriggeredDialogue)
        {
            // ========== INTERAKSI PERTAMA KALI ==========
            // Tampilkan dialog
            Debug.Log("STEP 1: Menampilkan dialog...");
            if (dialogue != null && DialogueManager.Instance != null)
            {
                HandleInstructions(true, true);
                yield return StartCoroutine(ShowDialogueAndWait(dialogue));
            }
            else
            {
                ShowSimpleMessage($"Akses dibutuhkan! Selesaikan {requiredPuzzleName} untuk melanjutkan.");
                yield return new WaitForSeconds(2f);
            }
            
            hasTriggeredDialogue = true;
            
            // ========== UBAH INSTRUCTION ==========
            ChangeInstruction();
            
            // ========== AKTIFKAN object ==========
            ActivateObject();
            
            Debug.Log($"Dialog selesai. Objective diubah, object diaktifkan");
            
            // ========== BUKA PUZZLE INVENTORY ==========
            yield return new WaitForSeconds(0.001f);
            OpenPuzzleInventory();
        }
        else
        {
            // ========== INTERAKSI BERIKUTNYA ==========
            if (isPuzzleSolved)
            {
                // Puzzle sudah diselesaikan
                Debug.Log("Puzzle sudah diselesaikan. Buka inventory untuk review...");
                
                // Buka puzzle inventory (untuk review)
                OpenPuzzleInventory();
                
                // Mulai menunggu player close inventory
                StartWaitingForInventoryClose();
            }
            else
            {
                // Puzzle belum diselesaikan, buka inventory lagi
                OpenPuzzleInventory();
            }
        }
        
        interactionCoroutine = null;
    }

    private void OnPuzzleSolved()
    {
        if (isPuzzleSolved) return;
        
        isPuzzleSolved = true;
        Debug.Log("Puzzle berhasil diselesaikan!");
        
        // Tampilkan dialog sukses
        StartCoroutine(ShowSuccessDialogue());
    }

    private IEnumerator ShowSuccessDialogue()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (successDialogue != null && DialogueManager.Instance != null)
        {
            yield return StartCoroutine(ShowDialogueAndWait(successDialogue));
        }
        else
        {
            ShowSimpleMessage($"Selamat! {requiredPuzzleName} berhasil diselesaikan!");
            yield return new WaitForSeconds(2f);
        }
        
        // Update visuals
        UpdateVisuals();
        
        Debug.Log("Puzzle selesai, siap untuk pergi ke scene berikutnya");
    }

    private void StartWaitingForInventoryClose()
    {
        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
        }
        
        isWaitingForInventoryClose = true;
        Debug.Log("Menunggu player menutup inventory...");
    }

    private void OnInventoryClosed()
    {
        instruction.SetActive(false);

        if (!isWaitingForInventoryClose) return;
    
        isWaitingForInventoryClose = false;
        Debug.Log("Inventory ditutup, memulai transisi...");
        
        // Lanjut ke transisi dengan cutscene
        StartCoroutine(StartTransitionWithCutscene());
    }

    private IEnumerator StartTransitionWithCutscene()
    {
        Debug.Log("Memulai transisi dengan cutscene...");
        
        // Mulai transition (black screen, fade audio, dll)

        tooglePause.SetActive(false);
        toogleInventory.SetActive(false);
        yield return StartCoroutine(StartTransition());
        fogMerah.SetActive(false);
        
        // Play cutscene (jika diaktifkan)
        if (playCutscene && cutsceneObject != null)
        {
            Debug.Log("Memainkan cutscene...");
            cutsceneCoroutine = StartCoroutine(PlayCutscene());
            yield return cutsceneCoroutine;
        }
        
        // Load scene setelah cutscene selesai
        StartCoroutine(LoadNextScene());
    }

    // ========== CUTSCENE SYSTEM ==========
    private IEnumerator PlayCutscene()
    {
        Debug.Log("Memulai cutscene...");
        
        // Tunggu sebentar sebelum memulai cutscene
        yield return new WaitForSeconds(cutsceneDelay);
        
        // Nonaktifkan black screen jika diatur
        if (disableBlackScreenDuringCutscene && blackScreen != null)
        {
            blackScreen.SetActive(false);
            Debug.Log("Black screen dinonaktifkan selama cutscene");
        }
        
        // Aktifkan cutscene object
        cutsceneObject.SetActive(true);
        
        // Setup VideoPlayer jika ada
        if (videoPlayer != null)
        {
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
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(false);
        }
        
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

    // ========== METHOD UNTUK MENGAKTIFKAN object ==========
    private void ActivateObject()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log($"Object diaktifkan: {objectToActivate.name}");
        }
        else
        {
            Debug.LogWarning("objectToActivate GameObject null - tidak ada object yang diaktifkan");
        }
    }

    private void OpenPuzzleInventory()
    {
        Debug.Log("Membuka Puzzle Inventory...");
        // mejaPuzzle.SetActive(true);
        
        if (PuzzleInventory.Instance != null)
        {
            PuzzleInventory.Instance.OpenInventory();
            hasOpenedInventory = true;
        }
        else
        {
            Debug.LogError("PuzzleInventory.Instance tidak ditemukan!");
            
            // Coba cari secara manual
            PuzzleInventory puzzleInventory = FindObjectOfType<PuzzleInventory>();
            if (puzzleInventory != null)
            {
                puzzleInventory.OpenInventory();
                hasOpenedInventory = true;
            }
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
        // Anda bisa menambahkan visual feedback di sini jika diperlukan
        // Misalnya: mengubah warna indicator, dll.
    }

    private void HandleInstructions(bool disableFirst = true, bool disableSecond = true)
    {
        if (disableFirst && instructionToDisable != null)
        {
            instructionToDisable.SetActive(false);
            Debug.Log($"Instruction dimatikan: {instructionToDisable.name}");
        }
        
        if (disableSecond && instructionToDisable2 != null)
        {
            instructionToDisable2.SetActive(false);
            Debug.Log($"Instruction 2 dimatikan: {instructionToDisable2.name}");
        }
        
        if (instructionToEnable != null)
        {
            instructionToEnable.SetActive(true);
            Debug.Log($"Instruction dihidupkan: {instructionToEnable.name}");
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