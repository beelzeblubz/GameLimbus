using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float x = 0f;
    private float y = 0f;
    private bool moving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Animate();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        // Cek apakah dialogue aktif ATAU popup aktif ATAU transition aktif
        bool shouldBlockMovement = false;
        
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
        
        // CEK TRANSITION AKTIF DARI LIFT
        if (DialogueLift.IsAnyTransitionActive) // GANTI DARI IsAnyPopupActive
        {
            shouldBlockMovement = true;
        }

        if (shouldBlockMovement)
        {
            movement = Vector2.zero;
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

        if (moving)
        {
            animator.SetFloat("X", x);
            animator.SetFloat("Y", y);
        }

        animator.SetBool("Moving", moving);
    }
}