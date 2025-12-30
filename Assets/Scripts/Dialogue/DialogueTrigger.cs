using System.Collections.Generic;
using UnityEngine;

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

    [Header("Interaction Settings")]
    [SerializeField] private InteractionType interactionType;

    [Header("Buat Press E Aja")]
    [SerializeField] private GameObject eventDetect;

    public Dialogue dialogue;

    private bool playerInRange = false;
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = true;
        Debug.Log("Player masuk area interaksi");

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
        Debug.Log("Player keluar area interaksi");

        // PressE
        if (interactionType == InteractionType.PressE && eventDetect != null)
        {
            eventDetect.SetActive(false);
        }
    }

    private void Update()
    {
        if (interactionType == InteractionType.PressE &&
            playerInRange &&
            Input.GetKeyDown(KeyCode.E))
        {
            TriggerDialogue();
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
}
