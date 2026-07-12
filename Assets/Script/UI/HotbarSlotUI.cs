using UnityEngine;
using UnityEngine.UI;
using TMPro;

// En enskild förmåge-knapp i hotbaren.
// Ren VISNING + vidarebefordran av klick — all logik (cooldown, Rage, giltighet)
// sköts fortfarande av AbilityCaster. Byggs och kopplas ihop av HotbarUI.
public class HotbarSlotUI : MonoBehaviour
{
    [HideInInspector] public int slotIndex;
    [HideInInspector] public AbilityCaster caster;

    public Image iconImage;
    public Image cooldownOverlay;   // Image.Type = Filled, krymper medan cooldown räknar ner
    public TMP_Text keybindText;

    static readonly Color emptyColor = new Color(0f, 0f, 0f, 0.35f);

    void Update()
    {
        if (caster == null || iconImage == null) return;

        Ability ability = (slotIndex >= 0 && slotIndex < caster.slots.Length) ? caster.slots[slotIndex] : null;

        if (ability == null)
        {
            iconImage.sprite = null;
            iconImage.color = emptyColor;
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            return;
        }

        if (ability.icon != null)
        {
            iconImage.sprite = ability.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = PlaceholderColorFor(ability);
        }

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = caster.CooldownFraction(slotIndex);
    }

    // Stabil platshållarfärg per förmåga, så samma ability alltid ser likadan ut tills riktiga ikoner finns.
    static Color PlaceholderColorFor(Ability ability)
    {
        int hash = ability.abilityName != null ? ability.abilityName.GetHashCode() : ability.GetInstanceID();
        float hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.55f, 0.85f);
    }

    // Kopplas till knappens OnClick av HotbarUI.
    public void OnClickCast()
    {
        if (caster != null) caster.TryCast(slotIndex);
    }

    // Publik hake för en framtida "spellbook"/pool-UI som vill tilldela förmågor till slots.
    // Vägrar automatiskt på låsta slots (se AbilityCaster.SetSlot).
    public void AssignAbility(Ability ability)
    {
        if (caster != null) caster.SetSlot(slotIndex, ability);
    }
}
