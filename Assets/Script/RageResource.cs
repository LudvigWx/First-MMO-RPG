using UnityEngine;

// Warbrand-klassens resurs: RAGE.
// - Börjar på 0
// - Ökar av Basic Attack och av att ta skada
// - Nollställs när spelaren lämnar combat
//
// Andra klasser får senare EGNA resurs-script (Mana, Energy osv) — detta är bara Warbrand.
public class RageResource : MonoBehaviour
{
    [Header("Rage-mängd")]
    public int maxRage = 100;
    public int currentRage = 0;

    [Header("Rage-generering")]
    public int ragePerBasicAttack = 10;   // Rage du får när en Basic Attack TRÄFFAR
    public int ragePerDamageTaken = 5;     // Rage du får när DU tar skada (kopplas in när fiender slår tillbaka)

    [Header("Referenser (hittas automatiskt)")]
    public CombatManager combat;
    public PlayerHealth playerHealth;

    // Håller koll på om vi var i combat förra framen, så vi kan upptäcka NÄR vi lämnar combat.
    private bool wasInCombat = false;

    void Start()
    {
        if (combat == null) combat = GetComponent<CombatManager>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
    }

    // Prenumerera på PlayerHealths generella skade-event i stället för att bli anropad direkt
    // -> Mana/Stamina-script för framtida klasser kan göra exakt samma sak utan att röra PlayerHealth.
    void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnPlayerDamaged += HandlePlayerDamaged;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnPlayerDamaged -= HandlePlayerDamaged;
    }

    void HandlePlayerDamaged(float amount)
    {
        GainFromDamageTaken();
    }

    void Update()
    {
        // Upptäck övergången "i combat -> inte i combat" och nollställ då Rage.
        bool inCombatNow = combat != null && combat.InCombat;
        if (wasInCombat && !inCombatNow)
        {
            ResetRage();
        }
        wasInCombat = inCombatNow;
    }

    // ---- Generering ----
    public void AddRage(int amount)
    {
        currentRage += amount;
        if (currentRage > maxRage) currentRage = maxRage;
        if (currentRage < 0) currentRage = 0;
        Debug.Log("Rage: " + currentRage + "/" + maxRage);
    }

    public void GainFromBasicAttack() => AddRage(ragePerBasicAttack);
    public void GainFromDamageTaken() => AddRage(ragePerDamageTaken); // anropas senare av spelarens skade-system

    // ---- Förbrukning (används av ability-systemet i Steg 2) ----
    public bool HasEnough(int cost) => currentRage >= cost;

    public bool Spend(int cost)
    {
        if (cost <= 0) return true;          // gratis förmågor (t.ex. Basic Attack)
        if (!HasEnough(cost)) return false;  // inte råd -> förmågan får inte gå
        currentRage -= cost;
        Debug.Log("Rage spenderad: -" + cost + " (kvar: " + currentRage + ")");
        return true;
    }

    public void ResetRage()
    {
        currentRage = 0;
        Debug.Log("Rage nollställd.");
    }
}
