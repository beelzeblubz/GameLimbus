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
    public float typingSpeed = 0.2f;
    // public Animator animator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        lines = new Queue<DialogueLine>();
    }

    private void Update()
    {
        if (!isDialoguesActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DisplayNextDialogueLine();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (isDialoguesActive) return;
        
        isDialoguesActive = true;
        dialogueBox.SetActive(true);
        // animator.Play("show");
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

        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void EndDialogue()
    {
        isDialoguesActive = false;
        dialogueBox.SetActive(false);

        dialogueArea.text = "";
        characterName.text = "";
        // animator.Play("hide");
    }
}
