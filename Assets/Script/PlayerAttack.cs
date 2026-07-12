using System.Collections;
using UnityEngine;

// MELEE-BASIC-ATTACK-MOTORN (auto-attack).
// Själva på/av-knappen sitter numera i ability-systemet: MeleeBasicAttackAbility i slot 0
// anropar ToggleAutoAttack() här. När den är PÅ svingar den automatiskt på en timer så
// länge du har en giltig target inom räckvidd och är vänd mot den. Stängs av automatiskt
// när du lämnar combat eller tappar target.
public class PlayerAttack : MonoBehaviour
{
    [Header("Referenser")]
    public TargetingController targeting;
    public CombatManager combat;
    public RageResource rage;

    [Header("Attack-inställningar")]
    public int attackDamage = 20;
    public float attackRange = 4f;      // Hur nära du måste stå
    public float attackWindup = 0.5f;   // "Cast-tid" innan träffen sker
    public float attackInterval = 1.5f; // Tid mellan auto-svingar (attackhastighet)
    public float maxAttackAngle = 90f;  // Du måste vara vänd mot fienden (grader från "rakt fram")

    private bool isAttacking = false;   // mitt i en swing (windup pågår)
    private bool autoAttackOn = false;  // auto-attack på/av
    private float nextSwingTime = 0f;   // när nästa auto-swing får ske
    private bool wasInCombat = false;

    void Start()
    {
        if (targeting == null) targeting = GetComponent<TargetingController>();
        if (combat == null) combat = GetComponent<CombatManager>();
        if (rage == null) rage = GetComponent<RageResource>();
    }

    void Update()
    {
        // Lämnar vi combat -> stäng av auto-attack
        bool inCombatNow = combat != null && combat.InCombat;
        if (wasInCombat && !inCombatNow && autoAttackOn)
        {
            autoAttackOn = false;
            Debug.Log("Auto-attack AV (lämnade combat).");
        }
        wasInCombat = inCombatNow;

        // Kör auto-attack-loopen (på/av-toggeln kommer från ability-systemet, slot 0)
        if (autoAttackOn) HandleAutoAttack();
    }

    // Anropas av MeleeBasicAttackAbility (slot 0) för att slå på/av auto-attacken.
    public void ToggleAutoAttack()
    {
        if (!autoAttackOn)
        {
            // Slå PÅ – kräver en giltig target
            if (targeting.CurrentTarget == null || targeting.CurrentTarget.IsDead)
            {
                Debug.Log("Ingen target vald – välj en fiende först.");
                return;
            }
            autoAttackOn = true;
            nextSwingTime = 0f;   // svinga direkt
            Debug.Log("Auto-attack PÅ (Basic Attack).");
        }
        else
        {
            autoAttackOn = false;
            Debug.Log("Auto-attack AV.");
        }
    }

    void HandleAutoAttack()
    {
        // Behöver en levande target
        Enemy target = targeting.CurrentTarget;
        if (target == null || target.IsDead)
        {
            autoAttackOn = false;
            Debug.Log("Auto-attack AV (ingen giltig target).");
            return;
        }

        if (isAttacking) return;               // redan mitt i en swing
        if (Time.time < nextSwingTime) return; // väntar på nästa swing

        // Inom räckvidd? Annars vänta tyst och försök igen strax (spammar inte Console).
        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > attackRange)
        {
            nextSwingTime = Time.time + 0.25f;
            return;
        }

        // Vänd mot fienden? Annars vänta (du måste vrida dig mot målet för att slå).
        if (!IsFacing(target))
        {
            nextSwingTime = Time.time + 0.15f;
            return;
        }

        nextSwingTime = Time.time + attackInterval;
        StartCoroutine(AttackRoutine(target));
    }

    IEnumerator AttackRoutine(Enemy target)
    {
        isAttacking = true;

        // Windup / cast-tid
        yield return new WaitForSeconds(attackWindup);

        // Kontrollera att målet fortfarande finns, är inom räckvidd OCH att du är vänd mot det
        if (target != null && !target.IsDead)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= attackRange && IsFacing(target))
            {
                // ===== TRÄFF =====
                if (rage != null) rage.GainFromBasicAttack();   // Basic Attack genererar Rage
                target.TakeDamage(attackDamage);                // kan döda fienden (triggar Die/respawn)

                if (target.IsDead)
                {
                    Debug.Log("DÖDSSTÖT! " + target.enemyName + " besegrad.");
                    // Ingen EnterCombat här – fienden är död. CombatManager avslutar med SEGER.
                }
                else
                {
                    combat.EnterCombat(target);   // se till att vi är i combat (om vi inte redan var)
                    Debug.Log("TRÄFF! " + target.enemyName + " -" + attackDamage + " HP (kvar: " + target.currentHealth + ")");
                }
            }
        }

        isAttacking = false;
    }

    // Är spelaren vänd mot fienden? (jämför "rakt fram" med riktningen till målet, plant i XZ)
    bool IsFacing(Enemy target)
    {
        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;   // står ovanpå varandra – räkna som vänd
        float angle = Vector3.Angle(transform.forward, to.normalized);
        return angle <= maxAttackAngle;
    }
}
