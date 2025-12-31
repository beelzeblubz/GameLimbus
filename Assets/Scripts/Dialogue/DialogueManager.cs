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
    public int soundInterval = 3;

    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private int soundCounter = 0;

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

    public void ButtonClick()   // Tidak masuk void update, soalnya ngedetect button. 
                                // Kalau ditaruh di update bakal ngedetect terus walau ga di inetacr apa apa. 
                                // Jadi GameObject button tinggal kasih ButtonClick
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

        StopAllCoroutines();

        StartCoroutine(TypeSentence(currentLine.line));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueArea.text = sentence;
        dialogueArea.maxVisibleCharacters = 0;
        soundCounter = 0;

        for (int i = 0; i < sentence.Length; i++)
        {
            dialogueArea.maxVisibleCharacters++;

            char letter = sentence[i];
            soundCounter++;

            if (audioSource && typingSFX && letter != ' ' && soundCounter % soundInterval == 0)
            {
                audioSource.PlayOneShot(typingSFX);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueArea.maxVisibleCharacters = dialogueArea.text.Length;
        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;

        if (audioSource)
            audioSource.Stop();
    }

    void EndDialogue()
    {
        isDialoguesActive = false;
        dialogueBox.SetActive(false);

        dialogueArea.text = "";
        characterName.text = "";
    }
}
