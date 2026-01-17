using UnityEngine;

public class ScaleProtector : MonoBehaviour
{
    private Vector3 lastValidScale = Vector3.one;
    
    void Awake()
    {
        lastValidScale = transform.localScale;
        
        // Fix jika scale 0
        if (lastValidScale == Vector3.zero || 
            lastValidScale.magnitude < 0.1f)
        {
            lastValidScale = Vector3.one;
            transform.localScale = lastValidScale;
        }
    }
    
    void Update()
    {
        // Monitor scale
        Vector3 currentScale = transform.localScale;
        
        // Jika scale menjadi invalid, fix
        if (currentScale == Vector3.zero || 
            float.IsNaN(currentScale.x) || 
            currentScale.magnitude < 0.1f)
        {
            transform.localScale = lastValidScale;
        }
        else
        {
            lastValidScale = currentScale;
        }
    }
    
    public void SetProtectedScale(Vector3 newScale)
    {
        if (newScale == Vector3.zero || newScale.magnitude < 0.1f)
        {
            newScale = Vector3.one;
        }
        
        lastValidScale = newScale;
        transform.localScale = newScale;
    }
    
    public Vector3 GetProtectedScale()
    {
        return lastValidScale;
    }
    
    [ContextMenu("Force Fix Scale")]
    public void ForceFixScale()
    {
        if (transform.localScale == Vector3.zero)
        {
            transform.localScale = Vector3.one;
            lastValidScale = Vector3.one;
        }
        else
        {
            lastValidScale = transform.localScale;
        }
    }
}