using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UISelectionFeedback : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    private Vector3 originalScale;
    private bool hasOriginalScale = false;

    void Awake()
    {
        CacheScale();
        EnsureOutline();
    }

    void OnEnable()
    {
        CacheScale();
        EnsureOutline();
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            SetHighlight(true);
        }
        else
        {
            SetHighlight(false);
        }
    }

    void OnDisable()
    {
        SetHighlight(false);
    }

    private void CacheScale()
    {
        if (!hasOriginalScale)
        {
            originalScale = transform.localScale;
            if (originalScale.sqrMagnitude < 0.001f) originalScale = Vector3.one;
            hasOriginalScale = true;
        }
    }

    private void EnsureOutline()
    {
        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(0f, 1f, 0.95f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlight(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
        {
            SetHighlight(false);
        }
    }

    public void SetHighlight(bool active)
    {
        CacheScale();
        EnsureOutline();
        if (outline != null)
        {
            outline.enabled = active;
            outline.effectColor = active ? new Color(0f, 1f, 0.95f, 0.95f) : Color.clear;
        }
        transform.localScale = active ? originalScale * 1.05f : originalScale;
    }
}
