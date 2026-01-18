using UnityEngine;

public class InstructionChanger : MonoBehaviour
{
    [Header("Instruction Objects")]
    [Tooltip("GameObject instruksi yang akan dinonaktifkan saat interaksi")]
    public GameObject instructionToDisable;  // Kolom 1 di Inspector
    
    [Tooltip("GameObject instruksi yang akan diaktifkan saat interaksi")]
    public GameObject instructionToEnable;   // Kolom 2 di Inspector

    // Method ini dipanggil ketika object di-interact
    public void ChangeInstruction()
    {
        if (instructionToDisable != null)
        {
            instructionToDisable.SetActive(false);
        }
        
        if (instructionToEnable != null)
        {
            instructionToEnable.SetActive(true);
        }
        
        Debug.Log("Instruction changed!");
    }
}