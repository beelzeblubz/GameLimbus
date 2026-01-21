using UnityEngine;

public class EnableDisableInstruction : MonoBehaviour
{
    [SerializeField] private GameObject instructionToDisable;
    [SerializeField] private GameObject instructionToEnable;
    [SerializeField] private GameObject distroy;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (instructionToDisable != null)
        {
            instructionToDisable.SetActive(false);
        }
        
        if (instructionToEnable != null)
        {
            instructionToEnable.SetActive(true);
            
        }

        Destroy(distroy);
    }
}