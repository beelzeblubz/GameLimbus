using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public GameObject dialogueBox;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    private Queue<DialogueLine> lines;

    public bool isDialoguesActive = false;
    public float typingSpeed = 0.03f;

    [Header("Typing SFX")]
    public AudioSource audioSource;
    public AudioClip typingSFX;
    [Range(0f, 1f)] public float typingVolume = 0.5f;
    public bool loopTypingSound = true;

    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine soundCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        lines = new Queue<DialogueLine>();
    }

    private void Update()
    {
        SpaceClick();
    }

    public void SpaceClick()
    {
        if (!isDialoguesActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)   // Jika masih typing
            {
                SkipTyping();
            }
            else    // Jika tidak sedang typing
            {
                DisplayNextDialogueLine();
            }
        }
    }

    public void ButtonClick()
    {
        if (!isDialoguesActive) return;

        if (isTyping)   // Jika masih typing
        {
            SkipTyping();
        }
        else    // Jika tidak sedang typing
        {
            DisplayNextDialogueLine();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (isDialoguesActive) return;

        isDialoguesActive = true;
        dialogueBox.SetActive(true);
        lines.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.charIcon;
        characterName.text = currentLine.character.name;

        // Hentikan semua coroutine yang sedang berjalan
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);

        // Mulai typing
        typingCoroutine = StartCoroutine(TypeSentence(currentLine.line));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueArea.text = sentence;
        dialogueArea.maxVisibleCharacters = 0;

        // Mulai audio typing
        StartTypingSound();

        // Type setiap karakter
        for (int i = 0; i < sentence.Length; i++)
        {
            dialogueArea.maxVisibleCharacters++;
            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    void StartTypingSound()
    {
        // Hentikan audio yang sedang berjalan
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        // Mulai audio typing jika ada AudioSource dan SFX
        if (audioSource != null && typingSFX != null)
        {
            // Atur audio source
            audioSource.clip = typingSFX;
            audioSource.volume = typingVolume;
            audioSource.loop = loopTypingSound;
            
            // Mulai memutar
            audioSource.Play();

            // Mulai coroutine untuk mengontrol audio
            soundCoroutine = StartCoroutine(MonitorTypingSound());
        }
    }

    IEnumerator MonitorTypingSound()
    {
        // Tetap mainkan audio selama typing berlangsung
        while (isTyping && audioSource != null)
        {
            // Jika audio berhenti tetapi masih typing, mulai ulang
            if (!audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
            yield return null; // Tunggu 1 frame
        }

        // Hentikan audio saat selesai typing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);

        dialogueArea.maxVisibleCharacters = dialogueArea.text.Length;
        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;

        // Hentikan audio typing
        if (audioSource && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
            soundCoroutine = null;
        }
    }

    void EndDialogue()
    {
        isDialoguesActive = false;
        
        // Hentikan semua audio
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        // Hentikan semua coroutine
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);

        // Reset state
        isTyping = false;
        dialogueBox.SetActive(false);
        dialogueArea.text = "";
        characterName.text = "";
    }

    // Optional: Method untuk mengontrol volume secara dinamis
    public void SetTypingVolume(float volume)
    {
        typingVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = typingVolume;
        }
    }

    // Optional: Method untuk pause/resume typing sound
    public void PauseTypingSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void ResumeTypingSound()
    {
        if (audioSource != null && !audioSource.isPlaying && isTyping)
        {
            audioSource.UnPause();
        }
    }
}