using UnityEngine;

// Spelarens health med "regen utanför strid" + en skademinsknings-buff (för Shield Wall).
// När fiender slår spelaren kallar de PlayerHealth.TakeDamage(...), som räknar bort ev.
// aktiv skademinskning och sedan skickar ut OnPlayerDamaged-eventet. PlayerHealth vet
// INGET om Rage, Mana osv – vilket resurssystem som helst kan prenumerera på eventet
// (se RageResource för ett exempel).
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 200;
    public int currentHealth = 200;

    [Header("Health-regen (utanför strid)")]
    public float regenDelay = 5f;        // Sekunder utan att ta skada innan regen startar
    public float regenPerSecond = 12f;   // HP som återfås per sekund

    // Generellt skade-event: vilket resurssystem som helst (Rage, Mana, Stamina...) kan
    // prenumerera på detta för att reagera när spelaren tar skada.
    public event System.Action<float> OnPlayerDamaged;

    private float lastDamageTime = -999f;
    private float regenCarry = 0f;

    // ---- Skademinskning (Shield Wall lägger denna) ----
    private float damageReduction = 0f;   // 0..1 (0.4 = -40% skada)
    private float reductionExpiry = 0f;   // Time.time då bufferten tar slut
    private bool reductionWasActive = false;

    private float CurrentReduction => Time.time < reductionExpiry ? damageReduction : 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        RegenTick();
        TrackReductionExpiry();
    }

    void RegenTick()
    {
        if (IsDead) return;
        if (currentHealth >= maxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        regenCarry += regenPerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(regenCarry);
        if (whole > 0)
        {
            currentHealth += whole;
            regenCarry -= whole;
            if (currentHealth >= maxHealth)
            {
                currentHealth = maxHealth;
                regenCarry = 0f;
            }
        }
    }

    // Loggar när Shield Wall-bufferten tar slut (så vi ser det även utan inkommande skada)
    void TrackReductionExpiry()
    {
        bool active = CurrentReduction > 0f;
        if (reductionWasActive && !active) Debug.Log("Shield Wall tog slut.");
        reductionWasActive = active;
    }

    // Lägger en tidsbegränsad skademinskning (anropas av Shield Wall)
    public void ApplyDamageReduction(float percent, float duration)
    {
        damageReduction = Mathf.Clamp01(percent);
        reductionExpiry = Time.time + duration;
        Debug.Log("Shield Wall aktiv: -" + Mathf.RoundToInt(damageReduction * 100f) +
                  "% skada i " + duration + "s.");
    }

    // Kallas senare av fiendernas attacker (EnemyAI)
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        int finalAmount = Mathf.RoundToInt(amount * (1f - CurrentReduction));
        if (finalAmount < 0) finalAmount = 0;

        currentHealth -= finalAmount;
        if (currentHealth < 0) currentHealth = 0;
        lastDamageTime = Time.time;
        regenCarry = 0f;

        OnPlayerDamaged?.Invoke(finalAmount);   // låt ev. prenumeranter (Rage, Mana...) reagera
        Debug.Log("Spelaren tog " + finalAmount + " skada" +
                  (CurrentReduction > 0f ? " (Shield Wall dämpade)" : "") +
                  " (kvar: " + currentHealth + "/" + maxHealth + ")");
    }

    public bool IsDead => currentHealth <= 0;
}
