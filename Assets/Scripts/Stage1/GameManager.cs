using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    // Status game global
    public bool HasKeycard { get; private set; } = false;
    public bool IsLiftUnlocked { get; private set; } = false;
    public string KeycardName { get; private set; } = "Security Card";
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Tetap ada di semua scene
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetKeycard(bool hasKeycard, string keycardName = "Security Card")
    {
        HasKeycard = hasKeycard;
        KeycardName = keycardName;
        
        Debug.Log($"Keycard status: {HasKeycard}, Name: {KeycardName}");
    }
    
    public void SetLiftUnlocked(bool isUnlocked)
    {
        IsLiftUnlocked = isUnlocked;
        Debug.Log($"Lift status: {(isUnlocked ? "Unlocked" : "Locked")}");
    }
    
    public void ResetAll()
    {
        HasKeycard = false;
        IsLiftUnlocked = false;
        KeycardName = "Security Card";
        
        Debug.Log("All game data reset");
    }
    
    // Method untuk testing dari Inspector
    public void DebugGameStatus()
    {
        Debug.Log($"Game Status - Keycard: {HasKeycard}, Lift: {(IsLiftUnlocked ? "Unlocked" : "Locked")}");
    }
}