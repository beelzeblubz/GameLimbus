using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolEnemy : MonoBehaviour
{
    [Header("Collider Settings")]
    [SerializeField] private BoxCollider2D detectionCollider;  // Area deteksi (trigger)
    [SerializeField] private CircleCollider2D bodyCollider;    // Tubuh enemy (solid)
    [SerializeField] private Collider2D physicalCollider;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float startWaitTime = 2f;
    [SerializeField] private float patrolDistanceThreshold = 0.2f;
    
    [Header("Chase Settings")]
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float chaseStopDistance = 1f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleCheckDistance = 1f;
    [SerializeField] private float obstacleAvoidForce = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float stuckCheckInterval = 0.5f;
    [SerializeField] private float stuckThreshold = 0.1f;
    
    [Header("Patrol Spots")]
    [SerializeField] private Transform[] moveSpots;
    [SerializeField] private bool useSequentialPatrol = true;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useAnimator = true;
    
    [Header("Chase Visual Effects")]
    [SerializeField] private GameObject[] activateOnChase;
    [SerializeField] private GameObject[] deactivateOnChase;
    [SerializeField] private bool enableChaseVisuals = true;
    
    private Transform player;
    private int currentSpot = 0;
    private float waitTime;
    private bool _isChasing = false;
    private bool isChasing
    {
        get { return _isChasing; }
        set
        {
            if (_isChasing != value)
            {
                _isChasing = value;
                if (enableChaseVisuals)
                    UpdateChaseVisuals(value);
            }
        }
    }
    private Vector2 lastKnownPlayerPosition;
    private Vector2 lastPosition;
    private float stuckTimer;
    private Rigidbody2D rb;
    private Vector2 avoidanceDirection;
    private bool isStuck = false;
    private float stuckTime;
    private bool movingForward = true;
    
    private Vector2 movement;
    private float x = 0f;
    private float y = 0f;
    private bool moving = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        if (useAnimator && animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        InitializeChaseVisuals();
        FindPlayer();
        
        if (moveSpots.Length > 0)
        {
            currentSpot = 0;
        }
        
        waitTime = startWaitTime;
        lastPosition = transform.position;
        // SetupCollider();

        // if (physicalCollider == null)
        // {
        //     Debug.LogError("Physical collider not assigned!");
        // }
        
        // // Nonaktifkan semua collider lain (jika ada)
        // Collider2D[] allColliders = GetComponents<Collider2D>();
        // foreach (Collider2D col in allColliders)
        // {
        //     if (col != physicalCollider)
        //     {
        //         col.enabled = false; // Nonaktifkan collider non-fisik
        //     }
        // }

        SetupDualColliders();
    }

    private void SetupDualColliders()
    {
        // 1. COLLIDER DETEKSI (TRIGGER) - untuk mulai chase
        if (detectionCollider != null)
        {
            detectionCollider.isTrigger = true;  // Trigger mode
            detectionCollider.enabled = true;
            
            // Atur size detection area lebih besar
            // detectionCollider.size = new Vector2(5f, 5f);
            Debug.Log($"Detection collider setup - Trigger: {detectionCollider.isTrigger}");
        }
        else
        {
            Debug.LogWarning("Detection collider not assigned!");
        }
        
        // 2. COLLIDER FISIK (SOLID) - untuk kill player
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;  // Solid mode
            bodyCollider.enabled = true;
            
            // Atur size tubuh enemy
            // bodyCollider.radius = 1f;
            Debug.Log($"Body collider setup - Trigger: {bodyCollider.isTrigger}");
        }
        else
        {
            Debug.LogWarning("Body collider not assigned!");
        }
    }
    
    private void InitializeChaseVisuals()
    {
        UpdateChaseVisuals(false);
    }
    
    private void UpdateChaseVisuals(bool chasing)
    {
        if (!enableChaseVisuals) return;
        
        foreach (GameObject obj in activateOnChase)
        {
            if (obj != null) obj.SetActive(chasing);
        }
        
        foreach (GameObject obj in deactivateOnChase)
        {
            if (obj != null) obj.SetActive(!chasing);
        }
    }
    
    void Update()
    {
        // ========== CEK APAKAH MOVEMENT DIBLOKIR ==========
        if (IsMovementBlocked())
        {
            // Hentikan semua movement
            rb.velocity = Vector2.zero;
            movement = Vector2.zero;
            x = 0f;
            y = 0f;
            moving = false;
            
            // Update animator ke state idle
            if (animator != null)
            {
                animator.SetBool("Moving", false);
            }
            
            return; // Skip semua logic movement
        }
        // ==================================================
        
        if (player == null) FindPlayer();
        
        CheckIfStuck();
        CalculateAvoidance();
        
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= followDistance)
            {
                if (!isChasing) isChasing = true;
                lastKnownPlayerPosition = player.position;
                ChasePlayer();
            }
            else
            {
                if (isChasing)
                {
                    if (Vector2.Distance(transform.position, lastKnownPlayerPosition) > 0.5f)
                    {
                        if (Vector2.Distance(transform.position, lastKnownPlayerPosition) <= 0.5f)
                        {
                            isChasing = false;
                        }
                    }
                    else
                    {
                        isChasing = false;
                    }
                }
                
                if (!isChasing) Patrol();
            }
        }
        else
        {
            if (!isChasing) Patrol();
        }
        
        Animate();
    }
    
    void FixedUpdate()
    {
        // ========== CEK APAKAH MOVEMENT DIBLOKIR ==========
        if (IsMovementBlocked())
        {
            rb.velocity = Vector2.zero;
            return; // Skip semua physics movement
        }
        // ==================================================
        
        ApplyMovement();
    }
    
    // ========== METHOD BARU: CEK APAKAH MOVEMENT DIBLOKIR ==========
    private bool IsMovementBlocked()
    {
        // 1. Cek apakah sedang ada dialogue aktif
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
        {
            return true;
        }
        
        // 2. Cek apakah inventory puzzle sedang dibuka
        if (PuzzleInventory.Instance != null && PuzzleInventory.Instance.IsInventoryOpen)
        {
            return true;
        }
        
        // 3. Cek apakah ada popup puzzle piece aktif
        if (JigsawPuzzleCollector.IsAnyPopupActive)
        {
            return true;
        }
        
        return false;
    }
    // ================================================================
    
    private void ApplyMovement()
    {
        if (isStuck)
        {
            Vector2 escapeDirection = GetEscapeDirection();
            rb.velocity = escapeDirection * speed * 0.5f;
            movement = escapeDirection;
            x = escapeDirection.x;
            y = escapeDirection.y;
            return;
        }
        
        Vector2 moveDirection = GetMovementDirection();
        
        if (moveDirection != Vector2.zero)
        {
            Vector2 finalDirection = (moveDirection + avoidanceDirection * 0.3f).normalized;
            rb.velocity = finalDirection * (isChasing ? chaseSpeed : speed);
            movement = finalDirection;
            x = finalDirection.x;
            y = finalDirection.y;
        }
        else
        {
            rb.velocity = Vector2.zero;
            movement = Vector2.zero;
            x = 0f;
            y = 0f;
        }
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
        {
            animator.SetBool("Moving", moving);
            animator.SetBool("IsChasing", isChasing);
        }
    }
    
    private Vector2 GetMovementDirection()
    {
        if (isChasing && player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= chaseStopDistance) return Vector2.zero;
            return direction;
        }
        else if (isChasing && player == null)
        {
            Vector2 direction = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, lastKnownPlayerPosition);
            if (distance <= 0.5f) return Vector2.zero;
            return direction;
        }
        else
        {
            if (moveSpots.Length == 0) return Vector2.zero;
            
            Vector2 direction = (moveSpots[currentSpot].position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, moveSpots[currentSpot].position);
            if (distance <= patrolDistanceThreshold) return Vector2.zero;
            return direction;
        }
    }
    
    private void CalculateAvoidance()
    {
        avoidanceDirection = Vector2.zero;
        
        Vector2[] rayDirections = new Vector2[]
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(0.7f, 0.7f).normalized,
            new Vector2(-0.7f, 0.7f).normalized,
            new Vector2(0.7f, -0.7f).normalized,
            new Vector2(-0.7f, -0.7f).normalized
        };
        
        foreach (Vector2 dir in rayDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, obstacleCheckDistance, obstacleLayer);
            if (hit.collider != null)
            {
                avoidanceDirection -= dir * (1f / hit.distance) * obstacleAvoidForce;
            }
        }
        
        avoidanceDirection = avoidanceDirection.normalized;
    }
    
    private void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;
        
        if (stuckTimer >= stuckCheckInterval)
        {
            float distanceMoved = Vector2.Distance(transform.position, lastPosition);
            
            if (distanceMoved < stuckThreshold && rb.velocity.magnitude > 0.1f)
            {
                stuckTime += stuckCheckInterval;
                
                if (stuckTime >= 1f)
                {
                    isStuck = true;
                    if (stuckTime >= 3f) EscapeStuck();
                }
            }
            else
            {
                stuckTime = 0f;
                isStuck = false;
            }
            
            lastPosition = transform.position;
            stuckTimer = 0f;
        }
    }
    
    private void EscapeStuck()
    {
        Vector2 escapePos = transform.position;
        
        Vector2[] escapeDirections = new Vector2[]
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1f, 1f).normalized, new Vector2(-1f, 1f).normalized,
            new Vector2(1f, -1f).normalized, new Vector2(-1f, -1f).normalized
        };
        
        foreach (Vector2 dir in escapeDirections)
        {
            Vector2 testPos = (Vector2)transform.position + dir * 0.5f;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(testPos, 0.3f, obstacleLayer);
            if (colliders.Length == 0)
            {
                escapePos = testPos;
                break;
            }
        }
        
        transform.position = escapePos;
        rb.velocity = Vector2.zero;
        isStuck = false;
        stuckTime = 0f;
    }
    
    private Vector2 GetEscapeDirection()
    {
        Vector2 bestDirection = Random.insideUnitCircle.normalized;
        int minHitCount = int.MaxValue;
        
        Vector2[] testDirections = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            testDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        
        foreach (Vector2 dir in testDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 2f, obstacleLayer);
            int hitCount = (hit.collider != null) ? 1 : 0;
            
            if (hitCount < minHitCount)
            {
                minHitCount = hitCount;
                bestDirection = dir;
            }
        }
        
        return bestDirection;
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }
    
    private void Patrol()
    {
        if (moveSpots.Length == 0) return;
        
        if (Vector2.Distance(transform.position, moveSpots[currentSpot].position) < patrolDistanceThreshold)
        {
            if (waitTime <= 0)
            {
                SelectNextSpot();
                waitTime = startWaitTime;
            }
            else
            {
                waitTime -= Time.deltaTime;
            }
        }
    }
    
    private void SelectNextSpot()
    {
        if (useSequentialPatrol)
        {
            currentSpot++;
            if (currentSpot >= moveSpots.Length) currentSpot = 0;
        }
        else
        {
            int newSpot;
            do
            {
                newSpot = Random.Range(0, moveSpots.Length);
            } 
            while (moveSpots.Length > 1 && newSpot == currentSpot);
            
            currentSpot = newSpot;
        }
    }
    
    public void SetSequentialPatrol(bool sequential)
    {
        useSequentialPatrol = sequential;
    }
    
    public void JumpToSpot(int spotIndex)
    {
        if (spotIndex >= 0 && spotIndex < moveSpots.Length)
        {
            currentSpot = spotIndex;
        }
    }
    
    public int GetCurrentSpot()
    {
        return currentSpot;
    }
    
    private void ChasePlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    }
    
    // private void SetupCollider()
    // {
    //     Collider2D collider = GetComponent<Collider2D>();
    //     if (collider == null)
    //     {
    //         BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
    //         boxCollider.size = new Vector2(0.8f, 0.8f);
    //     }
    // }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDirection * obstacleAvoidForce * 10f, ForceMode2D.Impulse);
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Vector2 tangent = Vector2.Perpendicular(collision.contacts[0].normal).normalized;
            rb.AddForce(tangent * obstacleAvoidForce * 5f * Time.deltaTime);
        }
    }
    
    public void DebugChaseVisuals()
    {
        Debug.Log($"=== Chase Visuals Debug ===");
        Debug.Log($"Is Chasing: {isChasing}");
        Debug.Log($"Enable Chase Visuals: {enableChaseVisuals}");
        
        Debug.Log("Objects to activate on chase:");
        foreach (GameObject obj in activateOnChase)
        {
            Debug.Log($"- {obj?.name}: Active = {obj?.activeSelf}");
        }
        
        Debug.Log("Objects to deactivate on chase:");
        foreach (GameObject obj in deactivateOnChase)
        {
            Debug.Log($"- {obj?.name}: Active = {obj?.activeSelf}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);
        
        Gizmos.color = Color.cyan;
        Vector2[] rayDirections = new Vector2[]
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(0.7f, 0.7f).normalized,
            new Vector2(-0.7f, 0.7f).normalized,
            new Vector2(0.7f, -0.7f).normalized,
            new Vector2(-0.7f, -0.7f).normalized
        };
        
        foreach (Vector2 dir in rayDirections)
        {
            Gizmos.DrawRay(transform.position, dir * obstacleCheckDistance);
        }
        
        if (moveSpots != null && moveSpots.Length > 0)
        {
            Gizmos.color = Color.blue;
            
            for (int i = 0; i < moveSpots.Length; i++)
            {
                if (moveSpots[i] != null)
                {
                    Gizmos.DrawWireSphere(moveSpots[i].position, 0.3f);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(moveSpots[i].position + Vector3.up * 0.4f, $"Spot {i}");
                    #endif
                    
                    if (i < moveSpots.Length - 1 && moveSpots[i + 1] != null)
                    {
                        Gizmos.DrawLine(moveSpots[i].position, moveSpots[i + 1].position);
                    }
                    
                    if (i == moveSpots.Length - 1 && moveSpots[0] != null)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(moveSpots[i].position, moveSpots[0].position);
                        Gizmos.color = Color.blue;
                    }
                }
            }
            
            if (Application.isPlaying && moveSpots[currentSpot] != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(moveSpots[currentSpot].position, 0.4f);
                Gizmos.DrawLine(transform.position, moveSpots[currentSpot].position);
            }
        }
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, avoidanceDirection);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, movement);
        }
    }
}