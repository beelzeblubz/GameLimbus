using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float x;
    private float y;
    private bool moving;


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
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        movement = new Vector2(x,y);
        movement.Normalize();
        rb.MovePosition (rb.position + movement * moveSpeed * Time.deltaTime);
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
