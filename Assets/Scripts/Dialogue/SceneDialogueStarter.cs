using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneDialogueStarter : MonoBehaviour
{
    public Dialogue dialogue;
    public float delay = 0.1f;
    private static bool hasPlayed = false;

    private void Start()
    {
        Invoke(nameof(StartDialogue), delay);
        if (hasPlayed) return;

        hasPlayed = true;
        Invoke(nameof(StartDialogue), delay);
    }

    void StartDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
        }
    }
}
