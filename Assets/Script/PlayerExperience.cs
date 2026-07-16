using UnityEngine;

// Håller koll på spelarens nivå och XP. Lyssnar på CombatManager.EnemyDefeated
// och delar ut XP automatiskt vid varje kill. Lägg på PlayerArmature
// (bredvid PlayerHealth/RageResource). Ingen UI än — testas via Console.
//
// XP-kurva: polynomisk (xpToNextLevel = baseXP * nivå^curveExponent), inte
// multiplikativ som förut. Ger snabba/lätta nivåer i början och en brant
// kurva mot maxLevel — tänkt att quests (byggs senare) ska bära merparten
// av XP:n vid höga nivåer, mob-kills är utfyllnad.
public class PlayerExperience : MonoBehaviour
{
    [Header("Nivå & XP")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel;
    public int maxLevel = 80;

    [Header("XP-kurva (xpToNextLevel = baseXP * nivå^curveExponent)")]
    public int baseXP = 80;
    public float curveExponent = 1.8f;

    public bool IsMaxLevel => level >= maxLevel;

    // Skjuts av varje gång XP faktiskt läggs till (kills nu, quests senare) — UI (t.ex.
    // PlayerHudUI:s flytande "XP: xxx"-popup) lyssnar på denna istället för att pollas.
    public event System.Action<int> OnXpGained;

    private CombatManager combatManager;

    void Start()
    {
        xpToNextLevel = CalculateXpForLevel(level);

        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
            combatManager.EnemyDefeated += HandleEnemyDefeated;
        else
            Debug.LogWarning("PlayerExperience: Ingen CombatManager hittades — XP delas inte ut.");
    }

    void OnDestroy()
    {
        if (combatManager != null)
            combatManager.EnemyDefeated -= HandleEnemyDefeated;
    }

    void HandleEnemyDefeated(Enemy enemy)
    {
        AddXP(enemy != null ? enemy.XpReward : 0);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        currentXP += amount;
        Debug.Log("+" + amount + " XP (" + currentXP + " / " + xpToNextLevel + ")");
        OnXpGained?.Invoke(amount);

        while (!IsMaxLevel && currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            xpToNextLevel = CalculateXpForLevel(level);
            Debug.Log("LEVEL UP! Nu nivå " + level + " (nästa nivå kräver " + xpToNextLevel + " XP)");
        }

        if (IsMaxLevel)
        {
            currentXP = 0;
            Debug.Log("MAX LEVEL (" + maxLevel + ") uppnådd!");
        }
    }

    int CalculateXpForLevel(int lvl)
    {
        return Mathf.RoundToInt(baseXP * Mathf.Pow(lvl, curveExponent));
    }
}
