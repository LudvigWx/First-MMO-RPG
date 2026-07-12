using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Identitet")]
    public string enemyName = "Fiende";
    public int level = 1;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Typ")]
    public bool isBoss = false;   // Bocka i denna för bossar

    [Header("Health-regen (utanför strid)")]
    public float regenDelay = 4f;        // Sekunder utan att ta skada innan regen startar
    public float regenPerSecond = 10f;   // Hur mycket HP som återfås per sekund

    [Header("Respawn")]
    public float respawnTime = 25f;      // Sekunder efter död innan fienden kommer tillbaka

    private float lastDamageTime = -999f;
    private float regenCarry = 0f;       // sparar bråkdelar av HP mellan frames
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private TargetingController targeting;

    void Awake()
    {
        currentHealth = maxHealth;          // starta med full health
        spawnPos = transform.position;      // kom ihåg var vi föddes
        spawnRot = transform.rotation;
    }

    void Start()
    {
        // Hitta spelarens targeting-script så vi kan avmarkera oss själva när vi dör
        targeting = FindFirstObjectByType<TargetingController>();
    }

    void Update()
    {
        RegenTick();
    }

    // Långsam health-regen när fienden INTE tagit skada på ett tag
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

    // Kallas när spelaren träffar
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        lastDamageTime = Time.time;   // nollställ regen-timern
        regenCarry = 0f;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public bool IsDead => currentHealth <= 0;

    void Die()
    {
        Debug.Log(enemyName + " dog. Respawnar om " + respawnTime + "s.");

        // Avmarkera oss själva om spelaren hade oss som target
        if (targeting != null && targeting.CurrentTarget == this)
            targeting.ClearTarget();

        SetVisible(false);                 // göm kropp + stäng av så vi inte kan träffas
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        // Tillbaka på ursprungsplatsen med full health
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        currentHealth = maxHealth;
        regenCarry = 0f;
        lastDamageTime = Time.time;
        SetVisible(true);

        Debug.Log(enemyName + " respawnade.");
    }

    // Slår av/på alla renderers + colliders (kroppen försvinner men objektet lever kvar
    // så respawn-timern kan fortsätta räkna).
    void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
    }
}
