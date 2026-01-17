// LoadingScreenFadeOut.cs di scene Stage2
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenFadeOut : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private float fadeOutDuration = 1f;
    
    private CanvasGroup canvasGroup;
    
    void Start()
    {
        if (loadingScreen != null)
        {
            // Aktifkan loading screen di awal scene
            loadingScreen.SetActive(true);
            
            // Dapatkan CanvasGroup
            canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
            }
            
            // Set alpha ke 1 (full hitam)
            canvasGroup.alpha = 1f;
            
            // Mulai fade out
            StartCoroutine(FadeOut());
        }
    }
    
    private IEnumerator FadeOut()
    {
        Debug.Log("Memulai fade out loading screen...");
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        loadingScreen.SetActive(false);
        
        Debug.Log("Loading screen fade out selesai!");
    }
}