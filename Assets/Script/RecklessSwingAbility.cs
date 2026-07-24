using UnityEngine;

// RECKLESS SWING – Ravager-specifik (subklass av Warbrand).
// Offensiv burst: hög skada, kostar MER Rage än snittet, kort cooldown. Kräver target i närstrid.
// Skapa via: Create -> Abilities -> Warbrand -> Ravager -> Reckless Swing.
[CreateAssetMenu(fileName = "RecklessSwing", menuName = "Abilities/Warbrand/Ravager/Reckless Swing")]
public class RecklessSwingAbility : Ability
{
    [Header("Reckless Swing-inställningar")]
    public int damage = 60;   // hög skada (mot Basic Attack ~20, Charge ~20)

    void Reset()
    {
        abilityName = "Reckless Swing";
        description = "Powerful strike. High damage, costly in Rage, short cooldown. Ravager-specific.";
        isFreeFiller = false;
        cooldown = 3f;       // kort cooldown
        rageCost = 35;       // mer än snittet (Charge 20)
        requiresTarget = true;
        range = 4f;          // närstrid
    }

    public override bool Activate(AbilityContext ctx)
    {
        Enemy target = ctx.targeting.CurrentTarget;
        if (target == null || target.IsDead) return false;

        target.TakeDamage(damage);
        ctx.combat.EnterCombat(target);
        Debug.Log("RECKLESS SWING! " + target.enemyName + " -" + damage +
                  " HP (kvar: " + target.currentHealth + ")");
        return true;
    }
}
