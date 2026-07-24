using System.Collections;
using UnityEngine;

// CHARGE – Warbrand generell gap-closer.
// Rusar fram till målet och slår till. Kostar Rage + har cooldown (som alla icke-basic förmågor).
// Skapa ett data-kort via: Högerklick i Project -> Create -> Abilities -> Warbrand -> Charge.
[CreateAssetMenu(fileName = "Charge", menuName = "Abilities/Warbrand/Charge")]
public class ChargeAbility : Ability
{
    [Header("Charge-inställningar")]
    public int chargeDamage = 40;       // märkbart mer än Basic Attack (20)
    public float chargeSpeed = 22f;     // hur snabbt man rusar fram
    public float stopDistance = 2.5f;   // hur nära målet man stannar
    public float maxChargeTime = 1.0f;  // säkerhets-tak så man inte rusar för evigt

    // Bra standardvärden sätts automatiskt när du skapar kortet (du kan ändra efteråt).
    void Reset()
    {
        abilityName = "Charge";
        description = "Rush to the target and strike. Gap-closer.";
        isFreeFiller = false;
        cooldown = 6f;
        rageCost = 20;
        requiresTarget = true;
        range = 20f;   // kan användas på långt håll (det är ju en gap-closer)
    }

    public override bool Activate(AbilityContext ctx)
    {
        Enemy target = ctx.targeting.CurrentTarget;
        if (target == null || target.IsDead) return false;   // extra säkerhet

        ctx.runner.StartCoroutine(ChargeRoutine(ctx, target));
        return true;
    }

    IEnumerator ChargeRoutine(AbilityContext ctx, Enemy target)
    {
        Debug.Log("CHARGE mot " + target.enemyName + "!");

        float t = 0f;
        while (t < maxChargeTime)
        {
            if (target == null || target.IsDead) break;

            Vector3 to = target.transform.position - ctx.player.position;
            to.y = 0f;

            if (to.magnitude <= stopDistance) break;   // framme

            Vector3 dir = to.normalized;
            ctx.player.rotation = Quaternion.LookRotation(dir);      // vänd mot målet
            ctx.controller.Move(dir * chargeSpeed * Time.deltaTime); // rusa fram
            t += Time.deltaTime;
            yield return null;
        }

        // Framme -> skada + gå in i combat
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(chargeDamage);
            ctx.combat.EnterCombat(target);
            Debug.Log("CHARGE träff! " + target.enemyName + " -" + chargeDamage +
                      " HP (kvar: " + target.currentHealth + ")");
        }
    }
}
