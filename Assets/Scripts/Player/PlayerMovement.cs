using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private float initialMovementLockDuration = 1f; // Durasi awal blocking movement

    private Rigidbody2D rb;
    private Vector2 movement;
    private float x = 0f;
    private float y = 0f;
    private bool moving = false;
    private float gameStartTime = 0f;
    private bool isInitialLockActive = true;
    
    // Static property untuk kontrol dari script lain
    public static bool IsMovementBlocked { get; set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        IsMovementBlocked = false;
        
        // Simpan waktu mulai game
        gameStartTime = Time.time;
        isInitialLockActive = true;
        
        Debug.Log($"Movement akan terkunci selama {initialMovementLockDuration} detik pertama");
    }

    void Update()
    {
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
        Move();
    }

    private void Move()
    {
        // Cek semua kondisi yang memblokir movement
        bool shouldBlockMovement = false;
        
        // Cek lock awal game (1 detik pertama)
        if (isInitialLockActive)
        {
            shouldBlockMovement = true;
        }
        
        // Cek static property
        if (IsMovementBlocked)
        {
            shouldBlockMovement = true;
        }
        
        // Cek dialogue aktif
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
        {
            shouldBlockMovement = true;
        }
        
        // Cek popup aktif dari Keycard
        if (DialogueKeycard.IsAnyPopupActive)
        {
            shouldBlockMovement = true;
        }
        
        // Cek transition aktif dari Lift
        if (DialogueLift.IsAnyTransitionActive)
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
    
    // Method untuk manual unlock movement awal (jika diperlukan)
    public void UnlockInitialMovement()
    {
        if (isInitialLockActive)
        {
            isInitialLockActive = false;
            Debug.Log("Initial movement lock dibuka secara manual");
        }
    }
    
    // Method untuk mengatur durasi lock awal
    public void SetInitialLockDuration(float duration)
    {
        initialMovementLockDuration = duration;
        gameStartTime = Time.time;
        isInitialLockActive = true;
        Debug.Log($"Initial movement lock diatur ke {duration} detik");
    }
}