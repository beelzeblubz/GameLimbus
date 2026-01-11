using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite charIcon;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

public class DialogueTrigger : MonoBehaviour
{
    public enum InteractionType
    {
        PressE,
        AutoTrigger
    }
    
    public enum ItemType
    {
        Normal,         // Hanya dialogue biasa
        KeycardPickup,  // Meja dengan keycard
        Lift            // Lift yang butuh keycard
    }

    [Header("Interaction Settings")]
    [SerializeField] private InteractionType interactionType = InteractionType.PressE;
    [SerializeField] private ItemType itemType = ItemType.Normal;
    
    [Header("Buat Press E Aja")]
    [SerializeField] private GameObject eventDetect;

    [Header("Dialogue Settings")]
    public Dialogue dialogue;
    [SerializeField] private Dialogue successDialogue;  // Untuk setelah mendapatkan keycard
    [SerializeField] private Dialogue lockedDialogue;   // Untuk lift terkunci

    [Header("Keycard Settings")]
    [SerializeField] private string keycardName = "Security Card";
    [SerializeField] private string requiredKeycardName = "Security Card"; // Untuk lift

    [Header("Pickup Settings")]
    [SerializeField] private GameObject itemModel;      // Model item yang akan hilang saat diambil
    [SerializeField] private GameObject pickupPopup;    // UI popup saat dapat item
    [SerializeField] private float popupDuration = 2f;  // Durasi popup tampil

    [Header("Lift Settings")]
    [SerializeField] private GameObject lockedIndicator; // Tanda lift terkunci
    [SerializeField] private GameObject unlockedIndicator; // Tanda lift terbuka
    [SerializeField] private string nextSceneName = "Stage2"; // Scene yang akan diload
    [SerializeField] private float sceneLoadDelay = 1f; // Delay sebelum load scene

    // Status game
    private bool playerInRange = false;
    private bool hasTriggered = false;
    private bool popupActive = false;
    private bool isLiftLocked = true;
    private Coroutine popupCoroutine;

    // GameManager reference
    private GameManager gameManager;

    private void Start()
    {
        // Cari GameManager di scene
        gameManager = GameManager.Instance;
        
        // Jika ada GameManager, sinkronkan status keycard
        if (gameManager != null)
        {
            // Jika ini adalah keycard pickup, update status berdasarkan GameManager
            if (itemType == ItemType.KeycardPickup)
            {
                // Jika sudah punya keycard, sembunyikan model
                if (gameManager.HasKeycard && itemModel != null)
                {
                    itemModel.SetActive(false);
                    // Nonaktifkan interaksi juga
                    GetComponent<Collider2D>().enabled = false;
                }
            }
            
            // Jika ini adalah lift, cek status unlock berdasarkan GameManager
            if (itemType == ItemType.Lift && gameManager.IsLiftUnlocked)
            {
                isLiftLocked = false;
                UpdateVisuals();
            }
        }
        
        // Setup visual sesuai tipe
        UpdateVisuals();
        
        // Nonaktifkan popup di awal
        if (pickupPopup != null)
        {
            pickupPopup.SetActive(false);
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
            ClosePopup();
        }
    }

    private void HandleInteraction()
    {
        switch (itemType)
        {
            case ItemType.Normal:
                TriggerDialogue();
                break;
                
            case ItemType.KeycardPickup:
                // Cek apakah sudah punya keycard
                TriggerDialogue();
                
                bool alreadyHasKeycard = false;
                if (gameManager != null)
                {
                    alreadyHasKeycard = gameManager.HasKeycard;
                }
                
                if (!alreadyHasKeycard)
                {
                    GiveKeycard();
                }
                else
                {
                    TriggerDialogue();
                }
                break;
                
            case ItemType.Lift:
                TryOpenLift();
                break;
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

    private void GiveKeycard()
    {
        // Simpan status ke GameManager
        if (gameManager != null)
        {
            gameManager.SetKeycard(true, keycardName);
        }
        
        // Tampilkan popup
        ShowPickupPopup();
        
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
    }

    private void ShowPickupPopup()
    {
        if (pickupPopup != null)
        {
            // Aktifkan popup
            pickupPopup.SetActive(true);
            popupActive = true;
            
            // Mulai coroutine untuk auto close
            if (popupCoroutine != null)
                StopCoroutine(popupCoroutine);
            
            popupCoroutine = StartCoroutine(AutoClosePopup());
        }
        else
        {
            // Jika tidak ada popup, tampilkan dialogue biasa
            if (successDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(successDialogue);
            }
            else if (dialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogue);
            }
        }
    }

    private IEnumerator AutoClosePopup()
    {
        yield return new WaitForSeconds(popupDuration);
        ClosePopup();
    }

    private void ClosePopup()
    {
        if (pickupPopup != null)
        {
            pickupPopup.SetActive(false);
            popupActive = false;
        }
        
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }
    }

    private void TryOpenLift()
    {
        if (isLiftLocked)
        {
            // Cek apakah player punya keycard
            bool playerHasKeycard = false;
            if (gameManager != null)
            {
                playerHasKeycard = gameManager.HasKeycard;
            }
            
            if (playerHasKeycard)
            {
                // Unlock lift
                isLiftLocked = false;
                UpdateVisuals();
                
                // Simpan status unlock ke GameManager
                if (gameManager != null)
                {
                    gameManager.SetLiftUnlocked(true);
                }
                
                // Tampilkan dialogue sukses
                if (successDialogue != null && DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(successDialogue);
                }
                else
                {
                    // Buat dialogue sederhana
                    ShowSimpleMessage($"Lift terbuka! Menggunakan {requiredKeycardName}");
                }
                
                Debug.Log("Lift berhasil dibuka dengan keycard");
                
                // Load scene Stage2 setelah delay
                StartCoroutine(LoadNextScene());
            }
            else
            {
                // Tampilkan dialogue terkunci
                if (lockedDialogue != null && DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(lockedDialogue);
                }
                else
                {
                    // Buat dialogue sederhana
                    ShowSimpleMessage($"Lift terkunci. Butuh {requiredKeycardName} untuk membukanya.");
                }
                
                Debug.Log("Lift terkunci, butuh keycard");
            }
        }
        else
        {
            // Lift sudah terbuka, langsung load scene
            ShowSimpleMessage("Naik lift ke Stage 2...");
            StartCoroutine(LoadNextScene());
        }
    }

    private IEnumerator LoadNextScene()
    {
        // Tunggu sebentar sebelum load scene
        yield return new WaitForSeconds(sceneLoadDelay);
        
        // Load scene Stage2
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

    private void ShowSimpleMessage(string message)
    {
        // Buat dialogue sederhana tanpa ScriptableObject
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

    private void UpdateVisuals()
    {
        // Update indikator lift
        if (itemType == ItemType.Lift)
        {
            if (lockedIndicator != null)
                lockedIndicator.SetActive(isLiftLocked);
            
            if (unlockedIndicator != null)
                unlockedIndicator.SetActive(!isLiftLocked);
        }
    }

    // Method untuk debug/testing
    public void ForceUnlockLift()
    {
        if (itemType == ItemType.Lift)
        {
            isLiftLocked = false;
            UpdateVisuals();
            Debug.Log("Lift di-unlock paksa");
        }
    }
    
    public void GiveKeycardFromOtherScript()
    {
        if (itemType == ItemType.KeycardPickup)
        {
            GiveKeycard();
        }
    }
}