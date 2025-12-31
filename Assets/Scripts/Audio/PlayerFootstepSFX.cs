using UnityEngine;

public class PlayerFootstepSFX : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip footstepSFX;

    [Header("Footstep Settings")]
    public float stepInterval = 0.4f;

    [Header("Random Pitch Settings")]
    [Range(0.5f, 1.5f)]
    public float minPitch = 0.9f;

    [Range(0.5f, 1.5f)]
    public float maxPitch = 1.1f;

    private float stepTimer;

    void Update()
    {
        // Hentikan suara saat dialog aktif
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialoguesActive)
        {
            stepTimer = 0f;
            return;
        }

        bool isMoving = animator.GetBool("Moving");

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (audioSource != null && footstepSFX != null)
        {
            // 🎲 RANDOM PITCH
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(footstepSFX);
        }
    }
}
