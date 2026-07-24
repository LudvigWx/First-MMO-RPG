using UnityEngine;

// SHIELD WALL – Bulwark-specifik (subklass av Warbrand).
// Defensiv buff: minskar inkommande skada med X% i Y sekunder. Kostar Rage + lång cooldown.
// Self-cast (kräver ingen target).
// Skapa via: Create -> Abilities -> Warbrand -> Bulwark -> Shield Wall.
[CreateAssetMenu(fileName = "ShieldWall", menuName = "Abilities/Warbrand/Bulwark/Shield Wall")]
public class ShieldWallAbility : Ability
{
    [Header("Shield Wall-inställningar")]
    [Range(0f, 1f)] public float damageReductionPercent = 0.4f;  // 0.4 = -40% skada
    public float buffDuration = 6f;

    void Reset()
    {
        abilityName = "Shield Wall";
        description = "Reduces incoming damage for a few seconds. Bulwark-specific.";
        isFreeFiller = false;
        cooldown = 20f;      // lång cooldown
        rageCost = 30;
        requiresTarget = false;  // self-cast
        range = 0f;
    }

    public override bool Activate(AbilityContext ctx)
    {
        if (ctx.playerHealth == null)
        {
            Debug.LogWarning("Shield Wall: ingen PlayerHealth på spelaren – lägg till komponenten på PlayerArmature.");
            return false;   // returnerar false -> ingen Rage dras, ingen cooldown startar
        }

        ctx.playerHealth.ApplyDamageReduction(damageReductionPercent, buffDuration);
        return true;
    }
}
