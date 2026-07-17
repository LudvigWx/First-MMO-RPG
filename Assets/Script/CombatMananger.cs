using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public bool InCombat { get; private set; }
    public Enemy CombatTarget { get; private set; }

    // Dash tillåts BARA i combat mot en boss (används i Steg 5)
    public bool DashAllowed => InCombat && CombatTarget != null && CombatTarget.isBoss;

    [Header("Combat-slut")]
    public float combatExitDistance = 8f;   // Springer du längre bort än så här -> FLYKT (bryts direkt)
    public float combatTimeout = 5f;        // Sekunder utan att landa en träff -> striden släpper

    // Varför striden tog slut
    public enum CombatEndReason { EnemyDefeated, Fled, LeftCombat, TargetLost }

    // XP/leveling-systemet hakar på HÄR senare för att dela ut XP vid en kill.
    public event System.Action<Enemy> EnemyDefeated;

    private float lastActionTime;   // när du senast gjorde något i strid (landade en träff)

    // Anropas vid varje träff. Startar combat om det behövs och håller striden "vid liv".
    public void EnterCombat(Enemy enemy)
    {
        if (enemy == null) return;

        lastActionTime = Time.time;   // varje träff nollställer timeout-klockan

        // Redan i combat mot samma fiende? Håll bara igång – logga inte om på nytt.
        if (InCombat && CombatTarget == enemy) return;

        InCombat = true;
        CombatTarget = enemy;

        string typ = enemy.isBoss ? " (BOSS – dash tillåten!)" : " (vanlig – ingen dash)";
        Debug.Log("COMBAT START mot " + enemy.enemyName + typ);
    }

    public void ExitCombat(CombatEndReason reason = CombatEndReason.TargetLost)
    {
        if (!InCombat) return;

        Enemy target = CombatTarget;

        switch (reason)
        {
            case CombatEndReason.EnemyDefeated:
                string namn = target != null ? target.enemyName : "Fienden";
                Debug.Log("SEGER! " + namn + " besegrad.");
                EnemyDefeated?.Invoke(target);   // <-- XP/quest-kroken (PlayerExperience + QuestManager lyssnar)
                break;

            case CombatEndReason.Fled:
                Debug.Log("Du flydde från striden – ingen XP.");
                break;

            case CombatEndReason.LeftCombat:
                Debug.Log("Du lämnade striden (ingen aktivitet) – ingen XP.");
                break;

            default:
                Debug.Log("Combat slut.");
                break;
        }

        InCombat = false;
        CombatTarget = null;
    }

    void Update()
    {
        if (!InCombat) return;

        // Målet försvann helt
        if (CombatTarget == null)
        {
            ExitCombat(CombatEndReason.TargetLost);
            return;
        }

        // Målet dog → SEGER
        if (CombatTarget.IsDead)
        {
            ExitCombat(CombatEndReason.EnemyDefeated);
            return;
        }

        // Sprang du för långt bort → FLYKT (bryts direkt)
        float dist = Vector3.Distance(transform.position, CombatTarget.transform.position);
        if (dist > combatExitDistance)
        {
            ExitCombat(CombatEndReason.Fled);
            return;
        }

        // Ingen träff på ett tag → striden släpper (du bröt striden)
        if (Time.time - lastActionTime > combatTimeout)
        {
            ExitCombat(CombatEndReason.LeftCombat);
        }
    }
}
