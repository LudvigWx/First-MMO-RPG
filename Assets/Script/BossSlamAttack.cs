using System.Collections;
using UnityEngine;

// Telegraferad markattack, bara aktiv på bossar (enemy.isBoss). Bossen fryser (via
// EnemyAI.IsCasting) och en blinkande markindikator visar var smällen kommer — hinner
// spelaren ut ur zonen innan förvarningstiden är slut (t.ex. med DashAbility) blir det
// ingen skada. Zonen är FAST vid spelarens position när casten startar, den följer inte
// efter, så det alltid går att fly undan om man reagerar i tid.
//
// Lägg detta script på samma GameObject som Enemy.cs/EnemyAI.cs, bara på bossar (t.ex. Sven).
public class BossSlamAttack : MonoBehaviour
{
    [Header("Referenser (hittas automatiskt)")]
    public Enemy enemy;
    public EnemyAI enemyAI;
    public PlayerHealth playerHealth;

    [Header("Slam-inställningar")]
    public float triggerRange = 10f;        // hur nära spelaren måste vara för att bossen ska casta
    public float telegraphDuration = 1.5f;  // förvarningstid innan smällen
    public float slamRadius = 2f;           // varningszonens/skadeområdets radie — sänkt, 3.5 var nästan ogenomflybart
    public int slamDamage = 40;
    public float slamCooldown = 9f;         // separat cooldown, oberoende av vanliga attacker
    public float firstSlamDelay = 5f;       // bossen får INTE casta Slam som allra första sak i striden

    [Header("Varningsindikator (platshållare, ingen VFX/shader-risk)")]
    public Color telegraphColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public float blinkInterval = 0.15f;

    private float nextSlamTime = 0f;
    private bool isCasting = false;
    private bool wasEngagedLastFrame = false;

    void Start()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    void Update()
    {
        bool engagedNow = enemyAI != null && enemyAI.IsEngaged;

        // Striden precis börjat (eller startat om efter att bossen gett upp och gått hem) —
        // ge spelaren en stund att hinna slå några gånger innan första Slammen kan komma.
        if (engagedNow && !wasEngagedLastFrame)
        {
            nextSlamTime = Mathf.Max(nextSlamTime, Time.time + firstSlamDelay);
        }
        wasEngagedLastFrame = engagedNow;

        if (enemy == null || !enemy.isBoss || enemy.IsDead || isCasting) return;
        if (playerHealth == null || playerHealth.IsDead) return;
        if (Time.time < nextSlamTime) return;
        if (!engagedNow) return; // bossen måste redan jaga/slåss, inte bara stå still

        float dist = Vector3.Distance(transform.position, playerHealth.transform.position);
        if (dist <= triggerRange) StartCoroutine(SlamRoutine());
    }

    IEnumerator SlamRoutine()
    {
        isCasting = true;
        nextSlamTime = Time.time + slamCooldown;
        if (enemyAI != null) enemyAI.IsCasting = true;

        Vector3 zoneCenter = playerHealth.transform.position;
        Debug.Log(enemy.enemyName + " förbereder SLAM — spring ut ur zonen inom " + telegraphDuration + "s!");

        GameObject indicator = CreateTelegraphIndicator(zoneCenter);
        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();

        float elapsed = 0f;
        float nextBlink = blinkInterval;
        bool blinkOn = true;

        while (elapsed < telegraphDuration)
        {
            if (enemy.IsDead) break; // bossen dog under sin egen förvarning — ingen skada blir av

            elapsed += Time.deltaTime;
            if (elapsed >= nextBlink)
            {
                blinkOn = !blinkOn;
                indicatorRenderer.enabled = blinkOn;
                nextBlink += blinkInterval;
            }
            yield return null;
        }

        if (enemy.IsDead)
        {
            Debug.Log("SLAM avbröts — " + enemy.enemyName + " dog under förvarningen.");
        }
        else
        {
            Vector3 playerFlat = playerHealth.transform.position; playerFlat.y = 0f;
            Vector3 zoneFlat = zoneCenter; zoneFlat.y = 0f;

            if (Vector3.Distance(playerFlat, zoneFlat) <= slamRadius)
            {
                playerHealth.TakeDamage(slamDamage);
                Debug.Log("SLAM träffade! -" + slamDamage + " skada.");
            }
            else
            {
                Debug.Log("SLAM missade — spelaren hann undan zonen!");
            }
        }

        Destroy(indicator);
        isCasting = false;
        if (enemyAI != null) enemyAI.IsCasting = false;
    }

    // Platt cirkel-platshållare (skalad Quad, opak färg — ingen transparent-shader-uppsättning
    // som riskerar att se fel ut utan att gå att verifiera visuellt här). Byt gärna ut mot en
    // riktig dekal/VFX senare.
    GameObject CreateTelegraphIndicator(Vector3 worldPos)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "SlamTelegraph";
        Destroy(indicator.GetComponent<Collider>());

        indicator.transform.position = worldPos + Vector3.up * 0.05f;
        indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        indicator.transform.localScale = Vector3.one * (slamRadius * 2f);

        Renderer rend = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = telegraphColor;
        rend.material = mat;

        return indicator;
    }
}
