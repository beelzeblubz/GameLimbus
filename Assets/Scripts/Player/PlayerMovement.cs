using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private float initialMovementLockDuration = 1f;

    [Header("Death Settings")]
    [SerializeField] private GameObject instruction;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private float deathSFXVolume = 0.8f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float x = 0f;
    private float y = 0f;
    private bool moving = false;
    private float gameStartTime = 0f;
    private bool isInitialLockActive = true;
    private bool isDead = false; // Status kematian player
    
    // Static property untuk kontrol dari script lain
    public static bool IsMovementBlocked { get; set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        IsMovementBlocked = false;
        isDead = false;
        
        gameStartTime = Time.time;
        isInitialLockActive = true;
        
        // Pastikan Game Over UI tidak aktif di awal
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
            instruction.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Game Over UI belum di-assign di Inspector!");
        }
        
        Debug.Log($"Movement akan terkunci selama {initialMovementLockDuration} detik pertama");
    }

    void Update()
    {
        // Skip semua update jika player sudah mati
        if (isDead) return;
        
        // Cek apakah masih dalam periode awal blocking
        if (isInitialLockActive && Time.time - gameStartTime >= initialMovementLockDuration)
        {
            isInitialLockActive = false;
            Debug.Log("Movement lock awal telah berakhir, player bisa bergerak");
        }
        
        Animate();
    }

    void FixedUpdate()
    {
        // Skip movement jika player sudah mati
        if (isDead) return;
        
        Move();
    }

    private void Move()
    {
        // Cek semua kondisi yang memblokir movement
        bool shouldBlockMovement = false;
        
        if (isInitialLockActive)
        {
            shouldBlockMovement = true;
        }
        
        if (IsMovementBlocked)
        {
            shouldBlockMovement = true;
        }
        
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
        {
            shouldBlockMovement = true;
        }
        
        // Tambahan: Cek PuzzleInventory jika ada
        if (PuzzleInventory.Instance != null && PuzzleInventory.Instance.IsInventoryOpen)
        {
            shouldBlockMovement = true;
        }
        
        // Tambahan: Cek Popup Puzzle Piece jika ada
        if (JigsawPuzzleCollector.IsAnyPopupActive)
        {
            shouldBlockMovement = true;
        }

        if (shouldBlockMovement)
        {
            movement = Vector2.zero;
            if (animator != null)
                animator.SetBool("Moving", false);
            return;
        }

        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        movement = new Vector2(x, y);
        movement.Normalize();
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);
    }

    private void Animate()
    {
        if (movement.magnitude > 0.1 || movement.magnitude < -0.1)
        {
            moving = true; 
        }
        else
        {
            moving = false;
        }

        if (moving && animator != null)
        {
            animator.SetFloat("X", x);
            animator.SetFloat("Y", y);
        }

        if (animator != null)
            animator.SetBool("Moving", moving);
    }

    // ========== COLLISION DETECTION UNTUK ENEMY ==========
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Cek apakah player menabrak object dengan tag "Enemy"
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (!isDead) // Hanya trigger jika player belum mati
            {
                Debug.Log($"Player terkena {collision.gameObject.name}!");
                Die();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Alternative: Jika menggunakan Trigger Collider
        if (collision.CompareTag("Enemy"))
        {
            if (!isDead)
            {
                Debug.Log($"Player terkena {collision.gameObject.name}!");
                Die();
            }
        }
    }
    // ======================================================

    // ========== DEATH SYSTEM ==========
    private void Die()
    {
        if (isDead) return; // Cegah double death
        
        isDead = true;
        Debug.Log("=== PLAYER MATI! ===");
        
        // Hentikan semua movement
        rb.velocity = Vector2.zero;
        movement = Vector2.zero;
        
        if (animator != null)
        {
            animator.SetBool("Moving", false);
        }
        
        // Play death SFX
        if (deathSFX != null)
        {
            PlaySFX(deathSFX, deathSFXVolume);
        }
        
        // Trigger Game Over setelah delay
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        // Tunggu sebentar (untuk animasi death atau SFX)
        yield return new WaitForSeconds(0f);
        
        // PAUSE GAME
        Time.timeScale = 0f;
        Debug.Log("Game di-pause (Time.timeScale = 0)");
        
        // Tampilkan Game Over UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            Debug.Log("Game Over UI ditampilkan");
        }
        else
        {
            Debug.LogError("Game Over UI NULL! Tidak bisa menampilkan Game Over screen!");
        }
    }
    // ==================================

    // ========== AUDIO METHOD ==========
    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        
        GameObject audioObject = new GameObject("TempAudio_Death");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.Play();
        
        Destroy(audioObject, clip.length + 0.1f);
    }
    // ==================================

    // ========== PUBLIC METHODS ==========
    public void UnlockInitialMovement()
    {
        if (isInitialLockActive)
        {
            isInitialLockActive = false;
            Debug.Log("Initial movement lock dibuka secara manual");
        }
    }
    
    public void SetInitialLockDuration(float duration)
    {
        initialMovementLockDuration = duration;
        gameStartTime = Time.time;
        isInitialLockActive = true;
        Debug.Log($"Initial movement lock diatur ke {duration} detik");
    }

    public bool IsDead()
    {
        return isDead;
    }

    // Method untuk restart/respawn (opsional, bisa dipanggil dari GameOverManager)
    public void Respawn()
    {
        isDead = false;
        Time.timeScale = 1f; // Resume game
        
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
        
        Debug.Log("Player respawn");
    }
    // ====================================
}