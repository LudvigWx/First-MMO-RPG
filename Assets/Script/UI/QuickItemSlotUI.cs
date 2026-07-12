using UnityEngine;
using UnityEngine.UI;

// Platshållare för quick-use items (potions m.m.).
// Helt oberoende av ability-cooldowns — hakas i mot ett framtida item-system.
public class QuickItemSlotUI : MonoBehaviour
{
    [HideInInspector] public int slotIndex;
    public Image iconImage;

    static readonly Color emptyColor = new Color(0f, 0f, 0f, 0.35f);

    void Start()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = emptyColor;
        }
    }

    // Kopplas till knappens OnClick av HotbarUI. Fyll i riktig logik när item-systemet finns.
    public void OnClickUse()
    {
        Debug.Log("Quick item slot " + (slotIndex + 1) + " använd (platshållare, inget item-system kopplat än).");
    }
}
