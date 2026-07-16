using UnityEngine;

// ENKEL FIENDE-AI (ingen NavMesh används i projektet ännu, så vi flyttar rakt mot spelaren
// med Vector3.MoveTowards). Inget avancerat state-machine-ramverk, bara fyra lägen:
//
//   Idle      -> passiv, väntar
//   Chasing   -> spelaren är inom aggroRange, jagar
//   Attacking -> spelaren är inom attackRange, står still och slår på en cooldown
//   Returning -> spelaren sprang för långt bort (leashRange), ger upp och går hem
//
// Lägg detta script på samma GameObject som Enemy.cs.
public class EnemyAI : MonoBehaviour
{
    [Header("Referenser (hittas automatiskt)")]
    public Enemy enemy;
    public PlayerHealth playerHealth;   // spelaren hittas automatiskt via detta

    [Header("Avstånd")]
    public float aggroRange = 8f;    // Hur nära spelaren måste vara för att fienden ska börja jaga
    public float attackRange = 2f;   // Hur nära fienden måste vara för att attackera
    public float leashRange = 16f;   // Hur långt fienden får dras bort från startpositionen innan den ger upp

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackCooldown = 2f;   // Sekunder mellan varje attack

    [Header("Rörelse")]
    public float moveSpeed = 3.5f;

    enum State { Idle, Chasing, Attacking, Returning }
    private State state = State.Idle;

    private Transform player;
    private Vector3 spawnPos;
    private float nextAttackTime = 0f;

    // Sätts av t.ex. BossSlamAttack medan en telegraferad specialattack förbereds — fryser
    // hela AI:t (ingen rörelse/vanlig attack) så bossen står still under förvarningen.
    public bool IsCasting = false;

    // Används av BossSlamAttack för att inte trigga en specialattack innan bossen ens har
    // märkt spelaren (annars kunde Slam trigga på ett större avstånd än aggroRange).
    public bool IsEngaged => state == State.Chasing || state == State.Attacking;

    void Awake()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
        spawnPos = transform.position;
    }

    void Start()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) player = playerHealth.transform;
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead || player == null || IsCasting) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        float distFromSpawn = Vector3.Distance(transform.position, spawnPos);

        switch (state)
        {
            case State.Idle:
                if (distToPlayer <= aggroRange) state = State.Chasing;
                break;

            case State.Chasing:
                if (distFromSpawn > leashRange) { state = State.Returning; break; }
                if (distToPlayer <= attackRange) { state = State.Attacking; break; }
                MoveTowards(player.position);
                break;

            case State.Attacking:
                if (distFromSpawn > leashRange) { state = State.Returning; break; }
                if (distToPlayer > attackRange) { state = State.Chasing; break; }
                FaceTowards(player.position);
                TryAttack();
                break;

            case State.Returning:
                MoveTowards(spawnPos);

                if (Vector3.Distance(transform.position, spawnPos) < 0.1f)
                {
                    // Hemma igen -> reset, redo att aggra på nytt
                    transform.position = spawnPos;
                    enemy.currentHealth = enemy.maxHealth;
                    state = State.Idle;
                }
                else if (distToPlayer <= aggroRange)
                {
                    // Spelaren kom tillbaka innan fienden hann hem -> jaga igen direkt
                    state = State.Chasing;
                }
                break;
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
        FaceTowards(target);
    }

    void FaceTowards(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    // Kallas av Enemy.cs vid respawn — annars fryser state (t.ex. Attacking/Chasing)
    // kvar från dödsögonblicket och fienden jagar dig direkt även om du sprang långt bort.
    public void ResetAI()
    {
        state = State.Idle;
        nextAttackTime = 0f;
    }
}
