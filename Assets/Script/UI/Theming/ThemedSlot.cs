using UnityEngine;
using UnityEngine.UI;

// Applicerar en SlotTheme på en hotbar-/quickuse-slots bakgrunds-Image. SetHighlighted(true)
// växlar till highlightedSprite/highlightedTint (t.ex. för en låst ability-slot).
[ExecuteAlways]
public class ThemedSlot : MonoBehaviour
{
    public SlotTheme theme;
    public Image slotBackground;

    private bool highlighted;

    void OnValidate() => Apply();
    void Start() => Apply();

    public void SetHighlighted(bool on)
    {
        highlighted = on;
        Apply();
    }

    public void Apply()
    {
        if (theme == null || slotBackground == null) return;

        if (highlighted)
        {
            if (theme.highlightedSprite != null) slotBackground.sprite = theme.highlightedSprite;
            slotBackground.color = theme.highlightedTint;
        }
        else
        {
            if (theme.emptySprite != null) slotBackground.sprite = theme.emptySprite;
            slotBackground.color = theme.emptyTint;
        }
    }
}
