using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Smoothly scales a UI element up while hovered (or selected with keyboard/gamepad).</summary>
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float speed = 14f;

    private Vector3 baseScale;
    private bool hovered;
    private bool selected;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        hovered = false;
        selected = false;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        Vector3 target = baseScale * (hovered || selected ? hoverScale : 1f);
        // Frame-rate independent smoothing; unscaled so it works while the game is paused.
        float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, target, t);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    // Only react to navigation selection; a mouse click also selects, which would leave the button stuck scaled up.
    public void OnSelect(BaseEventData eventData) => selected = !(eventData is PointerEventData);
    public void OnDeselect(BaseEventData eventData) => selected = false;
}
