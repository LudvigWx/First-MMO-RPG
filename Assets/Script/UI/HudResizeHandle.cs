using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Litet handtag i nedre högra hörnet av ett element (Health/Rage/XP-barerna) — dra för att
// ändra bredd+höjd. Läggs på av PlayerHudUI som barn till baren själv, så det följer med
// automatiskt när baren flyttas/kopplas loss i Full Edit Mode. Synligt bara när både
// HudDragHandle.EditModeActive och HudDragHandle.FullEditModeActive är sanna.
public class HudResizeHandle : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform target;
    public LayoutElement targetLayoutElement;
    public string sizeKey;
    public Vector2 minSize = new Vector2(40f, 6f);
    public Vector2 maxSize = new Vector2(900f, 80f);

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        ApplyVisibility();
    }

    // Kallas av PlayerHudUI varje gång Full Edit Mode eller pausmenyns Edit Mode-flik
    // slås av/på, så handtaget bara syns/går att träffa när båda är sanna.
    public void ApplyVisibility()
    {
        gameObject.SetActive(HudDragHandle.EditModeActive && HudDragHandle.FullEditModeActive);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!HudDragHandle.EditModeActive || !HudDragHandle.FullEditModeActive) return;
        if (target == null) return;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scale;

        Vector2 size = target.sizeDelta;
        size.x = Mathf.Clamp(size.x + delta.x, minSize.x, maxSize.x);
        size.y = Mathf.Clamp(size.y - delta.y, minSize.y, maxSize.y);
        target.sizeDelta = size;

        if (targetLayoutElement != null)
        {
            targetLayoutElement.preferredWidth = size.x;
            targetLayoutElement.preferredHeight = size.y;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!HudDragHandle.EditModeActive || !HudDragHandle.FullEditModeActive) return;
        if (target == null || string.IsNullOrEmpty(sizeKey)) return;

        PlayerPrefs.SetFloat(sizeKey + "_w", target.sizeDelta.x);
        PlayerPrefs.SetFloat(sizeKey + "_h", target.sizeDelta.y);
        PlayerPrefs.Save();
    }
}
