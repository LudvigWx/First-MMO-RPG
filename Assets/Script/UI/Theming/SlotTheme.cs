using UnityEngine;

// Ett visuellt tema för hotbar-/quickuse-slots. "Highlighted" används t.ex. för låsta
// ability-slots (samma roll som outline-highlighten på slot 1 i hotbaren idag).
[CreateAssetMenu(menuName = "Naxestra/UI Theme/Slot Theme", fileName = "NewSlotTheme")]
public class SlotTheme : ScriptableObject
{
    [Header("Tom slot")]
    public Sprite emptySprite;
    public Color emptyTint = Color.white;

    [Header("Highlighted/selected slot")]
    public Sprite highlightedSprite;
    public Color highlightedTint = Color.white;
}
