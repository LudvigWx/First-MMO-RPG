using UnityEngine;

// MELEE BASIC ATTACK (svärd/yxa-klasser, t.ex. Warbrand).
// Basic Attack är den ENDA förmågan utan cooldown och utan resurskostnad (ren filler).
// Den här varianten TOGGLAR den befintliga auto-attack-motorn (PlayerAttack):
// tryck för att slå på, tryck igen för att slå av. Slår upprepat på nära håll och
// genererar Rage vid träff.
//
// (Caster-klasser får senare en egen RangedBasicAttack som istället kastar på håll
//  och matar Mana – samma idé, annan variant i slot 0.)
[CreateAssetMenu(fileName = "BasicAttack_Melee", menuName = "Abilities/Warbrand/Basic Attack (Melee)")]
public class MeleeBasicAttackAbility : Ability
{
    // Bra standardvärden när du skapar kortet.
    void Reset()
    {
        abilityName = "Basic Attack";
        description = "Repeated melee attack. No cooldown, no cost. Generates Rage.";
        isFreeFiller = true;    // enda förmågan som FÅR sakna cooldown/kostnad
        cooldown = 0f;
        rageCost = 0;
        requiresTarget = false; // motorn (PlayerAttack) sköter target-kravet + av-toggling själv
        range = 0f;
    }

    public override bool Activate(AbilityContext ctx)
    {
        if (ctx.playerAttack == null) return false;
        ctx.playerAttack.ToggleAutoAttack();   // slå på/av auto-attacken
        return true;
    }
}
