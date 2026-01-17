using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class ButtonHighlightScale : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float highlightedScale = 1.2f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Highlight Options")]
    [SerializeField] private bool useMouseHighlight = true;
    [SerializeField] private bool useKeyboardHighlight = true;
    [SerializeField] private bool resetOnDisable = true;
    
    private RectTransform rectTransform;
    private Vector3 targetScale;
    private Coroutine scaleCoroutine;
    private bool isHighlighted = false;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Set initial scale
        rectTransform.localScale = Vector3.one * normalScale;
        targetScale = Vector3.one * normalScale;
    }
    
    void OnEnable()
    {
        if (!isHighlighted)
        {
            rectTransform.localScale = Vector3.one * normalScale;
            targetScale = Vector3.one * normalScale;
        }
    }
    
    void OnDisable()
    {
        if (resetOnDisable)
        {
            rectTransform.localScale = Vector3.one * normalScale;
            targetScale = Vector3.one * normalScale;
            isHighlighted = false;
        }
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }
    
    // Mouse hover highlight
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useMouseHighlight) return;
        
        HighlightButton();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!useMouseHighlight) return;
        
        UnhighlightButton();
    }
    
    // Keyboard/controller selection highlight
    public void OnSelect(BaseEventData eventData)
    {
        if (!useKeyboardHighlight) return;
        
        HighlightButton();
    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        if (!useKeyboardHighlight) return;
        
        UnhighlightButton();
    }
    
    // Public methods untuk manual control
    public void HighlightButton()
    {
        if (isHighlighted) return;
        
        isHighlighted = true;
        targetScale = Vector3.one * highlightedScale;
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        scaleCoroutine = StartCoroutine(ScaleToTarget());
    }
    
    public void UnhighlightButton()
    {
        if (!isHighlighted) return;
        
        isHighlighted = false;
        targetScale = Vector3.one * normalScale;
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        scaleCoroutine = StartCoroutine(ScaleToTarget());
    }
    
    private IEnumerator ScaleToTarget()
    {
        Vector3 startScale = rectTransform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            float curveValue = scaleCurve.Evaluate(t);
            
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }
        
        rectTransform.localScale = targetScale;
        scaleCoroutine = null;
    }
    
    // Method untuk testing di Inspector
    [ContextMenu("Test Highlight")]
    private void TestHighlight()
    {
        HighlightButton();
    }
    
    [ContextMenu("Test Unhighlight")]
    private void TestUnhighlight()
    {
        UnhighlightButton();
    }
    
    void Update()
    {
        // Debug visualization (optional)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (!isHighlighted)
                HighlightButton();
            else
                UnhighlightButton();
        }
        #endif
    }
}