using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueJumpscare : MonoBehaviour
{
    public enum InteractionType
    {
        PressE,
        AutoTrigger,
        PressEOnce
    }
    
    [Header("Interaction Settings")]
    [SerializeField] private InteractionType interactionType = InteractionType.PressE;
    [SerializeField] private bool canRetrigger = false; // Bisa dipicu ulang setelah selesai
    
    [Header("Buat Press E Aja")]
    [SerializeField] private GameObject eventDetect;

    [Header("Dialogue Settings")]
    public Dialogue dialogue;

    [Header("Jumpscare Settings")]
    [SerializeField] private Image jumpscareImage; // UI Image untuk jumpscare
    [SerializeField] private AudioSource jumpscareAudioSource; // AudioSource untuk jumpscare
    [SerializeField] private float jumpscareDuration = 2f; // Durasi jumpscare (detik)
    
    [Header("Jumpscare Visual")]
    [SerializeField] private float flashInterval = 0.1f; // Interval kedipan lebih cepat
    [SerializeField] private Color flashColor = Color.white; // Warna putih lebih terlihat
    
    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool isPlayingJumpscare = false; // Sedang memainkan jumpscare
    private GameObject playerObject; // Simpan reference ke player
    private Color originalImageColor;
    
    // Untuk menyimpan komponen yang dinonaktifkan
    private List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();
    private List<bool> originalComponentStates = new List<bool>();

    private void Start()
    {
        // Setup visual
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }

        // Setup jumpscare image
        if (jumpscareImage != null)
        {
            jumpscareImage.gameObject.SetActive(false);
            originalImageColor = jumpscareImage.color;
        }

        // Setup audio source (jika belum diassign)
        if (jumpscareAudioSource == null)
        {
            // Coba ambil dari GameObject ini
            jumpscareAudioSource = GetComponent<AudioSource>();
            
            // Jika masih null, cari di child objects
            if (jumpscareAudioSource == null)
            {
                jumpscareAudioSource = GetComponentInChildren<AudioSource>();
            }
            
            // Jika masih null, buat baru
            if (jumpscareAudioSource == null)
            {
                GameObject audioObject = new GameObject("JumpscareAudio");
                audioObject.transform.parent = transform;
                audioObject.transform.localPosition = Vector3.zero;
                jumpscareAudioSource = audioObject.AddComponent<AudioSource>();
                jumpscareAudioSource.playOnAwake = false;
            }
        }

        Debug.Log($"DialogueJumpscare initialized. Jumpscare Image: {jumpscareImage != null}, AudioSource: {jumpscareAudioSource != null}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = true;
        playerObject = collision.gameObject;
        
        Debug.Log($"Player entered trigger zone. HasTriggered: {hasTriggered}, CanRetrigger: {canRetrigger}");

        // PressE
        if (interactionType == InteractionType.PressE || interactionType == InteractionType.PressEOnce)
        {
            if (eventDetect != null)
            {
                // Tampilkan eventDetect hanya jika belum dipicu atau bisa dipicu ulang
                if (!hasTriggered || canRetrigger)
                {
                    eventDetect.SetActive(true);
                    Debug.Log("Event Detect activated");
                }
            }
        }

        // AutoTrigger
        if (interactionType == InteractionType.AutoTrigger)
        {
            // Cek apakah bisa dipicu
            if (!hasTriggered || canRetrigger)
            {
                Debug.Log("AutoTrigger activated");
                StartCoroutine(TriggerJumpscareThenDialogue());
            }
            else
            {
                Debug.Log("AutoTrigger skipped - already triggered and cannot retrigger");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = false;
        playerObject = null;
        
        Debug.Log("Player exited trigger zone");

        // PressE - sembunyikan eventDetect
        if ((interactionType == InteractionType.PressE || interactionType == InteractionType.PressEOnce) && eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
    }

    private void Update()
    {
        // PressE interaction
        if ((interactionType == InteractionType.PressE || interactionType == InteractionType.PressEOnce) && 
            playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Cek apakah bisa dipicu
            if (!hasTriggered || (interactionType == InteractionType.PressE && canRetrigger))
            {
                Debug.Log("E key pressed, triggering jumpscare");
                StartCoroutine(TriggerJumpscareThenDialogue());
            }
            else if (hasTriggered && interactionType == InteractionType.PressEOnce)
            {
                Debug.Log("Already triggered with PressEOnce type");
            }
        }

        // Test dengan tombol T (untuk debugging)
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestJumpscare();
        }
    }

    private IEnumerator TriggerJumpscareThenDialogue()
    {
        // Jika sedang memainkan jumpscare, jangan mulai lagi
        if (isPlayingJumpscare)
        {
            Debug.Log("Jumpscare already playing, skipping");
            yield break;
        }
        
        // Jika menggunakan PressEOnce dan sudah dipicu, skip
        if (interactionType == InteractionType.PressEOnce && hasTriggered && !canRetrigger)
        {
            Debug.Log("PressEOnce already triggered, skipping");
            yield break;
        }
        
        isPlayingJumpscare = true;
        hasTriggered = true;
        Debug.Log("Starting jumpscare sequence");
        
        // Nonaktifkan event detect jika ada
        if (eventDetect != null)
        {
            eventDetect.SetActive(false);
        }

        // Nonaktifkan player movement sementara jika ada
        DisablePlayerMovement();

        // Tampilkan jumpscare
        yield return StartCoroutine(ShowJumpscare());

        // Tunggu sebentar sebelum menampilkan dialog
        yield return new WaitForSeconds(0.3f);

        // Aktifkan kembali player movement
        EnablePlayerMovement();

        // Tampilkan dialog
        Debug.Log("Triggering dialogue");
        TriggerDialogue();
        
        // Reset isPlayingJumpscare setelah selesai
        isPlayingJumpscare = false;
        
        // Jika bisa dipicu ulang, reset hasTriggered setelah dialog selesai
        if (canRetrigger)
        {
            StartCoroutine(ResetTriggerAfterDialogue());
        }
    }

    private IEnumerator ResetTriggerAfterDialogue()
    {
        // Tunggu sampai dialog selesai
        // Anda bisa menyesuaikan waktu ini
        float waitTime = 5f; // Default 5 detik
        
        // Jika DialogueManager memiliki method untuk cek status
        if (DialogueManager.Instance != null)
        {
            // Tunggu sampai dialog selesai
            while (IsDialogueActive())
            {
                yield return null;
            }
        }
        else
        {
            // Fallback: tunggu waktu tertentu
            yield return new WaitForSeconds(waitTime);
        }
        
        // Reset trigger
        hasTriggered = false;
        Debug.Log("Trigger reset - can be triggered again");
        
        // Tampilkan eventDetect jika player masih di range
        if (playerInRange && eventDetect != null && 
            (interactionType == InteractionType.PressE || interactionType == InteractionType.PressEOnce))
        {
            eventDetect.SetActive(true);
        }
    }

    // Method untuk mengecek apakah dialog masih aktif
    private bool IsDialogueActive()
    {
        // Implementasi sesuai dengan DialogueManager Anda
        // Contoh sederhana:
        if (DialogueManager.Instance == null) return false;
        
        // Jika DialogueManager memiliki property/method untuk mengecek
        // return DialogueManager.Instance.IsDialogueActive;
        
        // Untuk sekarang, return false
        return false;
    }

    private IEnumerator ShowJumpscare()
    {
        Debug.Log("Showing jumpscare");

        // Pastikan ada jumpscare image
        if (jumpscareImage == null)
        {
            Debug.LogError("Jumpscare Image is not assigned!");
            yield break;
        }

        // Aktifkan gambar jumpscare
        jumpscareImage.gameObject.SetActive(true);
        Debug.Log("Jumpscare image activated");

        // Set warna awal ke transparan
        jumpscareImage.color = Color.clear;

        // Mainkan suara jumpscare dari AudioSource
        if (jumpscareAudioSource != null)
        {
            // Jika audio source sudah diputar, stop dulu
            if (jumpscareAudioSource.isPlaying)
            {
                jumpscareAudioSource.Stop();
            }
            
            // Mainkan suara
            jumpscareAudioSource.Play();
            Debug.Log("Playing jumpscare sound from AudioSource");
        }
        else
        {
            Debug.LogWarning("Jumpscare AudioSource is not assigned!");
        }

        // Hitung waktu mulai
        float elapsedTime = 0f;
        bool isFlashing = true;

        // Efek berkedip selama durasi jumpscare
        while (elapsedTime < jumpscareDuration)
        {
            // Toggle warna untuk efek berkedip
            if (isFlashing)
            {
                jumpscareImage.color = flashColor;
            }
            else
            {
                jumpscareImage.color = Color.clear;
            }
            
            isFlashing = !isFlashing;

            // Tunggu interval kedipan
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }

        // Matikan gambar jumpscare
        jumpscareImage.gameObject.SetActive(false);
        jumpscareImage.color = originalImageColor;
        
        // Stop suara jika masih diputar
        if (jumpscareAudioSource != null && jumpscareAudioSource.isPlaying)
        {
            jumpscareAudioSource.Stop();
        }
        
        Debug.Log("Jumpscare finished");
    }

    private void DisablePlayerMovement()
    {
        if (playerObject == null) 
        {
            Debug.LogWarning("Player object is null, cannot disable movement");
            return;
        }

        // Reset lists
        disabledComponents.Clear();
        originalComponentStates.Clear();

        // Cari komponen movement yang spesifik
        MonoBehaviour[] components = playerObject.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            string componentName = component.GetType().Name.ToLower();
            
            // Nonaktifkan hanya komponen movement tertentu
            if (componentName.Contains("movement") || 
                componentName.Contains("controller") ||
                componentName.Contains("playerinput") ||
                componentName.Contains("inputhandler") ||
                componentName == "playermovement" ||
                componentName.Contains("walk") ||
                componentName.Contains("run"))
            {
                if (component.enabled)
                {
                    // Simpan state asli
                    originalComponentStates.Add(true);
                    disabledComponents.Add(component);
                    
                    // Nonaktifkan
                    component.enabled = false;
                    Debug.Log($"Disabled: {component.GetType().Name}");
                }
            }
        }

        // Juga nonaktifkan Rigidbody2D velocity jika ada
        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            // Jangan nonaktifkan simulated karena bisa menyebabkan masalah
            Debug.Log("Stopped Rigidbody2D velocity");
        }

        Debug.Log($"Disabled {disabledComponents.Count} movement components");
    }

    private void EnablePlayerMovement()
    {
        if (playerObject == null) 
        {
            Debug.LogWarning("Player object is null, cannot enable movement");
            return;
        }

        // Aktifkan kembali komponen yang dinonaktifkan
        for (int i = 0; i < disabledComponents.Count; i++)
        {
            var component = disabledComponents[i];
            if (component != null)
            {
                component.enabled = true;
                Debug.Log($"Enabled: {component.GetType().Name}");
            }
        }
        
        // Clear lists
        disabledComponents.Clear();
        originalComponentStates.Clear();
        
        Debug.Log("Player movement restored");
    }

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is NULL!");
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogue);
    }

    // Untuk debugging: manual trigger jumpscare
    public void TestJumpscare()
    {
        if (!isPlayingJumpscare)
        {
            StartCoroutine(TriggerJumpscareThenDialogue());
        }
        else
        {
            Debug.Log("Jumpscare already playing");
        }
    }
    
    // Method untuk reset trigger manual
    public void ResetTrigger()
    {
        hasTriggered = false;
        isPlayingJumpscare = false;
        
        // Pastikan movement di-enable kembali
        EnablePlayerMovement();
        
        Debug.Log("Trigger manually reset");
    }
    
    // Method untuk mengecek status
    public bool CanTrigger()
    {
        return !hasTriggered || canRetrigger;
    }
    
    // Method untuk force enable movement (emergency)
    public void ForceEnableMovement()
    {
        EnablePlayerMovement();
        Debug.Log("Force enabled player movement");
    }
    
    // Method untuk mengatur AudioSource
    public void SetJumpscareAudioSource(AudioSource newAudioSource)
    {
        jumpscareAudioSource = newAudioSource;
        if (jumpscareAudioSource != null)
        {
            jumpscareAudioSource.playOnAwake = false;
            Debug.Log("Jumpscare AudioSource set");
        }
    }
    
    // Method untuk debug informasi player
    public void DebugPlayerComponents()
    {
        if (playerObject != null)
        {
            MonoBehaviour[] components = playerObject.GetComponents<MonoBehaviour>();
            Debug.Log($"=== Player Components ({components.Length}) ===");
            foreach (var component in components)
            {
                Debug.Log($"- {component.GetType().Name} (Enabled: {component.enabled})");
            }
        }
        else
        {
            Debug.Log("Player object is null");
        }
    }
}